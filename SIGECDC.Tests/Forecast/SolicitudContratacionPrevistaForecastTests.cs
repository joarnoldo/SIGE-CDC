using System.ComponentModel.DataAnnotations;
using SIGECDC.Application.Forecast;

namespace SIGECDC.Tests.Forecast;

public sealed class SolicitudContratacionPrevistaForecastTests
{
    [Theory]
    [InlineData("0")]
    [InlineData("9999999999999999.99")]
    [InlineData("123456.78")]
    public void Solicitud_AceptaSalariosValidosConHastaDosDecimales(string salario)
    {
        var solicitud = CrearSolicitudValida();
        solicitud.SalarioBaseMensual = decimal.Parse(
            salario,
            System.Globalization.CultureInfo.InvariantCulture);

        Assert.Empty(Validar(solicitud));
    }

    [Fact]
    public void Solicitud_RechazaCatalogosYFechaObligatoriaInvalidos()
    {
        var catalogosInvalidos = CrearSolicitudValida();
        catalogosInvalidos.IdDepartamento = 0;
        catalogosInvalidos.IdPuesto = -1;
        var fechaInvalida = CrearSolicitudValida();
        fechaInvalida.FechaInicioAplicacion = default;

        var resultadosCatalogos = Validar(catalogosInvalidos);
        var resultadosFecha = Validar(fechaInvalida);

        Assert.Contains(
            resultadosCatalogos,
            item => item.MemberNames.Contains(nameof(catalogosInvalidos.IdDepartamento)));
        Assert.Contains(
            resultadosCatalogos,
            item => item.MemberNames.Contains(nameof(catalogosInvalidos.IdPuesto)));
        Assert.Contains(
            resultadosFecha,
            item => item.MemberNames.Contains(nameof(fechaInvalida.FechaInicioAplicacion)));
    }

    [Theory]
    [InlineData("-0.01")]
    [InlineData("10000000000000000")]
    [InlineData("1.001")]
    public void Solicitud_RechazaSalarioFueraDeRangoOConEscalaExcesiva(string salario)
    {
        var solicitud = CrearSolicitudValida();
        solicitud.SalarioBaseMensual = decimal.Parse(
            salario,
            System.Globalization.CultureInfo.InvariantCulture);

        Assert.Contains(
            Validar(solicitud),
            item => item.MemberNames.Contains(nameof(solicitud.SalarioBaseMensual)));
    }

    internal static SolicitudContratacionPrevistaForecast CrearSolicitudValida() => new()
    {
        IdDepartamento = 1,
        IdPuesto = 1,
        SalarioBaseMensual = 1_250_000.50m,
        FechaInicioAplicacion = DateTime.Today.AddMonths(1)
    };

    private static IReadOnlyList<ValidationResult> Validar(object instancia)
    {
        var resultados = new List<ValidationResult>();
        Validator.TryValidateObject(
            instancia,
            new ValidationContext(instancia),
            resultados,
            validateAllProperties: true);
        return resultados;
    }
}
