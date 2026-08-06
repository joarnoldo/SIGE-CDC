using System.ComponentModel.DataAnnotations;
using SIGECDC.Application.Forecast;

namespace SIGECDC.Tests.Forecast;

public sealed class SolicitudCompararForecastRealTests
{
    [Fact]
    public void DimensionesOficiales_ConservanAlcanceAprobado()
    {
        Assert.Equal(
            [
                DimensionesComparacionRealForecast.Periodo,
                DimensionesComparacionRealForecast.Colaborador,
                DimensionesComparacionRealForecast.Departamento,
                DimensionesComparacionRealForecast.Proyecto
            ],
            DimensionesComparacionRealForecast.Todas);
        Assert.All(DimensionesComparacionRealForecast.Todas, dimension => Assert.Empty(
            Validar(new SolicitudCompararForecastReal
            {
                Dimension = dimension,
                IdForecastPeriodo = DimensionesComparacionRealForecast.RequierePeriodo(dimension)
                    ? 1
                    : null
            })));
    }

    [Theory]
    [InlineData(DimensionesComparacionRealForecast.Colaborador)]
    [InlineData(DimensionesComparacionRealForecast.Departamento)]
    [InlineData(DimensionesComparacionRealForecast.Proyecto)]
    public void DimensionesDeDetalle_ExigenPeriodoPositivo(string dimension)
    {
        var sinPeriodo = Validar(new SolicitudCompararForecastReal
        {
            Dimension = dimension
        });
        var periodoInvalido = Validar(new SolicitudCompararForecastReal
        {
            Dimension = dimension,
            IdForecastPeriodo = 0
        });

        Assert.Contains(sinPeriodo, item => item.MemberNames.Contains(
            nameof(SolicitudCompararForecastReal.IdForecastPeriodo)));
        Assert.Contains(periodoInvalido, item => item.MemberNames.Contains(
            nameof(SolicitudCompararForecastReal.IdForecastPeriodo)));
    }

    [Fact]
    public void Periodo_RechazaFiltroIndividual()
    {
        var errores = Validar(new SolicitudCompararForecastReal
        {
            Dimension = DimensionesComparacionRealForecast.Periodo,
            IdForecastPeriodo = 1
        });

        Assert.Single(errores);
        Assert.Contains(nameof(SolicitudCompararForecastReal.IdForecastPeriodo),
            errores[0].MemberNames);
    }

    [Fact]
    public void DimensionFueraDeAlcance_EsRechazada()
    {
        var errores = Validar(new SolicitudCompararForecastReal
        {
            Dimension = "DimensionManipulada"
        });

        Assert.Contains(errores, item => item.MemberNames.Contains(
            nameof(SolicitudCompararForecastReal.Dimension)));
    }

    private static List<ValidationResult> Validar(object instancia)
    {
        var resultados = new List<ValidationResult>();
        Validator.TryValidateObject(
            instancia,
            new ValidationContext(instancia),
            resultados,
            true);
        return resultados;
    }
}
