using SIGECDC.Domain.Forecast;

namespace SIGECDC.Application.Forecast;

public sealed class ForecastDesviacionService(
    IForecastComparacionRealService comparacionRealService)
    : IForecastDesviacionService
{
    public async Task<AnalisisDesviacionesForecast?> CalcularAsync(
        long idForecastEscenario,
        SolicitudCompararForecastReal solicitud,
        string idUsuarioActual,
        CancellationToken cancellationToken = default)
    {
        var comparacion = await comparacionRealService.CompararAsync(
            idForecastEscenario,
            solicitud,
            idUsuarioActual,
            cancellationToken);
        if (comparacion is null)
        {
            return null;
        }

        return new AnalisisDesviacionesForecast
        {
            Comparacion = comparacion,
            Totales = CalcularMetricas(comparacion.TotalesComparables),
            Filas = comparacion.Filas.Select(item => new FilaDesviacionForecast
            {
                Clave = item.Clave,
                Codigo = item.Codigo,
                Etiqueta = item.Etiqueta,
                Detalle = item.Detalle,
                TieneForecast = item.TieneForecast,
                TieneReal = item.TieneReal,
                EsContratacionPrevista = item.EsContratacionPrevista,
                Metricas = CalcularMetricas(item.Valores)
            }).ToList()
        };
    }

    private static MetricasDesviacionForecast CalcularMetricas(
        ValoresForecastReal valores) => new()
    {
        SalarioBruto = CalculadoraDesviacionForecast.Calcular(
            valores.SalarioBrutoProyectado,
            valores.SalarioBrutoReal),
        Deducciones = CalculadoraDesviacionForecast.Calcular(
            valores.DeduccionesProyectadas,
            valores.DeduccionesReales),
        SalarioNeto = CalculadoraDesviacionForecast.Calcular(
            valores.SalarioNetoProyectado,
            valores.SalarioNetoReal)
    };
}
