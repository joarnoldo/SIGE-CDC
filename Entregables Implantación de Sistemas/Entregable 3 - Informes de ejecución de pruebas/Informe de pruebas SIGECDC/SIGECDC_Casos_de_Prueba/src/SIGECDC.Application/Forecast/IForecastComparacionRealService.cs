namespace SIGECDC.Application.Forecast;

public interface IForecastComparacionRealService
{
    Task<DisponibilidadComparacionRealForecast?> ObtenerDisponibilidadAsync(
        long idForecastEscenario,
        string idUsuarioActual,
        CancellationToken cancellationToken = default);

    Task<ComparacionForecastReal?> CompararAsync(
        long idForecastEscenario,
        SolicitudCompararForecastReal solicitud,
        string idUsuarioActual,
        CancellationToken cancellationToken = default);
}
