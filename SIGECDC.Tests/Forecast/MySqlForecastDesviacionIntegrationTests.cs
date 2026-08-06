using Microsoft.EntityFrameworkCore;
using SIGECDC.Application.Forecast;
using SIGECDC.Domain.Forecast;
using SIGECDC.Domain.Planillas;
using SIGECDC.Persistence.Forecast;
using SIGECDC.Tests.Activos;

namespace SIGECDC.Tests.Forecast;

[Collection("MySQL QA Miembro 2")]
public sealed class MySqlForecastDesviacionIntegrationTests
{
    [MySqlQaFact]
    public async Task PeriodoYColaborador_CalculanDesviacionesSinInventarCorrespondenciasNiEscribir()
    {
        var token = NuevoToken();

        try
        {
            var preparado = await MySqlForecastComparacionRealIntegrationTests
                .PrepararGuardadoAsync(token);
            await MySqlForecastComparacionRealIntegrationTests.CrearPlanillaRealAsync(
                preparado,
                0,
                EstadosPlanilla.Aprobada,
                $"DES-{token}",
                [
                    new(preparado.Datos.IdColaborador, 3_500m, 350m, 3_150m),
                    new(preparado.Datos.IdColaboradorExcluido, 1_200m, 120m, 1_080m)
                ]);

            await using var contexto = MySqlForecastMovimientoPersonalIntegrationTests.CrearContexto();
            var escenarioAntes = await contexto.ForecastEscenarios
                .AsNoTracking()
                .SingleAsync(item => item.IdForecastEscenario == preparado.IdEscenario);
            var detallesAntes = await contexto.ForecastDetalles
                .AsNoTracking()
                .Where(item => item.IdForecastEscenario == preparado.IdEscenario)
                .Select(item => new { item.IdForecastDetalle, item.MontoReal, item.Diferencia })
                .ToListAsync();
            var servicio = new ForecastDesviacionService(
                new ForecastComparacionRealService(contexto));

            var porPeriodo = Assert.IsType<AnalisisDesviacionesForecast>(
                await servicio.CalcularAsync(
                    preparado.IdEscenario,
                    MySqlForecastComparacionRealIntegrationTests.SolicitudPeriodo(),
                    preparado.Datos.IdActorRecursosHumanos));
            Assert.Equal(2, porPeriodo.Filas.Count);
            var disponible = porPeriodo.Filas[0];
            var pendiente = porPeriodo.Filas[1];
            Assert.True(disponible.Metricas.SalarioBruto.EsComparacionCalculable);
            Assert.NotNull(disponible.Metricas.SalarioBruto.DesviacionPorcentual);
            Assert.False(pendiente.Metricas.SalarioBruto.EsComparacionCalculable);
            Assert.Equal(
                MotivosDesviacionForecast.SinCorrespondencia,
                pendiente.Metricas.SalarioBruto.MotivoNoCalculable);
            Assert.Equal(
                CalculadoraDesviacionForecast.Calcular(
                    porPeriodo.Comparacion.TotalesComparables.SalarioBrutoProyectado,
                    porPeriodo.Comparacion.TotalesComparables.SalarioBrutoReal),
                porPeriodo.Totales.SalarioBruto);

            var solicitudColaborador = new SolicitudCompararForecastReal
            {
                Dimension = DimensionesComparacionRealForecast.Colaborador,
                IdForecastPeriodo = preparado.IdsPeriodos[0]
            };
            var porColaborador = Assert.IsType<AnalisisDesviacionesForecast>(
                await servicio.CalcularAsync(
                    preparado.IdEscenario,
                    solicitudColaborador,
                    preparado.Datos.IdActorRecursosHumanos));
            var coincidente = porColaborador.Filas.Single(item =>
                item.Clave == $"COL:{preparado.Datos.IdColaborador}");
            var soloReal = porColaborador.Filas.Single(item =>
                item.Clave == $"COL:{preparado.Datos.IdColaboradorExcluido}");
            var prevista = porColaborador.Filas.Single(item =>
                item.Clave.StartsWith("PREV:", StringComparison.Ordinal));
            Assert.True(coincidente.Metricas.SalarioBruto.EsComparacionCalculable);
            Assert.False(soloReal.Metricas.SalarioBruto.EsComparacionCalculable);
            Assert.False(prevista.Metricas.SalarioBruto.EsComparacionCalculable);
            Assert.Equal(MotivosDesviacionForecast.SinCorrespondencia,
                soloReal.Metricas.SalarioBruto.MotivoNoCalculable);
            Assert.Equal(MotivosDesviacionForecast.SinCorrespondencia,
                prevista.Metricas.SalarioBruto.MotivoNoCalculable);

            var porDepartamento = Assert.IsType<AnalisisDesviacionesForecast>(
                await servicio.CalcularAsync(
                    preparado.IdEscenario,
                    new SolicitudCompararForecastReal
                    {
                        Dimension = DimensionesComparacionRealForecast.Departamento,
                        IdForecastPeriodo = preparado.IdsPeriodos[0]
                    },
                    preparado.Datos.IdActorRecursosHumanos));
            var departamentoPrincipal = porDepartamento.Filas.Single(item =>
                item.Clave == $"DEP:{preparado.Datos.IdDepartamentoPrincipal}");
            var departamentoSoloForecast = porDepartamento.Filas.Single(item =>
                item.Clave == $"DEP:{preparado.Datos.IdDepartamentoSecundario}");
            Assert.True(departamentoPrincipal.Metricas.SalarioBruto.EsComparacionCalculable);
            Assert.False(departamentoSoloForecast.Metricas.SalarioBruto.EsComparacionCalculable);
            Assert.True(porDepartamento.Totales.SalarioBruto.EsComparacionCalculable);

            var porProyecto = Assert.IsType<AnalisisDesviacionesForecast>(
                await servicio.CalcularAsync(
                    preparado.IdEscenario,
                    new SolicitudCompararForecastReal
                    {
                        Dimension = DimensionesComparacionRealForecast.Proyecto,
                        IdForecastPeriodo = preparado.IdsPeriodos[0]
                    },
                    preparado.Datos.IdActorRecursosHumanos));
            Assert.NotEmpty(porProyecto.Filas);
            Assert.All(porProyecto.Filas, fila =>
            {
                Assert.False(fila.Metricas.SalarioBruto.EsComparacionCalculable);
                Assert.Equal(
                    MotivosDesviacionForecast.SinCorrespondencia,
                    fila.Metricas.SalarioBruto.MotivoNoCalculable);
            });
            Assert.False(porProyecto.Totales.SalarioBruto.EsComparacionCalculable);
            Assert.Equal(
                MotivosDesviacionForecast.SinCorrespondencia,
                porProyecto.Totales.SalarioBruto.MotivoNoCalculable);

            var repetida = Assert.IsType<AnalisisDesviacionesForecast>(
                await servicio.CalcularAsync(
                    preparado.IdEscenario,
                    solicitudColaborador,
                    preparado.Datos.IdActorRecursosHumanos));
            Assert.Equal(
                porColaborador.Filas.Select(item => item.Metricas.SalarioBruto).ToList(),
                repetida.Filas.Select(item => item.Metricas.SalarioBruto).ToList());

            var escenarioDespues = await contexto.ForecastEscenarios
                .AsNoTracking()
                .SingleAsync(item => item.IdForecastEscenario == preparado.IdEscenario);
            var detallesDespues = await contexto.ForecastDetalles
                .AsNoTracking()
                .Where(item => item.IdForecastEscenario == preparado.IdEscenario)
                .Select(item => new { item.IdForecastDetalle, item.MontoReal, item.Diferencia })
                .ToListAsync();
            Assert.Equal(escenarioAntes.EstadoEscenario, escenarioDespues.EstadoEscenario);
            Assert.Equal(escenarioAntes.FechaModificacion, escenarioDespues.FechaModificacion);
            Assert.Equal(escenarioAntes.ModificadoPor, escenarioDespues.ModificadoPor);
            Assert.Null(escenarioDespues.MontoRealTotal);
            Assert.Null(escenarioDespues.DiferenciaTotal);
            Assert.Equal(detallesAntes, detallesDespues);
            Assert.Empty(contexto.ChangeTracker.Entries());
        }
        finally
        {
            await MySqlForecastMovimientoPersonalIntegrationTests.LimpiarDatosAsync(token);
        }
    }

    [MySqlQaFact]
    public async Task BaseCero_ConservaAbsolutaExplicitaYProtegeAutorizacion()
    {
        var token = NuevoToken();

        try
        {
            var preparado = await MySqlForecastComparacionRealIntegrationTests
                .PrepararGuardadoAsync(token);
            await MySqlForecastComparacionRealIntegrationTests.CrearPlanillaRealAsync(
                preparado,
                0,
                EstadosPlanilla.Cerrada,
                $"CER-{token}",
                [new(preparado.Datos.IdColaborador, 100m, 0m, 100m)]);

            await using var contexto = MySqlForecastMovimientoPersonalIntegrationTests.CrearContexto();
            var detallesPeriodo = await contexto.ForecastDetalles
                .Where(item => item.IdForecastEscenario == preparado.IdEscenario
                    && item.IdForecastPeriodo == preparado.IdsPeriodos[0])
                .ToListAsync();
            foreach (var detalle in detallesPeriodo)
            {
                detalle.MontoProyectado = 0m;
            }
            await contexto.SaveChangesAsync();
            contexto.ChangeTracker.Clear();

            var servicio = new ForecastDesviacionService(
                new ForecastComparacionRealService(contexto));
            var solicitud = new SolicitudCompararForecastReal
            {
                Dimension = DimensionesComparacionRealForecast.Colaborador,
                IdForecastPeriodo = preparado.IdsPeriodos[0]
            };
            var resultado = Assert.IsType<AnalisisDesviacionesForecast>(
                await servicio.CalcularAsync(
                    preparado.IdEscenario,
                    solicitud,
                    preparado.Datos.IdActorRecursosHumanos));
            var colaborador = resultado.Filas.Single(item =>
                item.Clave == $"COL:{preparado.Datos.IdColaborador}");
            Assert.Equal(100m, colaborador.Metricas.SalarioBruto.DesviacionAbsoluta);
            Assert.Null(colaborador.Metricas.SalarioBruto.DesviacionPorcentual);
            Assert.Equal(MotivosDesviacionForecast.BaseForecastCero,
                colaborador.Metricas.SalarioBruto.MotivoNoCalculable);
            Assert.Equal(0m, colaborador.Metricas.Deducciones.DesviacionAbsoluta);
            Assert.Equal(0m, colaborador.Metricas.Deducciones.DesviacionPorcentual);
            Assert.Equal(100m, colaborador.Metricas.SalarioNeto.DesviacionAbsoluta);
            Assert.Null(colaborador.Metricas.SalarioNeto.DesviacionPorcentual);

            foreach (var actor in new[]
                {
                    " ",
                    "usuario-inexistente",
                    preparado.Datos.IdActorInactivo,
                    preparado.Datos.IdActorAdministrador,
                    preparado.Datos.IdActorOperaciones,
                    preparado.Datos.IdActorEmpleado
                })
            {
                await Assert.ThrowsAsync<UnauthorizedAccessException>(() => servicio.CalcularAsync(
                    long.MaxValue,
                    MySqlForecastComparacionRealIntegrationTests.SolicitudPeriodo(),
                    actor));
            }

            Assert.Empty(contexto.ChangeTracker.Entries());
        }
        finally
        {
            await MySqlForecastMovimientoPersonalIntegrationTests.LimpiarDatosAsync(token);
        }
    }

    private static string NuevoToken() => Guid.NewGuid().ToString("N")[..8];
}
