using SIGECDC.Domain.Planillas;

namespace SIGECDC.Domain.Forecast;

public static class CodigosParametroForecast
{
    public const string AjusteSalarial = "AJUSTE_SALARIAL";
    public const string HorasExtraEstimadas = "HORAS_EXTRA_ESTIMADAS";
    public const string BonosEstimados = "BONOS_ESTIMADOS";
    public const string DeduccionesRecurrentes = "DEDUCCIONES_RECURRENTES";

    public static IReadOnlyList<DefinicionParametroForecast> Definiciones { get; } =
    [
        new(
            AjusteSalarial,
            "Ajuste salarial",
            TiposParametroPlanilla.Porcentaje,
            [TiposParametroPlanilla.Porcentaje]),
        new(
            HorasExtraEstimadas,
            "Horas extra estimadas",
            TiposParametroPlanilla.Monto,
            [TiposParametroPlanilla.Porcentaje, TiposParametroPlanilla.Monto]),
        new(
            BonosEstimados,
            "Bonos estimados",
            TiposParametroPlanilla.Monto,
            [TiposParametroPlanilla.Porcentaje, TiposParametroPlanilla.Monto]),
        new(
            DeduccionesRecurrentes,
            "Deducciones recurrentes",
            TiposParametroPlanilla.Monto,
            [TiposParametroPlanilla.Porcentaje, TiposParametroPlanilla.Monto])
    ];

    public static DefinicionParametroForecast? Obtener(string? codigo) =>
        Definiciones.FirstOrDefault(definicion => string.Equals(
            definicion.Codigo,
            codigo?.Trim(),
            StringComparison.OrdinalIgnoreCase));
}

public sealed record DefinicionParametroForecast(
    string Codigo,
    string Nombre,
    string TipoInicial,
    IReadOnlyList<string> TiposPermitidos);
