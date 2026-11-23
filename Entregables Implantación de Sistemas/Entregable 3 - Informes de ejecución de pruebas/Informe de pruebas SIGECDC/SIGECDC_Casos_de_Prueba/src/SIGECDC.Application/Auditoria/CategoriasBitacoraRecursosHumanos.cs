namespace SIGECDC.Application.Auditoria;

public static class CategoriasBitacoraRecursosHumanos
{
    public const string Colaborador = "Colaborador";
    public const string PeriodoPlanilla = "Período de planilla";
    public const string Planilla = "Planilla";

    public static IReadOnlyList<string> Todas { get; } =
    [
        Colaborador,
        PeriodoPlanilla,
        Planilla
    ];

    public static bool EsValida(string? categoria)
    {
        return string.IsNullOrWhiteSpace(categoria)
            || Todas.Contains(categoria.Trim(), StringComparer.Ordinal);
    }
}
