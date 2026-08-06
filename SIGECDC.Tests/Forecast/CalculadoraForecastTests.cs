using SIGECDC.Domain.Forecast;
using SIGECDC.Domain.Planillas;

namespace SIGECDC.Tests.Forecast;

public sealed class CalculadoraForecastTests
{
    [Fact]
    public void Calcular_AplicaParametrosYConservaConceptosExplicables()
    {
        var resultado = CalculadoraForecast.Calcular(CrearDatos(
            tipoPeriodo: TiposPeriodoPlanilla.Mensual,
            parametros:
            [
                new(CodigosParametroForecast.AjusteSalarial, TiposParametroPlanilla.Porcentaje, 10m),
                new(CodigosParametroForecast.HorasExtraEstimadas, TiposParametroPlanilla.Monto, 200m),
                new(CodigosParametroForecast.BonosEstimados, TiposParametroPlanilla.Porcentaje, 5m),
                new(CodigosParametroForecast.DeduccionesRecurrentes, TiposParametroPlanilla.Monto, 80m)
            ]));

        Assert.Equal(1365m, resultado.MontoProyectadoTotal);
        Assert.Equal(8, resultado.Conceptos.Count);
        AssertConcepto(resultado, ConceptosForecast.SalarioProporcional, 1000m, 100m, 1100m);
        AssertConcepto(resultado, ConceptosForecast.HorasExtra, 100m, 100m, 200m);
        AssertConcepto(resultado, ConceptosForecast.Bonos, 50m, 0m, 50m);
        AssertConcepto(resultado, ConceptosForecast.SalarioBruto, 1165m, 200m, 1365m);
        AssertConcepto(resultado, ConceptosForecast.Deducciones, 20m, 60m, 80m);
        AssertConcepto(resultado, ConceptosForecast.SalarioNeto, 1145m, 140m, 1285m);
    }

    [Fact]
    public void Calcular_SinParametrosUsaPromediosHistoricos()
    {
        var resultado = CalculadoraForecast.Calcular(CrearDatos(
            tipoPeriodo: TiposPeriodoPlanilla.Quincenal,
            parametros: []));

        Assert.Equal(665m, resultado.MontoProyectadoTotal);
        AssertConcepto(resultado, ConceptosForecast.SalarioProporcional, 500m, 0m, 500m);
        AssertConcepto(resultado, ConceptosForecast.HorasExtra, 100m, 0m, 100m);
        AssertConcepto(resultado, ConceptosForecast.Bonos, 50m, 0m, 50m);
        AssertConcepto(resultado, ConceptosForecast.SalarioNeto, 645m, 0m, 645m);
    }

    [Fact]
    public void Calcular_ProrrateaContratacionYSalidaPorDiasActivos()
    {
        var inicio = new DateTime(2026, 1, 1);
        var fin = new DateTime(2026, 1, 31);
        var alta = CalculadoraForecast.Calcular(CrearDatos(
            TiposPeriodoPlanilla.Mensual,
            [],
            inicio,
            fin,
            new DateTime(2026, 1, 16),
            null,
            sinVariables: true));
        var salida = CalculadoraForecast.Calcular(CrearDatos(
            TiposPeriodoPlanilla.Mensual,
            [],
            inicio,
            fin,
            inicio,
            new DateTime(2026, 1, 16),
            sinVariables: true));
        var fueraDelPeriodo = CalculadoraForecast.Calcular(CrearDatos(
            TiposPeriodoPlanilla.Mensual,
            [],
            inicio,
            fin,
            new DateTime(2026, 2, 1),
            null,
            sinVariables: true));

        Assert.Equal(516.13m, alta.MontoProyectadoTotal);
        Assert.Equal(483.87m, salida.MontoProyectadoTotal);
        Assert.Equal(0m, fueraDelPeriodo.MontoProyectadoTotal);
    }

    [Fact]
    public void Calcular_RespetaLimitesInclusivosDeAltaYSalida()
    {
        var inicio = new DateTime(2026, 1, 1);
        var fin = new DateTime(2026, 1, 31);

        var altaPrimerDia = CalculadoraForecast.Calcular(CrearDatos(
            TiposPeriodoPlanilla.Mensual,
            [],
            inicio,
            fin,
            inicio,
            null,
            sinVariables: true));
        var altaUltimoDia = CalculadoraForecast.Calcular(CrearDatos(
            TiposPeriodoPlanilla.Mensual,
            [],
            inicio,
            fin,
            fin,
            null,
            sinVariables: true));
        var salidaPrimerDia = CalculadoraForecast.Calcular(CrearDatos(
            TiposPeriodoPlanilla.Mensual,
            [],
            inicio,
            fin,
            inicio,
            inicio,
            sinVariables: true));
        var salidaSegundoDia = CalculadoraForecast.Calcular(CrearDatos(
            TiposPeriodoPlanilla.Mensual,
            [],
            inicio,
            fin,
            inicio,
            inicio.AddDays(1),
            sinVariables: true));
        var salidaPosterior = CalculadoraForecast.Calcular(CrearDatos(
            TiposPeriodoPlanilla.Mensual,
            [],
            inicio,
            fin,
            inicio,
            fin.AddDays(1),
            sinVariables: true));

        Assert.Equal(1000m, altaPrimerDia.MontoProyectadoTotal);
        Assert.Equal(32.26m, altaUltimoDia.MontoProyectadoTotal);
        Assert.Equal(0m, salidaPrimerDia.MontoProyectadoTotal);
        Assert.Equal(32.26m, salidaSegundoDia.MontoProyectadoTotal);
        Assert.Equal(1000m, salidaPosterior.MontoProyectadoTotal);
    }

    [Fact]
    public void DistribuirPorProyecto_AjustaResiduoSinPerderTotales()
    {
        var conceptos = new[]
        {
            new ConceptoForecastCalculado(ConceptosForecast.SalarioBruto, 100.01m, 10m, 110.01m)
        };
        var resultado = CalculadoraForecast.DistribuirPorProyecto(
            conceptos,
            [new(20, 66.6667m), new(10, 33.3333m)]);

        Assert.Equal([10L, 20L], resultado.Select(item => item.IdProyecto));
        Assert.Equal(100.01m, resultado.Sum(item => item.MontoBase));
        Assert.Equal(10m, resultado.Sum(item => item.MontoAjuste));
        Assert.Equal(110.01m, resultado.Sum(item => item.MontoProyectado));
        Assert.All(resultado, item => Assert.Equal(
            item.MontoProyectado,
            item.MontoBase + item.MontoAjuste));
    }

    [Fact]
    public void CalcularYDistribuir_RechazanEntradasNoDeterministas()
    {
        var datosTipoInvalido = CrearDatos("Semanal", []);
        var datosParametroInvalido = CrearDatos(
            TiposPeriodoPlanilla.Mensual,
            [new(CodigosParametroForecast.AjusteSalarial, TiposParametroPlanilla.Monto, 100m)]);

        Assert.Throws<ArgumentException>(() => CalculadoraForecast.Calcular(datosTipoInvalido));
        Assert.Throws<ArgumentException>(() => CalculadoraForecast.Calcular(datosParametroInvalido));
        Assert.Throws<ArgumentException>(() => CalculadoraForecast.DistribuirPorProyecto(
            [new(ConceptosForecast.SalarioBruto, 100m, 0m, 100m)],
            [new(1, 99.9999m)]));
    }

    private static DatosCalculoForecast CrearDatos(
        string tipoPeriodo,
        IReadOnlyCollection<ParametroForecastCalculo> parametros,
        DateTime? fechaInicioPeriodo = null,
        DateTime? fechaFinPeriodo = null,
        DateTime? fechaInicioAplicacion = null,
        DateTime? fechaSalidaPrevista = null,
        bool sinVariables = false)
    {
        var inicio = fechaInicioPeriodo ?? new DateTime(2026, 1, 1);
        var fin = fechaFinPeriodo ?? new DateTime(2026, 1, 31);
        return new DatosCalculoForecast(
            tipoPeriodo,
            inicio,
            fin,
            1000m,
            fechaInicioAplicacion ?? inicio,
            fechaSalidaPrevista,
            sinVariables
                ? new(0m, 0m, 0m, 0m, 0m, 0m)
                : new(500m, 100m, 50m, 25m, 10m, 20m),
            parametros);
    }

    private static void AssertConcepto(
        ResultadoCalculoForecast resultado,
        string concepto,
        decimal montoBase,
        decimal montoAjuste,
        decimal montoProyectado)
    {
        var detalle = Assert.Single(resultado.Conceptos, item => item.Concepto == concepto);
        Assert.Equal(montoBase, detalle.MontoBase);
        Assert.Equal(montoAjuste, detalle.MontoAjuste);
        Assert.Equal(montoProyectado, detalle.MontoProyectado);
    }
}
