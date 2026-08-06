using SIGECDC.Domain.Forecast;

namespace SIGECDC.Tests.Forecast;

public sealed class CalculadoraDesviacionForecastTests
{
    [Theory]
    [InlineData(100, 100, 0, 0)]
    [InlineData(100, 125, 25, 25)]
    [InlineData(100, 75, 25, 25)]
    [InlineData(-100, -75, 25, 25)]
    [InlineData(-100, -125, 25, 25)]
    public void ValoresComparables_CalculanAbsolutaYPorcentaje(
        decimal forecast,
        decimal real,
        decimal absoluta,
        decimal porcentual)
    {
        var resultado = CalculadoraDesviacionForecast.Calcular(forecast, real);

        Assert.Equal(forecast, resultado.MontoForecast);
        Assert.Equal(real, resultado.MontoReal);
        Assert.Equal(absoluta, resultado.DesviacionAbsoluta);
        Assert.Equal(porcentual, resultado.DesviacionPorcentual);
        Assert.True(resultado.EsComparacionCalculable);
        Assert.True(resultado.EsPorcentajeCalculable);
        Assert.Null(resultado.MotivoNoCalculable);
    }

    [Fact]
    public void AmbosEnCero_ProduceCeroAbsolutoYPorcentual()
    {
        var resultado = CalculadoraDesviacionForecast.Calcular(0m, 0m);

        Assert.Equal(0m, resultado.DesviacionAbsoluta);
        Assert.Equal(0m, resultado.DesviacionPorcentual);
        Assert.Null(resultado.MotivoNoCalculable);
    }

    [Theory]
    [InlineData(10)]
    [InlineData(-10)]
    public void ForecastCeroYRealDistinto_ConservaAbsolutaSinInventarPorcentaje(decimal real)
    {
        var resultado = CalculadoraDesviacionForecast.Calcular(0m, real);

        Assert.Equal(Math.Abs(real), resultado.DesviacionAbsoluta);
        Assert.Null(resultado.DesviacionPorcentual);
        Assert.True(resultado.EsComparacionCalculable);
        Assert.False(resultado.EsPorcentajeCalculable);
        Assert.Equal(MotivosDesviacionForecast.BaseForecastCero, resultado.MotivoNoCalculable);
    }

    public static IEnumerable<object?[]> ValoresSinCorrespondencia =>
    [
        [null, 10m],
        [10m, null],
        [null, null]
    ];

    [Theory]
    [MemberData(nameof(ValoresSinCorrespondencia))]
    public void FaltaDeCorrespondencia_NoCalculaDesviaciones(decimal? forecast, decimal? real)
    {
        var resultado = CalculadoraDesviacionForecast.Calcular(forecast, real);

        Assert.Null(resultado.DesviacionAbsoluta);
        Assert.Null(resultado.DesviacionPorcentual);
        Assert.False(resultado.EsComparacionCalculable);
        Assert.False(resultado.EsPorcentajeCalculable);
        Assert.Equal(MotivosDesviacionForecast.SinCorrespondencia, resultado.MotivoNoCalculable);
    }

    [Fact]
    public void Redondeo_UsaAwayFromZeroYPorcentajeSobreValorSinRedondear()
    {
        var resultado = CalculadoraDesviacionForecast.Calcular(2m, 2.025m);

        Assert.Equal(0.03m, resultado.DesviacionAbsoluta);
        Assert.Equal(1.25m, resultado.DesviacionPorcentual);
    }
}
