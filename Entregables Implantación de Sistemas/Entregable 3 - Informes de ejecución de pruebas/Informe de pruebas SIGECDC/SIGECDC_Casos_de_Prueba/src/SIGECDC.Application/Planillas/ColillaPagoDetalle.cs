namespace SIGECDC.Application.Planillas;

public sealed class ColillaPagoDetalle : DetalleReportePlanilla
{
    public string CodigoColilla { get; set; } = string.Empty;
    public long IdPeriodoPlanilla { get; set; }
    public long IdEjecucionPlanilla { get; set; }
    public int NumeroVersion { get; set; }
    public string CodigoPeriodo { get; set; } = string.Empty;
    public string NombrePeriodo { get; set; } = string.Empty;
    public string TipoPeriodo { get; set; } = string.Empty;
    public DateTime FechaInicio { get; set; }
    public DateTime FechaFin { get; set; }
    public string EstadoPlanilla { get; set; } = string.Empty;
    public string Rotulo => PublicacionPlanilla.Rotulo(EstadoPlanilla);
    public DateTime FechaGeneracion { get; set; }
}
