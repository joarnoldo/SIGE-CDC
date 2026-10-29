using SIGECDC.Domain.SitioPublico;
using SIGECDC.Domain.Planillas;

namespace SIGECDC.Domain.Forecast;

public sealed class ForecastDetalle
{
    public long IdForecastDetalle { get; set; }

    public long IdForecastEscenario { get; set; }

    public long IdForecastPeriodo { get; set; }

    public long IdForecastParticipante { get; set; }

    public long? IdProyecto { get; set; }

    public long? IdForecastAsignacionProyecto { get; set; }

    public long? IdConceptoPlanillaVersion { get; set; }

    public string Concepto { get; set; } = string.Empty;

    public decimal MontoBase { get; set; }

    public decimal MontoAjuste { get; set; }

    public decimal MontoProyectado { get; set; }

    public decimal? MontoReal { get; set; }

    public decimal? Diferencia { get; set; }

    public decimal? MontoBaseSinRedondear { get; set; }

    public decimal? MontoProyectadoSinRedondear { get; set; }

    public int CantidadObservaciones { get; set; }

    public bool HistoricoLimitado { get; set; }

    public string? VersionMotor { get; set; }

    public decimal? DiferenciaFirmada { get; set; }

    public decimal? DiferenciaAbsoluta { get; set; }

    public decimal? DiferenciaPorcentual { get; set; }

    public string? Observaciones { get; set; }

    public string EstadoRegistro { get; set; } = EstadosRegistro.Activo;

    public ForecastAsignacionProyecto? ForecastAsignacionProyecto { get; set; }

    public ConceptoPlanillaVersion? ConceptoPlanillaVersion { get; set; }
}
