namespace SIGECDC.Application.Auditoria;

public static class CategoriasBitacoraOperaciones
{
    public const string Activo = "Activo";
    public const string Proyecto = "Proyecto";
    public const string Asignacion = "Asignación";
    public const string Mantenimiento = "Mantenimiento";

    public static IReadOnlyList<string> Todas { get; } =
    [
        Activo,
        Proyecto,
        Asignacion,
        Mantenimiento
    ];

    public static bool EsValida(string? categoria)
    {
        return string.IsNullOrWhiteSpace(categoria)
            || Todas.Contains(categoria.Trim(), StringComparer.Ordinal);
    }
}
