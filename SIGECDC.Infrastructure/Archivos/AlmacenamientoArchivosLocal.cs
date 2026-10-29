using SIGECDC.Application.Archivos;

namespace SIGECDC.Infrastructure.Archivos;

public sealed class AlmacenamientoArchivosLocal(string rutaBase) : IAlmacenamientoArchivosService
{
    private const long TamanoMaximoBytes = 10 * 1024 * 1024;
    private static readonly IReadOnlyDictionary<string, string> FormatosPermitidos =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        [".pdf"] = "application/pdf",
        [".doc"] = "application/msword",
        [".docx"] = "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        [".jpg"] = "image/jpeg",
        [".jpeg"] = "image/jpeg",
        [".png"] = "image/png"
    };

    private readonly string _rutaBase = ObtenerRutaBase(rutaBase);

    public async Task<ArchivoGuardado> GuardarAsync(
        Stream contenido,
        string nombreOriginal,
        string? mimeType,
        long tamanoBytes,
        string carpetaRelativa,
        CancellationToken cancellationToken = default)
    {
        if (contenido is null || contenido == Stream.Null)
        {
            throw new ArgumentException("El archivo es obligatorio.", nameof(contenido));
        }

        if (string.IsNullOrWhiteSpace(nombreOriginal))
        {
            throw new ArgumentException("El nombre del archivo es obligatorio.", nameof(nombreOriginal));
        }

        if (tamanoBytes <= 0 || tamanoBytes > TamanoMaximoBytes)
        {
            throw new ArgumentException("El archivo debe ser mayor a 0 bytes y no superar 10 MB.");
        }

        var nombreLimpio = nombreOriginal.Trim();
        if (!string.Equals(Path.GetFileName(nombreLimpio), nombreLimpio, StringComparison.Ordinal))
        {
            throw new ArgumentException("El nombre del archivo no es valido.", nameof(nombreOriginal));
        }

        var extension = Path.GetExtension(nombreLimpio);

        if (!FormatosPermitidos.TryGetValue(extension, out var mimeTypeEsperado))
        {
            throw new ArgumentException("El tipo de archivo no esta permitido.");
        }

        if (!string.IsNullOrWhiteSpace(mimeType)
            && !string.Equals(mimeType.Trim(), mimeTypeEsperado, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("El tipo declarado del archivo no coincide con su extensión.");
        }

        var carpetaSegura = NormalizarRutaRelativa(carpetaRelativa);
        var nombreAlmacenado = $"{DateTime.UtcNow:yyyyMMddHHmmssfff}-{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
        var rutaRelativa = Path.Combine(carpetaSegura, nombreAlmacenado).Replace('\\', '/');
        var rutaCompleta = ObtenerRutaCompleta(rutaRelativa);

        Directory.CreateDirectory(Path.GetDirectoryName(rutaCompleta)!);

        try
        {
            await CopiarYValidarContenidoAsync(
                contenido,
                rutaCompleta,
                extension,
                tamanoBytes,
                cancellationToken);
        }
        catch
        {
            if (File.Exists(rutaCompleta))
            {
                File.Delete(rutaCompleta);
            }

            throw;
        }

        return new ArchivoGuardado(nombreAlmacenado, rutaRelativa, mimeTypeEsperado, tamanoBytes);
    }

    public Task<Stream> AbrirLecturaAsync(string rutaRelativa, CancellationToken cancellationToken = default)
    {
        var rutaCompleta = ObtenerRutaCompleta(rutaRelativa);

        if (!File.Exists(rutaCompleta))
        {
            throw new FileNotFoundException("No se encontro el archivo solicitado.");
        }

        Stream archivo = File.OpenRead(rutaCompleta);
        return Task.FromResult(archivo);
    }

    public Task EliminarAsync(string rutaRelativa, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var rutaCompleta = ObtenerRutaCompleta(rutaRelativa);

        if (File.Exists(rutaCompleta))
        {
            File.Delete(rutaCompleta);
        }

        return Task.CompletedTask;
    }

    private string ObtenerRutaCompleta(string rutaRelativa)
    {
        var rutaNormalizada = NormalizarRutaRelativa(rutaRelativa);
        var rutaCompleta = Path.GetFullPath(Path.Combine(_rutaBase, rutaNormalizada));

        if (!rutaCompleta.StartsWith(_rutaBase, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("La ruta del archivo no es valida.");
        }

        return rutaCompleta;
    }

    private static string NormalizarRutaRelativa(string rutaRelativa)
    {
        if (string.IsNullOrWhiteSpace(rutaRelativa))
        {
            throw new ArgumentException("La ruta relativa es obligatoria.", nameof(rutaRelativa));
        }

        var limpia = rutaRelativa.Replace('\\', Path.DirectorySeparatorChar)
            .Replace('/', Path.DirectorySeparatorChar)
            .Trim(Path.DirectorySeparatorChar);

        if (limpia.Contains("..", StringComparison.Ordinal))
        {
            throw new InvalidOperationException("La ruta relativa no es valida.");
        }

        return limpia;
    }

    private static string ObtenerRutaBase(string rutaBase)
    {
        var ruta = string.IsNullOrWhiteSpace(rutaBase)
            ? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "SIGE-CDC",
                "archivos-protegidos")
            : rutaBase;

        return Path.GetFullPath(ruta).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
    }

    private static async Task CopiarYValidarContenidoAsync(
        Stream origen,
        string rutaCompleta,
        string extension,
        long tamanoInformado,
        CancellationToken cancellationToken)
    {
        var encabezado = new byte[8];
        var cantidadEncabezado = 0;
        byte penultimo = 0;
        byte ultimo = 0;
        var total = 0L;
        var buffer = new byte[81920];

        await using var destino = new FileStream(
            rutaCompleta,
            FileMode.CreateNew,
            FileAccess.Write,
            FileShare.None,
            buffer.Length,
            FileOptions.Asynchronous | FileOptions.SequentialScan);

        while (true)
        {
            var leidos = await origen.ReadAsync(buffer, cancellationToken);
            if (leidos == 0)
            {
                break;
            }

            total += leidos;
            if (total > TamanoMaximoBytes)
            {
                throw new ArgumentException("El archivo no puede superar 10 MB.");
            }

            var porCopiar = Math.Min(encabezado.Length - cantidadEncabezado, leidos);
            if (porCopiar > 0)
            {
                buffer.AsSpan(0, porCopiar).CopyTo(encabezado.AsSpan(cantidadEncabezado));
                cantidadEncabezado += porCopiar;
            }

            for (var indice = 0; indice < leidos; indice++)
            {
                penultimo = ultimo;
                ultimo = buffer[indice];
            }

            await destino.WriteAsync(buffer.AsMemory(0, leidos), cancellationToken);
        }

        if (total != tamanoInformado)
        {
            throw new ArgumentException("El tamaño informado no coincide con el contenido recibido.");
        }

        if (!FirmaValida(extension, encabezado, cantidadEncabezado, penultimo, ultimo))
        {
            throw new ArgumentException("El contenido del archivo no coincide con su formato.");
        }
    }

    private static bool FirmaValida(
        string extension,
        byte[] encabezado,
        int cantidadEncabezado,
        byte penultimo,
        byte ultimo) =>
        extension.ToLowerInvariant() switch
        {
            ".pdf" => cantidadEncabezado >= 5
                && encabezado.AsSpan(0, 5).SequenceEqual("%PDF-"u8),
            ".jpg" or ".jpeg" => cantidadEncabezado >= 3
                && encabezado[0] == 0xFF
                && encabezado[1] == 0xD8
                && encabezado[2] == 0xFF
                && penultimo == 0xFF
                && ultimo == 0xD9,
            ".png" => cantidadEncabezado >= 8
                && encabezado.AsSpan(0, 8).SequenceEqual(
                    new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }),
            ".doc" => cantidadEncabezado >= 8
                && encabezado.AsSpan(0, 8).SequenceEqual(
                    new byte[] { 0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1 }),
            ".docx" => cantidadEncabezado >= 4
                && encabezado[0] == 0x50
                && encabezado[1] == 0x4B
                && encabezado[2] == 0x03
                && encabezado[3] == 0x04,
            _ => false
        };
}
