namespace SIGECDC.Application.Archivos;

public interface IAlmacenamientoArchivosService
{
    Task<ArchivoGuardado> GuardarAsync(
        Stream contenido,
        string nombreOriginal,
        string? mimeType,
        long tamanoBytes,
        string carpetaRelativa,
        CancellationToken cancellationToken = default);

    Task<Stream> AbrirLecturaAsync(string rutaRelativa, CancellationToken cancellationToken = default);

    Task EliminarAsync(string rutaRelativa, CancellationToken cancellationToken = default);
}
