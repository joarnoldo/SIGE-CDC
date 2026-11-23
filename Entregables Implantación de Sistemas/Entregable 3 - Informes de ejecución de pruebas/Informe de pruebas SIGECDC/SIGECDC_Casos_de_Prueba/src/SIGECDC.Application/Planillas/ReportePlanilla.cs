namespace SIGECDC.Application.Planillas;

public sealed class ReportePlanilla
{
    public int ColillasGeneradas { get; set; }
    public long IdEjecucionPlanilla { get; set; }
    public int NumeroVersion { get; set; }
    public string Rotulo => PublicacionPlanilla.Rotulo(EstadoPlanilla);
    public decimal IngresosNoSalarialesTotal { get; set; }
    public decimal BrutoPagaderoTotal { get; set; }
    public decimal CargasPatronalesTotal { get; set; }
    public decimal RiesgosTrabajoTotal { get; set; }
    public decimal OtrosCostosPatronalesTotal { get; set; }
    public decimal CostoLaboralTotal { get; set; }
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
