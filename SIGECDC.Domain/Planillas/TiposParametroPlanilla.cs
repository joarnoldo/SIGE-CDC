namespace SIGECDC.Domain.Planillas;

public static class TiposParametroPlanilla
{
    public const string Porcentaje = "Porcentaje";
    public const string Monto = "Monto";
    public const string Cantidad = "Cantidad";
    public const string Texto = "Texto";

    public static readonly IReadOnlySet<string> Permitidos = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        Porcentaje,
        Monto,
        Cantidad,
        Texto
    };
}
