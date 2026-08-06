using SIGECDC.Application.Archivos;

namespace SIGECDC.Tests.Activos;

internal sealed class AlmacenamientoArchivosPrueba
    : IAlmacenamientoArchivosService
{
    private readonly Dictionary<string, byte[]> _archivos =
        new(StringComparer.OrdinalIgnoreCase);

    public int CantidadArchivos => _archivos.Count;

    public bool FallarAlGuardar { get; set; }

    public bool Existe(string rutaRelativa)
    {
        return _archivos.ContainsKey(rutaRelativa);
    }

    public async Task<ArchivoGuardado> GuardarAsync(
        Stream contenido,
        string nombreOriginal,
        string? mimeType,
        long tamanoBytes,
        string carpetaRelativa,
        CancellationToken cancellationToken = default)
    {
        if (FallarAlGuardar)
        {
            throw new IOException("Fallo simulado al guardar la evidencia.");
        }

        var extension = Path.GetExtension(nombreOriginal).ToLowerInvariant();
        var nombreAlmacenado = $"{Guid.NewGuid():N}{extension}";
        var rutaRelativa =
            $"{carpetaRelativa.TrimEnd('/', '\\')}/{nombreAlmacenado}";

        await using var copia = new MemoryStream();
        await contenido.CopyToAsync(copia, cancellationToken);
        _archivos.Add(rutaRelativa, copia.ToArray());

        return new ArchivoGuardado(
            nombreAlmacenado,
            rutaRelativa,
            mimeType,
            copia.Length);
    }

    public Task<Stream> AbrirLecturaAsync(
        string rutaRelativa,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!_archivos.TryGetValue(rutaRelativa, out var contenido))
        {
            throw new FileNotFoundException(
                "No se encontró el archivo solicitado.");
        }

        Stream resultado = new MemoryStream(contenido, writable: false);
        return Task.FromResult(resultado);
    }

    public Task EliminarAsync(
        string rutaRelativa,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _archivos.Remove(rutaRelativa);
        return Task.CompletedTask;
    }
}
