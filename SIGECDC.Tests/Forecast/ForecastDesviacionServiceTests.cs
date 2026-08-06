using SIGECDC.Application.Forecast;
using SIGECDC.Domain.Forecast;

namespace SIGECDC.Tests.Forecast;

public sealed class ForecastDesviacionServiceTests
{
    [Fact]
    public async Task Calcular_TransformaTotalesYTresMetricasPorFilaConUnaSolaConsulta()
    {
        var comparacion = CrearComparacion();
        var dependencia = new ComparacionRealPrueba(comparacion);
        var servicio = new ForecastDesviacionService(dependencia);
        var solicitud = new SolicitudCompararForecastReal
        {
            Dimension = DimensionesComparacionRealForecast.Colaborador,
            IdForecastPeriodo = 7
        };

        var resultado = Assert.IsType<AnalisisDesviacionesForecast>(
            await servicio.CalcularAsync(9, solicitud, "actor-rh"));

        Assert.Same(comparacion, resultado.Comparacion);
        Assert.Equal(1, dependencia.CantidadLlamadas);
        Assert.Equal(9, dependencia.IdEscenarioRecibido);
        Assert.Same(solicitud, dependencia.SolicitudRecibida);
        Assert.Equal("actor-rh", dependencia.ActorRecibido);
        Assert.Equal(25m, resultado.Totales.SalarioBruto.DesviacionAbsoluta);
        Assert.Equal(25m, resultado.Totales.SalarioBruto.DesviacionPorcentual);
        Assert.Equal(3, resultado.Filas.Count);
        Assert.Equal(3m, resultado.Filas[0].Metricas.SalarioBruto.DesviacionAbsoluta);
        Assert.Equal(50m, resultado.Filas[0].Metricas.Deducciones.DesviacionPorcentual);
        Assert.Equal(2m, resultado.Filas[0].Metricas.SalarioNeto.DesviacionAbsoluta);
    }

    [Fact]
    public async Task Calcular_ConservaFilasSinCorrespondenciaComoNoCalculables()
    {
        var dependencia = new ComparacionRealPrueba(CrearComparacion());
        var servicio = new ForecastDesviacionService(dependencia);

        var resultado = Assert.IsType<AnalisisDesviacionesForecast>(
            await servicio.CalcularAsync(
                9,
                new SolicitudCompararForecastReal
                {
                    Dimension = DimensionesComparacionRealForecast.Colaborador,
                    IdForecastPeriodo = 7
                },
                "actor-rh"));

        var soloForecast = resultado.Filas.Single(item => item.Clave == "PREV:1");
        var soloReal = resultado.Filas.Single(item => item.Clave == "COL:2");
        Assert.Equal(MotivosDesviacionForecast.SinCorrespondencia,
            soloForecast.Metricas.SalarioBruto.MotivoNoCalculable);
        Assert.Equal(MotivosDesviacionForecast.SinCorrespondencia,
            soloReal.Metricas.SalarioBruto.MotivoNoCalculable);
        Assert.Null(soloForecast.Metricas.SalarioBruto.DesviacionAbsoluta);
        Assert.Null(soloReal.Metricas.SalarioBruto.DesviacionAbsoluta);
    }

    [Fact]
    public async Task Calcular_PropagaResultadoNuloSinCrearAnalisis()
    {
        var dependencia = new ComparacionRealPrueba(null);
        var servicio = new ForecastDesviacionService(dependencia);

        var resultado = await servicio.CalcularAsync(
            9,
            new SolicitudCompararForecastReal(),
            "actor-rh");

        Assert.Null(resultado);
        Assert.Equal(1, dependencia.CantidadLlamadas);
    }

    private static ComparacionForecastReal CrearComparacion() => new()
    {
        Dimension = DimensionesComparacionRealForecast.Colaborador,
        IdForecastPeriodo = 7,
        TotalesComparables = new ValoresForecastReal
        {
            SalarioBrutoProyectado = 100m,
            SalarioBrutoReal = 125m,
            DeduccionesProyectadas = 10m,
            DeduccionesReales = 15m,
            SalarioNetoProyectado = 90m,
            SalarioNetoReal = 110m
        },
        Filas =
        [
            new FilaComparacionForecastReal
            {
                Clave = "COL:1",
                Codigo = "COL-1",
                Etiqueta = "Coincidente",
                TieneForecast = true,
                TieneReal = true,
                Valores = new ValoresForecastReal
                {
                    SalarioBrutoProyectado = 10m,
                    SalarioBrutoReal = 13m,
                    DeduccionesProyectadas = 2m,
                    DeduccionesReales = 3m,
                    SalarioNetoProyectado = 8m,
                    SalarioNetoReal = 10m
                }
            },
            new FilaComparacionForecastReal
            {
                Clave = "PREV:1",
                Codigo = "PREV-1",
                Etiqueta = "Contratación prevista",
                TieneForecast = true,
                EsContratacionPrevista = true,
                Valores = new ValoresForecastReal
                {
                    SalarioBrutoProyectado = 20m,
                    DeduccionesProyectadas = 2m,
                    SalarioNetoProyectado = 18m
                }
            },
            new FilaComparacionForecastReal
            {
                Clave = "COL:2",
                Codigo = "COL-2",
                Etiqueta = "Solo real",
                TieneReal = true,
                Valores = new ValoresForecastReal
                {
                    SalarioBrutoReal = 30m,
                    DeduccionesReales = 3m,
                    SalarioNetoReal = 27m
                }
            }
        ]
    };

    private sealed class ComparacionRealPrueba(ComparacionForecastReal? resultado)
        : IForecastComparacionRealService
    {
        public int CantidadLlamadas { get; private set; }
        public long IdEscenarioRecibido { get; private set; }
        public SolicitudCompararForecastReal? SolicitudRecibida { get; private set; }
        public string? ActorRecibido { get; private set; }

        public Task<DisponibilidadComparacionRealForecast?> ObtenerDisponibilidadAsync(
            long idForecastEscenario,
            string idUsuarioActual,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<ComparacionForecastReal?> CompararAsync(
            long idForecastEscenario,
            SolicitudCompararForecastReal solicitud,
            string idUsuarioActual,
            CancellationToken cancellationToken = default)
        {
            CantidadLlamadas++;
            IdEscenarioRecibido = idForecastEscenario;
            SolicitudRecibida = solicitud;
            ActorRecibido = idUsuarioActual;
            return Task.FromResult(resultado);
        }
    }
}
