using System.ComponentModel.DataAnnotations;
using SIGECDC.Application.Forecast;
using SIGECDC.Domain.Forecast;

namespace SIGECDC.Tests.Forecast;

public sealed class FiltroResultadosForecastTests
{
    [Fact]
    public void DimensionesOficiales_SonValidasYConservanOrden()
    {
        Assert.Equal(
            [
                DimensionesResultadoForecast.Colaborador,
                DimensionesResultadoForecast.Departamento,
                DimensionesResultadoForecast.Proyecto,
                DimensionesResultadoForecast.Periodo
            ],
            DimensionesResultadoForecast.Todas);

        foreach (var dimension in DimensionesResultadoForecast.Todas)
        {
            Assert.Empty(Validar(new FiltroResultadosForecast { Dimension = dimension }));
        }
    }

    [Fact]
    public void DimensionDesconocidaYPeriodoNoPositivo_SonRechazados()
    {
        var resultados = Validar(new FiltroResultadosForecast
        {
            Dimension = "Costo",
            IdForecastPeriodo = 0
        });

        Assert.Contains(resultados, item => item.MemberNames.Contains(nameof(FiltroResultadosForecast.Dimension)));
        Assert.Contains(resultados, item => item.MemberNames.Contains(nameof(FiltroResultadosForecast.IdForecastPeriodo)));
    }

    [Fact]
    public void CatalogoConceptos_ContieneExactamenteLosConceptosPersistidos()
    {
        Assert.Equal(
            [
                ConceptosForecast.SalarioProporcional,
                ConceptosForecast.HorasExtra,
                ConceptosForecast.Bonos,
                ConceptosForecast.BeneficiosConfigurables,
                ConceptosForecast.Ausencias,
                ConceptosForecast.SalarioBruto,
                ConceptosForecast.Deducciones,
                ConceptosForecast.SalarioNeto
            ],
            CatalogoConceptosResultadoForecast.Todos.Select(item => item.Codigo));
        Assert.All(
            CatalogoConceptosResultadoForecast.Todos,
            item => Assert.False(string.IsNullOrWhiteSpace(item.Nombre)));
    }

    private static IReadOnlyList<ValidationResult> Validar(FiltroResultadosForecast filtro)
    {
        var resultados = new List<ValidationResult>();
        Validator.TryValidateObject(
            filtro,
            new ValidationContext(filtro),
            resultados,
            true);
        return resultados;
    }
}
