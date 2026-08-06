using SIGECDC.Domain.SitioPublico;

namespace SIGECDC.Domain.Forecast;

public sealed class ForecastPeriodo
{
    public long IdForecastPeriodo { get; set; }

    public long IdForecastEscenario { get; set; }

    public int NumeroOrden { get; set; }

    public string TipoPeriodo { get; set; } = string.Empty;

    public DateTime FechaInicio { get; set; }

    public DateTime FechaFin { get; set; }

    public string EstadoRegistro { get; set; } = EstadosRegistro.Activo;

    public ForecastEscenario? ForecastEscenario { get; set; }

    public ICollection<ForecastAsignacionProyecto> AsignacionesProyecto { get; set; } = [];
}
