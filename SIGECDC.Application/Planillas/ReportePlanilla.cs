namespace SIGECDC.Application.Planillas;

public sealed class ReportePlanilla
{
    public long IdPeriodoPlanilla { get; set; }

    public string CodigoPeriodo { get; set; } = string.Empty;

    public string NombrePeriodo { get; set; } = string.Empty;

    public string TipoPeriodo { get; set; } = string.Empty;

    public DateTime FechaInicio { get; set; }

    public DateTime FechaFin { get; set; }

    public string EstadoPlanilla { get; set; } = string.Empty;

    public DateTime FechaCalculo { get; set; }

    public DateTime? FechaAprobacion { get; set; }

    public DateTime? FechaCierre { get; set; }

    public decimal SalarioBrutoTotal { get; set; }

    public decimal DeduccionesTotal { get; set; }

    public decimal SalarioNetoTotal { get; set; }

    public IReadOnlyList<DetalleReportePlanilla> Detalles { get; set; } = [];
}
