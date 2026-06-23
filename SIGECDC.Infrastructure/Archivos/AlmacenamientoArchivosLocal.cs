using SIGECDC.Application.Archivos;

namespace SIGECDC.Infrastructure.Archivos;

public sealed class AlmacenamientoArchivosLocal(string rutaBase) : IAlmacenamientoArchivosService
{
    private const long TamanoMaximoBytes = 10 * 1024 * 1024;
    private static readonly HashSet<string> ExtensionesPermitidas = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf",
        ".doc",
        ".docx",
        ".jpg",
        ".jpeg",
        ".png"
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

        var extension = Path.GetExtension(nombreOriginal);

        if (!ExtensionesPermitidas.Contains(extension))
        {
            throw new ArgumentException("El tipo de archivo no esta permitido.");
        }

        var carpetaSegura = NormalizarRutaRelativa(carpetaRelativa);
        var nombreAlmacenado = $"{DateTime.UtcNow:yyyyMMddHHmmssfff}-{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
        var rutaRelativa = Path.Combine(carpetaSegura, nombreAlmacenado).Replace('\\', '/');
        var rutaCompleta = ObtenerRutaCompleta(rutaRelativa);

        Directory.CreateDirectory(Path.GetDirectoryName(rutaCompleta)!);

        await using var destino = File.Create(rutaCompleta);
        await contenido.CopyToAsync(destino, cancellationToken);

        return new ArchivoGuardado(nombreAlmacenado, rutaRelativa, mimeType, tamanoBytes);
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
}
