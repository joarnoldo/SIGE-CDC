namespace SIGECDC.Domain.Planillas;

public sealed class ParametroPlanillaPeriodo
{
    public long IdParametroPlanillaPeriodo { get; set; }

    public int IdParametroPlanilla { get; set; }

    public long IdPeriodoPlanilla { get; set; }

    public decimal? ValorDecimalOverride { get; set; }

    public string? ValorTextoOverride { get; set; }

    public DateTime FechaCreacion { get; set; }

    public string? CreadoPor { get; set; }

    public DateTime? FechaModificacion { get; set; }

    public string? ModificadoPor { get; set; }

    public string EstadoRegistro { get; set; } = "Activo";

    public ParametroPlanilla? ParametroPlanilla { get; set; }

    public PeriodoPlanilla? PeriodoPlanilla { get; set; }
}
