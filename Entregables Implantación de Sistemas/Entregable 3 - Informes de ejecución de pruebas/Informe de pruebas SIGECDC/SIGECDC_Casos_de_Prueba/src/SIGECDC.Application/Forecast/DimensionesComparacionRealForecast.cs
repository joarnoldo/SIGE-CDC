namespace SIGECDC.Application.Forecast;

public static class DimensionesComparacionRealForecast
{
    public const string Periodo = "Periodo";
    public const string Colaborador = "Colaborador";
    public const string Departamento = "Departamento";
    public const string Proyecto = "Proyecto";

    public static readonly IReadOnlyList<string> Todas =
    [
        Periodo,
        Colaborador,
        Departamento,
        Proyecto
    ];

    public static bool EsValida(string? dimension) =>
        Todas.Contains(dimension, StringComparer.Ordinal);

    public static bool RequierePeriodo(string? dimension) =>
        dimension is Colaborador or Departamento or Proyecto;
}
