namespace SIGECDC.Domain.Planillas;

public static class NaturalezasParametroPlanilla
{
    public const string Deduccion = "Deduccion";
    public const string Beneficio = "Beneficio";

    public static readonly IReadOnlySet<string> Permitidas = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        Deduccion,
        Beneficio
    };
}
