namespace SIGECDC.Application.Archivos;

public sealed class ArchivoDescarga(Stream contenido, string nombreOriginal, string? mimeType)
{
    public Stream Contenido { get; } = contenido;

    public string NombreOriginal { get; } = nombreOriginal;

    public string MimeType { get; } = string.IsNullOrWhiteSpace(mimeType)
        ? "application/octet-stream"
        : mimeType;
}
