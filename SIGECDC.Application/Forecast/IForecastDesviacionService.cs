namespace SIGECDC.Application.Forecast;

public interface IForecastDesviacionService
{
    Task<AnalisisDesviacionesForecast?> CalcularAsync(
        long idForecastEscenario,
        SolicitudCompararForecastReal solicitud,
        string idUsuarioActual,
        CancellationToken cancellationToken = default);
}
