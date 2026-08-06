namespace SIGECDC.Application.Forecast;

public interface IForecastMovimientoPersonalService
{
    Task<ConfiguracionMovimientosForecast?> ObtenerConfiguracionAsync(
        long idForecastEscenario,
        string idUsuarioActual,
        CancellationToken cancellationToken = default);

    Task<long> RegistrarContratacionPrevistaAsync(
        long idForecastEscenario,
        SolicitudContratacionPrevistaForecast solicitud,
        string idUsuarioActual,
        CancellationToken cancellationToken = default);

    Task ActualizarContratacionPrevistaAsync(
        long idForecastEscenario,
        long idForecastParticipante,
        SolicitudContratacionPrevistaForecast solicitud,
        string idUsuarioActual,
        CancellationToken cancellationToken = default);

    Task DesactivarContratacionPrevistaAsync(
        long idForecastEscenario,
        long idForecastParticipante,
        string idUsuarioActual,
        CancellationToken cancellationToken = default);

    Task GuardarSalidaPrevistaAsync(
        long idForecastEscenario,
        long idForecastParticipante,
        DateTime? fechaSalidaPrevista,
        string idUsuarioActual,
        CancellationToken cancellationToken = default);
}
