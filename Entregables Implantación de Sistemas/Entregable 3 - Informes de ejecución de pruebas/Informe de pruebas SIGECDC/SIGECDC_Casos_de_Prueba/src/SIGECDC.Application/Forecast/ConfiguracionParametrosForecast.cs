namespace SIGECDC.Application.Forecast;

public sealed class ConfiguracionParametrosForecast
{
    public long IdForecastEscenario { get; set; }

    public string NombreEscenario { get; set; } = string.Empty;

    public string EstadoEscenario { get; set; } = string.Empty;

    public bool PuedeEditar { get; set; }

    public IReadOnlyList<ParametroForecastConfigurado> Parametros { get; set; } = [];
}
