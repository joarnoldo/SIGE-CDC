using System.ComponentModel.DataAnnotations;
using SIGECDC.Application.Auditoria;

namespace SIGECDC.Tests.Auditoria;

public sealed class FiltroBitacoraRecursosHumanosTests
{
    [Fact]
    public void Validar_FiltroVacio_NoReportaErrores()
    {
        Assert.Empty(Validar(new FiltroBitacoraRecursosHumanos()));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Validar_PeriodoAbierto_NoReportaErrores(bool soloFechaInicial)
    {
        var filtro = new FiltroBitacoraRecursosHumanos
        {
            FechaDesde = soloFechaInicial ? new DateTime(2026, 7, 1) : null,
            FechaHasta = soloFechaInicial ? null : new DateTime(2026, 7, 31)
        };

        Assert.Empty(Validar(filtro));
    }

    [Theory]
    [InlineData(CategoriasBitacoraRecursosHumanos.Colaborador)]
    [InlineData(CategoriasBitacoraRecursosHumanos.PeriodoPlanilla)]
    [InlineData(CategoriasBitacoraRecursosHumanos.Planilla)]
    public void Validar_CategoriaOficial_NoReportaErrores(string categoria)
    {
        Assert.Empty(Validar(new FiltroBitacoraRecursosHumanos
        {
            FechaDesde = new DateTime(2026, 7, 1),
            FechaHasta = new DateTime(2026, 7, 31),
            Categoria = categoria
        }));
    }

    [Fact]
    public void Validar_RangoInvertido_ReportaAmbasFechas()
    {
        var filtro = new FiltroBitacoraRecursosHumanos
        {
            FechaDesde = new DateTime(2026, 8, 1),
            FechaHasta = new DateTime(2026, 7, 31)
        };

        var resultado = Assert.Single(Validar(filtro));

        Assert.Contains(nameof(filtro.FechaDesde), resultado.MemberNames);
        Assert.Contains(nameof(filtro.FechaHasta), resultado.MemberNames);
    }

    [Fact]
    public void Validar_CategoriaDesconocida_LaRechaza()
    {
        var filtro = new FiltroBitacoraRecursosHumanos
        {
            Categoria = "Auditor"
        };

        var resultado = Assert.Single(Validar(filtro));

        Assert.Contains(nameof(filtro.Categoria), resultado.MemberNames);
    }

    private static IReadOnlyCollection<ValidationResult> Validar(
        FiltroBitacoraRecursosHumanos filtro)
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
