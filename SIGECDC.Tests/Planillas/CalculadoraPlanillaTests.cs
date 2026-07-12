using SIGECDC.Domain.Planillas;

namespace SIGECDC.Tests.Planillas;

public sealed class CalculadoraPlanillaTests
{
    [Fact]
    public void Calcular_PeriodoMensual_SumaIncidenciasYParametros()
    {
        var resultado = CalculadoraPlanilla.Calcular(new DatosCalculoPlanilla(
            1_000_000m,
            TiposPeriodoPlanilla.Mensual,
            50_000m,
            25_000m,
            10_000m,
            5_000m,
            [
                Parametro("BENEFICIO", TiposParametroPlanilla.Monto, NaturalezasParametroPlanilla.Beneficio, 20_000m),
                Parametro("DEDUCCION", TiposParametroPlanilla.Porcentaje, NaturalezasParametroPlanilla.Deduccion, 10m)
            ]));

        Assert.Equal(1_000_000m, resultado.SalarioProporcional);
        Assert.Equal(20_000m, resultado.TotalBeneficiosConfigurables);
        Assert.Equal(1_085_000m, resultado.SalarioBruto);
        Assert.Equal(111_500m, resultado.TotalDeducciones);
        Assert.Equal(973_500m, resultado.SalarioNeto);
    }

    [Fact]
    public void Calcular_PeriodoQuincenal_UsaMitadDelSalarioMensual()
    {
        var resultado = CalculadoraPlanilla.Calcular(new DatosCalculoPlanilla(
            900_000m,
            TiposPeriodoPlanilla.Quincenal,
            0m,
            0m,
            0m,
            0m,
            []));

        Assert.Equal(900_000m, resultado.SalarioBase);
        Assert.Equal(450_000m, resultado.SalarioProporcional);
        Assert.Equal(450_000m, resultado.SalarioNeto);
    }

    [Fact]
    public void Calcular_ParametroColaborador_TienePrecedencia()
    {
        var parametro = new ParametroAplicablePlanilla(
            "DEDUCCION",
            TiposParametroPlanilla.Monto,
            NaturalezasParametroPlanilla.Deduccion,
            100m,
            200m,
            300m);

        var resultado = CalcularConParametros(parametro);

        Assert.Equal(300m, resultado.TotalDeducciones);
    }

    [Fact]
    public void Calcular_ParametroPeriodo_SeUsaSinValorColaborador()
    {
        var parametro = new ParametroAplicablePlanilla(
            "DEDUCCION",
            TiposParametroPlanilla.Monto,
            NaturalezasParametroPlanilla.Deduccion,
            100m,
            200m,
            null);

        var resultado = CalcularConParametros(parametro);

        Assert.Equal(200m, resultado.TotalDeducciones);
    }

    [Fact]
    public void Calcular_ParametroGlobal_SeUsaComoUltimaOpcion()
    {
        var parametro = Parametro(
            "DEDUCCION",
            TiposParametroPlanilla.Monto,
            NaturalezasParametroPlanilla.Deduccion,
            100m);

        var resultado = CalcularConParametros(parametro);

        Assert.Equal(100m, resultado.TotalDeducciones);
    }

    [Fact]
    public void Calcular_Porcentaje_InterpretaDiezComoDiezPorCientoDelBrutoPrevio()
    {
        var resultado = CalculadoraPlanilla.Calcular(new DatosCalculoPlanilla(
            1_000m,
            TiposPeriodoPlanilla.Mensual,
            100m,
            0m,
            0m,
            0m,
            [Parametro("PORCENTAJE", TiposParametroPlanilla.Porcentaje, NaturalezasParametroPlanilla.Deduccion, 10m)]));

        Assert.Equal(110m, resultado.TotalDeducciones);
        Assert.Equal(990m, resultado.SalarioNeto);
    }

    [Fact]
    public void Calcular_IgnoraCantidadTextoONaturalezaAusente()
    {
        var resultado = CalculadoraPlanilla.Calcular(new DatosCalculoPlanilla(
            1_000m,
            TiposPeriodoPlanilla.Mensual,
            0m,
            0m,
            0m,
            0m,
            [
                Parametro("CANTIDAD", TiposParametroPlanilla.Cantidad, NaturalezasParametroPlanilla.Deduccion, 50m),
                Parametro("TEXTO", TiposParametroPlanilla.Texto, NaturalezasParametroPlanilla.Beneficio, 50m),
                Parametro("SIN_NATURALEZA", TiposParametroPlanilla.Monto, null, 50m)
            ]));

        Assert.Equal(0m, resultado.TotalBeneficiosConfigurables);
        Assert.Equal(0m, resultado.TotalDeducciones);
        Assert.Equal(1_000m, resultado.SalarioNeto);
    }

    [Fact]
    public void Redondear_UsaAwayFromZero()
    {
        Assert.Equal(10.01m, CalculadoraPlanilla.Redondear(10.005m));
    }

    [Fact]
    public void Calcular_NetoNegativo_BloqueaElResultado()
    {
        var excepcion = Assert.Throws<InvalidOperationException>(() =>
            CalculadoraPlanilla.Calcular(new DatosCalculoPlanilla(
                1_000m,
                TiposPeriodoPlanilla.Mensual,
                0m,
                0m,
                0m,
                1_001m,
                [])));

        Assert.Contains("neto", excepcion.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Calcular_TipoPeriodoNoPermitido_RechazaElCalculo()
    {
        Assert.Throws<ArgumentException>(() =>
            CalculadoraPlanilla.Calcular(new DatosCalculoPlanilla(
                1_000m,
                "Semanal",
                0m,
                0m,
                0m,
                0m,
                [])));
    }

    private static ResultadoCalculoPlanilla CalcularConParametros(params ParametroAplicablePlanilla[] parametros)
    {
        return CalculadoraPlanilla.Calcular(new DatosCalculoPlanilla(
            1_000m,
            TiposPeriodoPlanilla.Mensual,
            0m,
            0m,
            0m,
            0m,
            parametros));
    }

    private static ParametroAplicablePlanilla Parametro(
        string codigo,
        string tipo,
        string? naturaleza,
        decimal? valorGlobal)
    {
        return new ParametroAplicablePlanilla(codigo, tipo, naturaleza, valorGlobal, null, null);
    }
}
