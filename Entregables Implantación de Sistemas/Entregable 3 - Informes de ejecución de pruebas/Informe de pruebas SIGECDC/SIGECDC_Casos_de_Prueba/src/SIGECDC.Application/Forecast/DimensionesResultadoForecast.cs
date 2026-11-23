namespace SIGECDC.Application.Forecast;

public static class DimensionesResultadoForecast
{
    public const string Colaborador = "Colaborador";
    public const string Departamento = "Departamento";
    public const string Proyecto = "Proyecto";
    public const string Periodo = "Periodo";

    public static readonly IReadOnlyList<string> Todas =
    [
        Colaborador,
        Departamento,
        Proyecto,
        Periodo
    ];

    public static bool EsValida(string? dimension) =>
        Todas.Contains(dimension, StringComparer.Ordinal);
}
