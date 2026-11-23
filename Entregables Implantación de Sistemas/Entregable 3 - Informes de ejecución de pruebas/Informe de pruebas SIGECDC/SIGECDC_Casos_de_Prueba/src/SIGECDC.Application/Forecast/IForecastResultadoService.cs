namespace SIGECDC.Application.Forecast;

public interface IForecastResultadoService
{
    Task<ConsultaResultadosForecast?> ObtenerResultadosAsync(
        long idForecastEscenario,
        FiltroResultadosForecast filtro,
        string idUsuarioActual,
        CancellationToken cancellationToken = default);
}
