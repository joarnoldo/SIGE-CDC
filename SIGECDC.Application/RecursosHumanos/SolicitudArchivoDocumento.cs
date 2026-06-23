namespace SIGECDC.Application.RecursosHumanos;

public sealed class SolicitudArchivoDocumento : IDisposable
{
    public long IdColaborador { get; set; }

    public int IdTipoDocumento { get; set; }

    public string NombreOriginal { get; set; } = string.Empty;

    public string? MimeType { get; set; }

    public long TamanoBytes { get; set; }

    public Stream Contenido { get; set; } = Stream.Null;

    public void Dispose()
    {
        Contenido.Dispose();
    }
}
