namespace SIGECDC.Application.Forecast;

public interface IForecastCalculoService
{
    Task<ResultadoGeneracionForecast> GenerarForecastAsync(
        long idForecastEscenario,
        string idUsuarioActual,
        CancellationToken cancellationToken = default);
}
