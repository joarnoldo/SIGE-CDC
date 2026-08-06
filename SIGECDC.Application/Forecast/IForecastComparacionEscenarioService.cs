namespace SIGECDC.Application.Forecast;

public interface IForecastComparacionEscenarioService
{
    Task<IReadOnlyList<EscenarioComparacionForecastOpcion>> ObtenerEscenariosElegiblesAsync(
        string idUsuarioActual,
        CancellationToken cancellationToken = default);

    Task<ComparacionEscenariosForecast> CompararAsync(
        SolicitudCompararEscenariosForecast solicitud,
        string idUsuarioActual,
        CancellationToken cancellationToken = default);
}
