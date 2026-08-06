using SIGECDC.Domain.Operaciones;
using SIGECDC.Domain.SitioPublico;

namespace SIGECDC.Domain.Forecast;

public sealed class ForecastAsignacionProyecto
{
    public long IdForecastAsignacionProyecto { get; set; }
    public long IdForecastEscenario { get; set; }
    public long IdForecastPeriodo { get; set; }
    public long IdForecastParticipante { get; set; }
    public long IdProyecto { get; set; }
    public decimal Porcentaje { get; set; }
    public string EstadoRegistro { get; set; } = EstadosRegistro.Activo;

    public ForecastPeriodo? ForecastPeriodo { get; set; }
    public ForecastParticipante? ForecastParticipante { get; set; }
    public Proyecto? Proyecto { get; set; }

    public ICollection<ForecastDetalle> Detalles { get; set; } = [];
}
