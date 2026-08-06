namespace SIGECDC.Application.Forecast;

public interface IForecastEscenarioService
{
    Task<IReadOnlyList<PlanillaHistoricaForecastOpcion>> ObtenerFuentesHistoricasDisponiblesAsync(
        string idUsuarioActual,
        CancellationToken cancellationToken = default);

    Task<long> CrearBorradorAsync(
        SolicitudCrearEscenarioForecast solicitud,
        string idUsuarioActual,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<EscenarioForecastResumen>> ObtenerEscenariosAsync(
        string idUsuarioActual,
        CancellationToken cancellationToken = default);

    Task<EscenarioForecastDetalle?> ObtenerDetalleAsync(
        long idForecastEscenario,
        string idUsuarioActual,
        CancellationToken cancellationToken = default);

    Task GuardarAsync(
        long idForecastEscenario,
        string idUsuarioActual,
        CancellationToken cancellationToken = default);
}
