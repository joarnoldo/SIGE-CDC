using SIGECDC.Domain.Planillas;

namespace SIGECDC.Domain.Forecast;

public sealed class ForecastObservacionHistorica
{
    public long IdForecastObservacionHistorica { get; set; }
    public long IdForecastEscenario { get; set; }
    public long IdForecastParticipante { get; set; }
    public long IdForecastFuenteHistorica { get; set; }
    public long? IdDetallePlanilla { get; set; }
    public long IdConceptoPlanillaVersion { get; set; }
    public decimal? MontoObservado { get; set; }
    public bool EsComparable { get; set; }
    public string? MotivoNoComparable { get; set; }

    public ForecastEscenario? ForecastEscenario { get; set; }
    public ForecastParticipante? ForecastParticipante { get; set; }
    public ForecastFuenteHistorica? ForecastFuenteHistorica { get; set; }
    public DetallePlanilla? DetallePlanilla { get; set; }
    public ConceptoPlanillaVersion? ConceptoPlanillaVersion { get; set; }
}
