namespace SIGECDC.Domain.RecursosHumanos;

public class DocumentoArchivo
{
    public long IdDocumentoArchivo { get; set; }

    public string EntidadRelacionada { get; set; } = string.Empty;

    public long IdEntidadRelacionada { get; set; }

    public int IdTipoDocumento { get; set; }

    public string NombreOriginal { get; set; } = string.Empty;

    public string NombreAlmacenado { get; set; } = string.Empty;

    public string RutaRelativa { get; set; } = string.Empty;

    public string? MimeType { get; set; }

    public long? TamanoBytes { get; set; }

    public DateTime FechaCarga { get; set; }

    public string? CargadoPor { get; set; }

    public string EstadoRegistro { get; set; } = "Activo";

    public TipoDocumento? TipoDocumento { get; set; }
}
