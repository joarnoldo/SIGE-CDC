using System.ComponentModel.DataAnnotations;
using SIGECDC.Application.Auditoria;

namespace SIGECDC.Tests.Auditoria;

public sealed class FiltroBitacoraOperacionesTests
{
    [Fact]
    public void Validar_FiltroVacio_NoReportaErrores()
    {
        Assert.Empty(Validar(new FiltroBitacoraOperaciones()));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Validar_PeriodoAbierto_NoReportaErrores(
        bool soloFechaInicial)
    {
        var filtro = new FiltroBitacoraOperaciones
        {
            FechaDesde = soloFechaInicial
                ? new DateTime(2026, 7, 1)
                : null,
            FechaHasta = soloFechaInicial
                ? null
                : new DateTime(2026, 7, 31)
        };

        Assert.Empty(Validar(filtro));
    }

    [Fact]
    public void Validar_MismoDia_NoReportaErrores()
    {
        var filtro = new FiltroBitacoraOperaciones
        {
            FechaDesde =
                new DateTime(2026, 7, 15, 8, 0, 0),
            FechaHasta =
                new DateTime(2026, 7, 15, 18, 0, 0)
        };

        Assert.Empty(Validar(filtro));
    }

    [Fact]
    public void Validar_RangoYCategoriaValidos_NoReportaErrores()
    {
        var filtro = new FiltroBitacoraOperaciones
        {
            FechaDesde = new DateTime(2026, 7, 1),
            FechaHasta = new DateTime(2026, 7, 31),
            Categoria =
                CategoriasBitacoraOperaciones.Mantenimiento
        };

        Assert.Empty(Validar(filtro));
    }

    [Fact]
    public void Validar_RangoInvertido_ReportaAmbasFechas()
    {
        var filtro = new FiltroBitacoraOperaciones
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

    [Fact]
    public void Validar_CategoriaDesconocida_LaRechaza()
    {
        var filtro = new FiltroBitacoraOperaciones
        {
            Categoria = "Auditor"
        };

        var resultado = Assert.Single(Validar(filtro));

        Assert.Contains(
            nameof(filtro.Categoria),
            resultado.MemberNames);
    }

    private static IReadOnlyCollection<ValidationResult> Validar(
        FiltroBitacoraOperaciones filtro)
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
