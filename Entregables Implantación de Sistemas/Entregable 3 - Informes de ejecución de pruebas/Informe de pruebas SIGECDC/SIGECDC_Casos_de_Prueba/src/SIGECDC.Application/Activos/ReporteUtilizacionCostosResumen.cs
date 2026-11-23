namespace SIGECDC.Application.Activos;

public sealed class ReporteUtilizacionCostosResumen
{
    public EncabezadoReporteUtilizacionCostos Encabezado { get; set; } = new();

    public DateTime? FechaDesde { get; set; }

    public DateTime? FechaHasta { get; set; }

    public decimal CostoEstimadoTotal { get; set; }

    public decimal CostoRealTotal { get; set; }

    public IReadOnlyList<UtilizacionPorMedicionResumen> TotalesUtilizacion { get; set; } = [];

    public IReadOnlyList<RegistroUsoReporteResumen> RegistrosUso { get; set; } = [];

    public IReadOnlyList<MantenimientoCostoReporteResumen> Mantenimientos { get; set; } = [];
}
