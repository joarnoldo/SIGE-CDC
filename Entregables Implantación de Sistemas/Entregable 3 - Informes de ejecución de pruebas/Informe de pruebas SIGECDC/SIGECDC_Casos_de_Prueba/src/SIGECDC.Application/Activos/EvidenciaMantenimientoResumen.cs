namespace SIGECDC.Application.Activos;

public sealed class EvidenciaMantenimientoResumen
{
    public long IdDocumentoArchivo { get; set; }

    public long IdMantenimiento { get; set; }

    public string NombreOriginal { get; set; } = string.Empty;

    public string? MimeType { get; set; }

    public long? TamanoBytes { get; set; }

    public DateTime FechaCarga { get; set; }
}
