namespace SIGECDC.Application.Forecast;

public interface IForecastParticipanteService
{
    Task<ConfiguracionParticipantesForecast?> ObtenerConfiguracionAsync(
        long idForecastEscenario,
        string idUsuarioActual,
        CancellationToken cancellationToken = default);

    Task GuardarComposicionAsync(
        long idForecastEscenario,
        SolicitudGuardarParticipantesForecast solicitud,
        string idUsuarioActual,
        CancellationToken cancellationToken = default);
}
