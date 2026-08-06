using SIGECDC.Application.RecursosHumanos;

namespace SIGECDC.Application.Forecast;

public sealed class ConfiguracionMovimientosForecast
{
    public long IdForecastEscenario { get; set; }
    public string NombreEscenario { get; set; } = string.Empty;
    public string EstadoEscenario { get; set; } = string.Empty;
    public DateTime FechaInicioProyeccion { get; set; }
    public DateTime FechaFinProyeccion { get; set; }
    public bool PuedeEditar { get; set; }
    public IReadOnlyList<PeriodoForecastResumen> Periodos { get; set; } = [];
    public IReadOnlyList<OpcionCatalogo> Departamentos { get; set; } = [];
    public IReadOnlyList<PuestoForecastOpcion> Puestos { get; set; } = [];
    public IReadOnlyList<ContratacionPrevistaForecastResumen> Contrataciones { get; set; } = [];
    public IReadOnlyList<ColaboradorSalidaPrevistaForecast> Colaboradores { get; set; } = [];
}
