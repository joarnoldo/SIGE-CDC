namespace SIGECDC.Domain.Planillas;

public static class AmbitosAsignacionParametroPlanilla
{
    public const string Colaborador = "Colaborador";
    public const string Periodo = "Periodo";

    public static readonly IReadOnlySet<string> Permitidos = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        Colaborador,
        Periodo
    };
}
