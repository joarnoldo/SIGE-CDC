namespace SIGECDC.Domain.SitioPublico;

public class FAQ
{
    public long IdFAQ { get; set; }

    public string Pregunta { get; set; } = string.Empty;

    public string Respuesta { get; set; } = string.Empty;

    public int Orden { get; set; }

    public bool EstaPublicado { get; set; }

    public DateTime FechaCreacion { get; set; }

    public string? CreadoPor { get; set; }

    public DateTime? FechaModificacion { get; set; }

    public string? ModificadoPor { get; set; }

    public string EstadoRegistro { get; set; } = EstadosRegistro.Activo;
}