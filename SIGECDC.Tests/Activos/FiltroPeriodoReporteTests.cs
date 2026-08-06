using System.ComponentModel.DataAnnotations;
using SIGECDC.Application.Activos;

namespace SIGECDC.Tests.Activos;

public sealed class FiltroPeriodoReporteTests
{
    [Fact]
    public void Validar_PeriodoVacio_NoReportaErrores()
    {
        Assert.Empty(Validar(new FiltroPeriodoReporte()));
    }

    [Fact]
    public void Validar_SoloFechaInicial_NoReportaErrores()
    {
        var filtro = new FiltroPeriodoReporte
        {
            FechaDesde = new DateTime(2026, 7, 1)
        };

        Assert.Empty(Validar(filtro));
    }

    [Fact]
    public void Validar_SoloFechaFinal_NoReportaErrores()
    {
        var filtro = new FiltroPeriodoReporte
        {
            FechaHasta = new DateTime(2026, 7, 31)
        };

        Assert.Empty(Validar(filtro));
    }

    [Fact]
    public void Validar_MismoDia_NoReportaErrores()
    {
        var filtro = new FiltroPeriodoReporte
        {
            FechaDesde = new DateTime(2026, 7, 15, 8, 0, 0),
            FechaHasta = new DateTime(2026, 7, 15, 18, 0, 0)
        };

        Assert.Empty(Validar(filtro));
    }

    [Fact]
    public void Validar_RangoCorrecto_NoReportaErrores()
    {
        var filtro = new FiltroPeriodoReporte
        {
            FechaDesde = new DateTime(2026, 7, 1),
            FechaHasta = new DateTime(2026, 7, 31)
        };

        Assert.Empty(Validar(filtro));
    }

    [Fact]
    public void Validar_RangoInvertido_ReportaAmbasFechas()
    {
        var filtro = new FiltroPeriodoReporte
        {
            FechaDesde = new DateTime(2026, 8, 1),
            FechaHasta = new DateTime(2026, 7, 31)
        };

        var resultado = Assert.Single(Validar(filtro));

        Assert.Contains(
            nameof(filtro.FechaDesde),
            resultado.MemberNames);
        Assert.Contains(
            nameof(filtro.FechaHasta),
            resultado.MemberNames);
    }

    private static IReadOnlyCollection<ValidationResult> Validar(
        FiltroPeriodoReporte filtro)
    {
        var resultados = new List<ValidationResult>();
        Validator.TryValidateObject(
            filtro,
            new ValidationContext(filtro),
            resultados,
            validateAllProperties: true);
        return resultados;
    }
}
