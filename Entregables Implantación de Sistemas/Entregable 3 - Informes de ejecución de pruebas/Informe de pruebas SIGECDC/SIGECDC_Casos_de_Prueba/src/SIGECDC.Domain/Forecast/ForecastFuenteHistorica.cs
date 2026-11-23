using SIGECDC.Domain.Planillas;

namespace SIGECDC.Domain.Forecast;

public sealed class ForecastFuenteHistorica
{
    public long IdForecastFuenteHistorica { get; set; }

    public long IdForecastEscenario { get; set; }

    public long IdPlanilla { get; set; }

    public int NumeroOrden { get; set; }

    public ForecastEscenario? ForecastEscenario { get; set; }

    public Planilla? Planilla { get; set; }
}
