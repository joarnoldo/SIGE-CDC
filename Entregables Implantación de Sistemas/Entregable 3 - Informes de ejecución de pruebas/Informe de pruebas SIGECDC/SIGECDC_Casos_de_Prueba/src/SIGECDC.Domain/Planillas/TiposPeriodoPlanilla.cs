namespace SIGECDC.Domain.Planillas;

public static class TiposPeriodoPlanilla
{
    public const string Mensual = "Mensual";
    public const string Quincenal = "Quincenal";

    public static readonly IReadOnlySet<string> Permitidos = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        Mensual,
        Quincenal
    };
}
