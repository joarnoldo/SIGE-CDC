namespace SIGECDC.Domain.Forecast;

public sealed class ForecastComparacionReal
{
    public long IdForecastComparacionReal { get; set; }
    public long IdForecastPeriodo { get; set; }
    public long? IdEjecucionPlanillaReal { get; set; }
    public DateTime FechaEvaluacion { get; set; }
    public string EvaluadoPor { get; set; } = string.Empty;
    public string Disponibilidad { get; set; } = string.Empty;
    public string? MotivoNoComparable { get; set; }
    public string? EstadoPlanillaObservado { get; set; }
    public string? EstadoPeriodoObservado { get; set; }
    public string? EstadoEjecucionObservado { get; set; }
    public bool? SnapshotCompletoObservado { get; set; }
}

public sealed class ForecastComparacionResultado
{
    public long IdForecastComparacionResultado { get; set; }
    public long IdForecastComparacionReal { get; set; }
    public string Granularidad { get; set; } = string.Empty;
    public long? IdForecastParticipante { get; set; }
    public long? IdColaborador { get; set; }
    public long? IdConceptoPlanilla { get; set; }
    public long? IdProyecto { get; set; }
    public bool EsSinAsignar { get; set; }
    public bool TieneForecast { get; set; }
    public bool TieneReal { get; set; }
    public decimal? MontoForecast { get; set; }
    public decimal? MontoReal { get; set; }
    public decimal? DiferenciaFirmada { get; set; }
    public decimal? DiferenciaAbsoluta { get; set; }
    public decimal? DiferenciaPorcentual { get; set; }
    public string? MotivoNoComparable { get; set; }
    public int NumeroOrden { get; set; }
}

public sealed class ForecastComparacionResultadoFuente
{
    public long IdForecastComparacionResultadoFuente { get; set; }
    public long IdForecastComparacionResultado { get; set; }
    public string Lado { get; set; } = string.Empty;
    public long? IdForecastDetalle { get; set; }
    public long? IdDetallePlanilla { get; set; }
    public long? IdConceptoDetallePlanilla { get; set; }
    public long? IdDetallePlanillaDistribucionProyecto { get; set; }
}


