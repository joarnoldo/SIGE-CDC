namespace SIGECDC.Domain.Forecast;

public static class CalculadoraDesviacionForecast
{
    public static ResultadoDesviacionForecast Calcular(
        decimal? montoForecast,
        decimal? montoReal)
    {
        if (!montoForecast.HasValue || !montoReal.HasValue)
        {
            return new ResultadoDesviacionForecast(
                montoForecast,
                montoReal,
                null,
                null,
                MotivosDesviacionForecast.SinCorrespondencia);
        }

        var absolutaSinRedondear = Math.Abs(montoReal.Value - montoForecast.Value);
        var absoluta = Redondear(absolutaSinRedondear);
        if (montoForecast.Value == 0m)
        {
            return montoReal.Value == 0m
                ? new ResultadoDesviacionForecast(0m, 0m, 0m, 0m, null)
                : new ResultadoDesviacionForecast(
                    montoForecast,
                    montoReal,
                    absoluta,
                    null,
                    MotivosDesviacionForecast.BaseForecastCero);
        }

        var porcentual = Redondear(
            absolutaSinRedondear / Math.Abs(montoForecast.Value) * 100m);
        return new ResultadoDesviacionForecast(
            montoForecast,
            montoReal,
            absoluta,
            porcentual,
            null);
    }

    private static decimal Redondear(decimal valor) =>
        Math.Round(valor, 2, MidpointRounding.AwayFromZero);
}

public sealed record ResultadoDesviacionForecast(
    decimal? MontoForecast,
    decimal? MontoReal,
    decimal? DesviacionAbsoluta,
    decimal? DesviacionPorcentual,
    string? MotivoNoCalculable)
{
    public bool EsComparacionCalculable => DesviacionAbsoluta.HasValue;

    public bool EsPorcentajeCalculable => DesviacionPorcentual.HasValue;
}

public static class MotivosDesviacionForecast
{
    public const string SinCorrespondencia = "SinCorrespondencia";
    public const string BaseForecastCero = "BaseForecastCero";
}
