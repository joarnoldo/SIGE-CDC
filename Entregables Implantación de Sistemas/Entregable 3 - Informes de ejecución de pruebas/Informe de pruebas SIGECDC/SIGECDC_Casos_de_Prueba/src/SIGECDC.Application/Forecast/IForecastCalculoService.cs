namespace SIGECDC.Application.Forecast;

public interface IForecastCalculoService
{
    Task<ResultadoGeneracionForecast> GenerarForecastAsync(
        SolicitudCalculoFinancieroForecast solicitud,
        string idUsuarioActual,
        CancellationToken cancellationToken = default);

    Task<ResultadoGeneracionForecast> GenerarForecastAsync(
        long idForecastEscenario,
        string idUsuarioActual,
        CancellationToken cancellationToken = default);
}
