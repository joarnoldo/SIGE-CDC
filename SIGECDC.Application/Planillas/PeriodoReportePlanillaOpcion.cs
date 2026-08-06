namespace SIGECDC.Application.Planillas;

public sealed class PeriodoReportePlanillaOpcion
{
    public long IdPeriodoPlanilla { get; set; }

    public string CodigoPeriodo { get; set; } = string.Empty;

    public string NombrePeriodo { get; set; } = string.Empty;

    public string TipoPeriodo { get; set; } = string.Empty;

    public DateTime FechaInicio { get; set; }

    public DateTime FechaFin { get; set; }

    public string EstadoPlanilla { get; set; } = string.Empty;
}
