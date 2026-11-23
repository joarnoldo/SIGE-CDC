using Microsoft.AspNetCore.DataProtection;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace SIGECDC.Web.Configuracion;

public sealed record RutasPersistentesAplicacion(
    string DataProtectionKeysPath,
    string AlmacenamientoArchivosPath);

public static class PersistenciaOperativa
{
    private const string AplicacionDataProtection = "SIGECDC";

    public static RutasPersistentesAplicacion Preparar(
        IServiceCollection services,
        IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        var permiteRutaLocalPredeterminada = environment.IsDevelopment()
            || environment.IsEnvironment("QA");

        var rutaKeys = ResolverRuta(
            configuration["DataProtection:KeysPath"],
            "DataProtection:KeysPath",
            Path.Combine("SIGE-CDC", "data-protection-keys"),
            permiteRutaLocalPredeterminada,
            environment.ContentRootPath);
        var rutaArchivos = ResolverRuta(
            configuration["AlmacenamientoArchivos:RutaBase"],
            "AlmacenamientoArchivos:RutaBase",
            Path.Combine("SIGE-CDC", "archivos-protegidos"),
            permiteRutaLocalPredeterminada,
            environment.ContentRootPath);

        if (string.Equals(rutaKeys, rutaArchivos, ComparacionRutas()))
        {
            throw new InvalidOperationException(
                "DataProtection:KeysPath y AlmacenamientoArchivos:RutaBase deben ser directorios distintos.");
        }

        VerificarDirectorioEscribible(rutaKeys, "DataProtection:KeysPath");
        VerificarDirectorioEscribible(rutaArchivos, "AlmacenamientoArchivos:RutaBase");

        var dataProtection = services.AddDataProtection()
            .SetApplicationName(AplicacionDataProtection)
            .PersistKeysToFileSystem(new DirectoryInfo(rutaKeys));
        var modo = configuration["DataProtection:ProtectionMode"] ?? "Dpapi";
        if (string.Equals(modo, "Certificate", StringComparison.OrdinalIgnoreCase))
        {
            var certificado = CargarCertificado(configuration, environment, rutaKeys, rutaArchivos);
            // La clave privada se importa en memoria, sin perfil/store Windows.
            // Se conserva durante la vida del proveedor de Data Protection.
            dataProtection.ProtectKeysWithCertificate(certificado)
                .UnprotectKeysWithAnyCertificate(certificado);
        }
        else if (!string.Equals(modo, "Dpapi", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "DataProtection:ProtectionMode debe ser Dpapi o Certificate. No se permite protección sin cifrado.");
        }
        else if (OperatingSystem.IsWindows())
        {
            dataProtection.ProtectKeysWithDpapi();
        }

        return new RutasPersistentesAplicacion(rutaKeys, rutaArchivos);
    }

    private static X509Certificate2 CargarCertificado(
        IConfiguration configuration,
        IWebHostEnvironment environment,
        string rutaKeys,
        string rutaArchivos)
    {
        var ruta = configuration["DataProtection:CertificatePath"];
        if (string.IsNullOrWhiteSpace(ruta) || !Path.IsPathFullyQualified(ruta))
        {
            throw new InvalidOperationException(
                "DataProtection:CertificatePath debe contener una ruta absoluta privada externa.");
        }

        try
        {
            ruta = Path.GetFullPath(ruta);
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            throw new InvalidOperationException("DataProtection:CertificatePath no contiene una ruta válida.");
        }

        if (EsMismaOSubruta(environment.ContentRootPath, ruta)
            || EsMismaOSubruta(rutaArchivos, ruta)
            || EsMismaOSubruta(rutaKeys, ruta))
        {
            throw new InvalidOperationException(
                "DataProtection:CertificatePath debe estar fuera de publicación, uploads y key ring.");
        }

        var password = configuration["DataProtection:CertificatePassword"];
        if (string.IsNullOrWhiteSpace(password))
        {
            throw new InvalidOperationException(
                "DataProtection:CertificatePassword es obligatoria para Certificate; configure el secreto externamente.");
        }

        X509Certificate2? certificado = null;
        try
        {
            // IIS sin perfil necesita contexto de máquina al importar el PFX.
            // EphemeralKeySet sigue impidiendo persistir la clave privada en Windows:
            // no es DPAPI LocalMachine, PersistKeySet ni un fallback sin cifrado.
            var flags = X509KeyStorageFlags.EphemeralKeySet;
            if (OperatingSystem.IsWindows()) flags |= X509KeyStorageFlags.MachineKeySet;
            certificado = X509CertificateLoader.LoadPkcs12FromFile(ruta, password, flags);
            var ahora = DateTime.UtcNow;
            if (!certificado.HasPrivateKey
                || ahora < certificado.NotBefore.ToUniversalTime()
                || ahora >= certificado.NotAfter.ToUniversalTime())
            {
                throw new InvalidOperationException(
                    "DataProtection:CertificatePath requiere un certificado vigente con clave privada.");
            }

            using var rsa = certificado.GetRSAPrivateKey();
            if (rsa is null || rsa.KeySize < 2048)
            {
                throw new InvalidOperationException(
                    "DataProtection:CertificatePath requiere una clave privada RSA de al menos 2048 bits.");
            }

            // Comprueba uso real de la clave importada, no solo metadatos.
            var prueba = RandomNumberGenerator.GetBytes(32);
            var recuperado = rsa.Decrypt(rsa.Encrypt(prueba, RSAEncryptionPadding.OaepSHA256),
                RSAEncryptionPadding.OaepSHA256);
            if (!CryptographicOperations.FixedTimeEquals(prueba, recuperado))
            {
                throw new CryptographicException();
            }

            return certificado;
        }
        catch (InvalidOperationException)
        {
            certificado?.Dispose();
            throw;
        }
        catch (Exception ex) when (ex is CryptographicException or IOException or UnauthorizedAccessException)
        {
            certificado?.Dispose();
            throw new InvalidOperationException(
                "No fue posible cargar DataProtection:CertificatePath con DataProtection:CertificatePassword. "
                + "Verifique archivo, permisos, formato PFX y secreto externo.");
        }
    }

    private static string ResolverRuta(
        string? valorConfigurado,
        string clave,
        string sufijoLocal,
        bool permiteRutaLocalPredeterminada,
        string contentRootPath)
    {
        if (string.IsNullOrWhiteSpace(valorConfigurado))
        {
            if (!permiteRutaLocalPredeterminada)
            {
                throw new InvalidOperationException(
                    $"La configuración externa {clave} es obligatoria fuera de Development/QA.");
            }

            valorConfigurado = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                sufijoLocal);
        }

        string ruta;
        try
        {
            ruta = Path.GetFullPath(valorConfigurado);
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            throw new InvalidOperationException($"La configuración {clave} no contiene una ruta válida.");
        }

        if (!permiteRutaLocalPredeterminada && EsMismaOSubruta(contentRootPath, ruta))
        {
            throw new InvalidOperationException(
                $"La configuración {clave} debe apuntar fuera del árbol reemplazable de publicación.");
        }

        return ruta.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
    }

    private static bool EsMismaOSubruta(string directorioBase, string ruta)
    {
        var baseNormalizada = Path.GetFullPath(directorioBase)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var rutaNormalizada = Path.GetFullPath(ruta)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var relativa = Path.GetRelativePath(baseNormalizada, rutaNormalizada);

        return relativa == "."
            || (!relativa.StartsWith($"..{Path.DirectorySeparatorChar}", ComparacionRutas())
                && !string.Equals(relativa, "..", ComparacionRutas())
                && !Path.IsPathRooted(relativa));
    }

    private static void VerificarDirectorioEscribible(string ruta, string clave)
    {
        try
        {
            Directory.CreateDirectory(ruta);
            var prueba = Path.Combine(ruta, $".sige-write-{Guid.NewGuid():N}.tmp");
            using var archivo = new FileStream(
                prueba,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                1,
                FileOptions.DeleteOnClose);
            archivo.WriteByte(0);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            throw new InvalidOperationException(
                $"No fue posible preparar el directorio configurado en {clave}. Verifique existencia y permisos de escritura.");
        }
    }

    private static StringComparison ComparacionRutas() =>
        OperatingSystem.IsWindows()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;
}
