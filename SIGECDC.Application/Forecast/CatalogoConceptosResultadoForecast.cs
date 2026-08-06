using SIGECDC.Domain.Forecast;

namespace SIGECDC.Application.Forecast;

public static class CatalogoConceptosResultadoForecast
{
    public static readonly IReadOnlyList<DefinicionConceptoResultadoForecast> Todos =
    [
        new(ConceptosForecast.SalarioProporcional, "Salario proporcional"),
        new(ConceptosForecast.HorasExtra, "Horas extra"),
        new(ConceptosForecast.Bonos, "Bonos"),
        new(ConceptosForecast.BeneficiosConfigurables, "Beneficios configurables"),
        new(ConceptosForecast.Ausencias, "Ausencias"),
        new(ConceptosForecast.SalarioBruto, "Salario bruto"),
        new(ConceptosForecast.Deducciones, "Deducciones"),
        new(ConceptosForecast.SalarioNeto, "Salario neto")
    ];
}

public sealed record DefinicionConceptoResultadoForecast(string Codigo, string Nombre);
