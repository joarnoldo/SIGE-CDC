namespace SIGECDC.Domain.Planillas;

public sealed class PeriodoPlanilla
{
    public long IdPeriodoPlanilla { get; set; }

    public string CodigoPeriodo { get; set; } = string.Empty;

    public string Nombre { get; set; } = string.Empty;

    public string TipoPeriodo { get; set; } = "Quincenal";

    public DateTime FechaInicio { get; set; }

    public DateTime FechaFin { get; set; }

    public int IdEstadoPlanilla { get; set; }

    public string? Observaciones { get; set; }

    public DateTime FechaCreacion { get; set; }

    public string? CreadoPor { get; set; }

    public DateTime? FechaModificacion { get; set; }

    public string? ModificadoPor { get; set; }

    public string EstadoRegistro { get; set; } = "Activo";

    public EstadoPlanilla? EstadoPlanilla { get; set; }
}
