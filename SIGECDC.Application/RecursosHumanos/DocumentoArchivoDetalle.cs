namespace SIGECDC.Application.RecursosHumanos;

public sealed class DocumentoArchivoDetalle
{
    public long IdDocumentoArchivo { get; set; }

    public string EntidadRelacionada { get; set; } = string.Empty;

    public long IdEntidadRelacionada { get; set; }

    public int IdTipoDocumento { get; set; }

    public string TipoDocumento { get; set; } = string.Empty;

    public string NombreOriginal { get; set; } = string.Empty;

    public string NombreAlmacenado { get; set; } = string.Empty;

    public string RutaRelativa { get; set; } = string.Empty;

    public string? MimeType { get; set; }

    public long? TamanoBytes { get; set; }

    public DateTime FechaCarga { get; set; }

    public string EstadoRegistro { get; set; } = string.Empty;
}
