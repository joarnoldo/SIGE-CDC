namespace SIGECDC.Application.Forecast;

public interface IForecastAsignacionProyectoService
{
    Task<ConfiguracionAsignacionesProyectoForecast?> ObtenerConfiguracionAsync(
        long idForecastEscenario,
        string idUsuarioActual,
        CancellationToken cancellationToken = default);

    Task GuardarDistribucionAsync(
        long idForecastEscenario,
        long idForecastParticipante,
        long idForecastPeriodo,
        SolicitudGuardarDistribucionForecast solicitud,
        string idUsuarioActual,
        CancellationToken cancellationToken = default);

    Task LimpiarDistribucionAsync(
        long idForecastEscenario,
        long idForecastParticipante,
        long idForecastPeriodo,
        string idUsuarioActual,
        CancellationToken cancellationToken = default);

    Task CopiarDistribucionAsync(
        long idForecastEscenario,
        long idForecastParticipante,
        long idForecastPeriodoOrigen,
        IReadOnlyList<long> idsPeriodosDestino,
        string idUsuarioActual,
        CancellationToken cancellationToken = default);
}
