namespace SIGECDC.Application.Forecast;

public interface IForecastParametroService
{
    Task<ConfiguracionParametrosForecast?> ObtenerConfiguracionAsync(
        long idForecastEscenario,
        string idUsuarioActual,
        CancellationToken cancellationToken = default);

    Task GuardarConfiguracionAsync(
        long idForecastEscenario,
        SolicitudGuardarParametrosForecast solicitud,
        string idUsuarioActual,
        CancellationToken cancellationToken = default);
}
