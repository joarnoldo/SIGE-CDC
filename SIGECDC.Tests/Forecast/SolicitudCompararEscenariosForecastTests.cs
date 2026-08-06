using System.ComponentModel.DataAnnotations;
using SIGECDC.Application.Forecast;

namespace SIGECDC.Tests.Forecast;

public sealed class SolicitudCompararEscenariosForecastTests
{
    [Fact]
    public void SolicitudValida_AceptaDimensionYPeriodoOpcional()
    {
        var solicitud = new SolicitudCompararEscenariosForecast
        {
            IdEscenarioBase = 1,
            IdEscenarioAlternativo = 2,
            Dimension = DimensionesResultadoForecast.Proyecto,
            NumeroOrdenPeriodo = 1
        };

        Assert.Empty(Validar(solicitud));
    }

    [Fact]
    public void IdentificadoresInvalidosOIguales_SeRechazan()
    {
        var solicitud = new SolicitudCompararEscenariosForecast
        {
            IdEscenarioBase = 0,
            IdEscenarioAlternativo = 0
        };

        Assert.Contains(Validar(solicitud), item =>
            item.MemberNames.Contains(nameof(solicitud.IdEscenarioBase)));
        Assert.Contains(Validar(solicitud), item =>
            item.MemberNames.Contains(nameof(solicitud.IdEscenarioAlternativo)));

        solicitud.IdEscenarioBase = 7;
        solicitud.IdEscenarioAlternativo = 7;
        Assert.Contains(Validar(solicitud), item =>
            item.MemberNames.Contains(nameof(solicitud.IdEscenarioAlternativo)));
    }

    [Fact]
    public void DimensionDesconocidaYPeriodoNoPositivo_SeRechazan()
    {
        var solicitud = new SolicitudCompararEscenariosForecast
        {
            IdEscenarioBase = 1,
            IdEscenarioAlternativo = 2,
            Dimension = "Costo",
            NumeroOrdenPeriodo = -1
        };

        var errores = Validar(solicitud);
        Assert.Contains(errores, item => item.MemberNames.Contains(nameof(solicitud.Dimension)));
        Assert.Contains(errores, item =>
            item.MemberNames.Contains(nameof(solicitud.NumeroOrdenPeriodo)));
    }

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
