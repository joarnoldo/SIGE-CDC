namespace SIGECDC.Application.Planillas;

public sealed class PeriodoPlanillaResumen
{
    public long IdPeriodoPlanilla { get; set; }

    public string CodigoPeriodo { get; set; } = string.Empty;

    public string Nombre { get; set; } = string.Empty;

    public string TipoPeriodo { get; set; } = string.Empty;

    public DateTime FechaInicio { get; set; }

    public DateTime FechaFin { get; set; }

    public string EstadoPlanilla { get; set; } = string.Empty;

    public int CantidadIncidencias { get; set; }

    public bool TienePlanilla { get; set; }

    public decimal SalarioNetoTotal { get; set; }
}
