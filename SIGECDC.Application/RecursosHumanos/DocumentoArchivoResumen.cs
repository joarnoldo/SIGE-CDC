namespace SIGECDC.Application.RecursosHumanos;

public sealed class DocumentoArchivoResumen
{
    public long IdDocumentoArchivo { get; set; }

    public long IdColaborador { get; set; }

    public int IdTipoDocumento { get; set; }

    public string TipoDocumento { get; set; } = string.Empty;

    public string NombreOriginal { get; set; } = string.Empty;

    public string? MimeType { get; set; }

    public long? TamanoBytes { get; set; }

    public DateTime FechaCarga { get; set; }

    public string EstadoRegistro { get; set; } = string.Empty;
}
