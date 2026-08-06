using SIGECDC.Domain.SitioPublico;

namespace SIGECDC.Domain.Forecast;

public sealed class ForecastDetalle
{
    public long IdForecastDetalle { get; set; }

    public long IdForecastEscenario { get; set; }

    public long IdForecastPeriodo { get; set; }

    public long IdForecastParticipante { get; set; }

    public long IdProyecto { get; set; }

    public string Concepto { get; set; } = string.Empty;

    public decimal MontoBase { get; set; }

    public decimal MontoAjuste { get; set; }

    public decimal MontoProyectado { get; set; }

    public decimal? MontoReal { get; set; }

    public decimal? Diferencia { get; set; }

    public string? Observaciones { get; set; }

    public string EstadoRegistro { get; set; } = EstadosRegistro.Activo;

    public ForecastAsignacionProyecto? ForecastAsignacionProyecto { get; set; }
}
