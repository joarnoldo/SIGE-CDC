using SIGECDC.Domain.Forecast;

namespace SIGECDC.Application.Forecast;

public sealed class AnalisisDesviacionesForecast
{
    public ComparacionForecastReal Comparacion { get; init; } = new();
    public MetricasDesviacionForecast Totales { get; init; } = new();
    public IReadOnlyList<FilaDesviacionForecast> Filas { get; init; } = [];
}

public sealed class FilaDesviacionForecast
{
    public string Clave { get; init; } = string.Empty;
    public string Codigo { get; init; } = string.Empty;
    public string Etiqueta { get; init; } = string.Empty;
    public string? Detalle { get; init; }
    public bool TieneForecast { get; init; }
    public bool TieneReal { get; init; }
    public bool EsContratacionPrevista { get; init; }
    public MetricasDesviacionForecast Metricas { get; init; } = new();
}

public sealed class MetricasDesviacionForecast
{
    public ResultadoDesviacionForecast SalarioBruto { get; init; } =
        CalculadoraDesviacionForecast.Calcular(null, null);

    public ResultadoDesviacionForecast Deducciones { get; init; } =
        CalculadoraDesviacionForecast.Calcular(null, null);

    public ResultadoDesviacionForecast SalarioNeto { get; init; } =
        CalculadoraDesviacionForecast.Calcular(null, null);
}
