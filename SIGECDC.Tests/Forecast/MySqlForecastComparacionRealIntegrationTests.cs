using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using SIGECDC.Application.Forecast;
using SIGECDC.Domain.Forecast;
using SIGECDC.Domain.Planillas;
using SIGECDC.Domain.SitioPublico;
using SIGECDC.Persistence.Forecast;
using SIGECDC.Tests.Activos;

namespace SIGECDC.Tests.Forecast;

[Collection("MySQL QA Miembro 2")]
public sealed class MySqlForecastComparacionRealIntegrationTests
{
    [MySqlQaFact]
    public async Task Periodos_AdmiteDisponibilidadParcialYAprobadaOCerrada()
    {
        var token = NuevoToken();

        try
        {
            var preparado = await PrepararGuardadoAsync(token);
            var idAprobada = await CrearPlanillaRealAsync(
                preparado,
                0,
                EstadosPlanilla.Aprobada,
                $"APR-{token}",
                [new(preparado.Datos.IdColaborador, 3_500m, 350m, 3_150m)]);
            var idCalculada = await CrearPlanillaRealAsync(
                preparado,
                1,
                EstadosPlanilla.Calculada,
                $"CAL-{token}",
                [new(preparado.Datos.IdColaborador, 3_800m, 380m, 3_420m)]);
            await CrearPlanillaRealAsync(
                preparado,
                1,
                EstadosPlanilla.Aprobada,
                $"INA-{token}",
                [new(preparado.Datos.IdColaborador, 9_999m, 0m, 9_999m)],
                activa: false);

            await using var contexto = MySqlForecastMovimientoPersonalIntegrationTests.CrearContexto();
            var servicio = new ForecastComparacionRealService(contexto);
            var parcial = Assert.IsType<ComparacionForecastReal>(await servicio.CompararAsync(
                preparado.IdEscenario,
                SolicitudPeriodo(),
                preparado.Datos.IdActorRecursosHumanos));

            Assert.Equal(1, parcial.Disponibilidad.CantidadPeriodosDisponibles);
            Assert.Equal(1, parcial.Disponibilidad.CantidadPeriodosPendientes);
            Assert.Equal(2, parcial.Filas.Count);
            var primero = parcial.Disponibilidad.Periodos[0];
            var segundo = parcial.Disponibilidad.Periodos[1];
            Assert.True(primero.EstaDisponible);
            Assert.Equal(idAprobada, primero.IdPlanilla);
            Assert.Equal(EstadosPlanilla.Aprobada, primero.EstadoPlanillaReal);
            Assert.Equal(3_500m, primero.Valores.SalarioBrutoReal);
            Assert.False(segundo.EstaDisponible);
            Assert.Null(segundo.Valores.SalarioBrutoReal);
            Assert.Contains("todavía no está aprobada", segundo.MensajeDisponibilidad);
            Assert.Equal(primero.Valores.SalarioBrutoProyectado,
                parcial.TotalesComparables.SalarioBrutoProyectado);
            Assert.Equal(3_500m, parcial.TotalesComparables.SalarioBrutoReal);

            var planillaCalculada = await contexto.Planillas
                .Include(item => item.PeriodoPlanilla)
                .SingleAsync(item => item.IdPlanilla == idCalculada);
            var idCerrada = await contexto.EstadosPlanilla
                .Where(item => item.Nombre == EstadosPlanilla.Cerrada)
                .Select(item => item.IdEstadoPlanilla)
                .SingleAsync();
            planillaCalculada.IdEstadoPlanilla = idCerrada;
            planillaCalculada.PeriodoPlanilla!.IdEstadoPlanilla = idCerrada;
            planillaCalculada.FechaCierre = DateTime.Now;
            await contexto.SaveChangesAsync();
            contexto.ChangeTracker.Clear();

            var completa = Assert.IsType<DisponibilidadComparacionRealForecast>(
                await servicio.ObtenerDisponibilidadAsync(
                    preparado.IdEscenario,
                    preparado.Datos.IdActorRecursosHumanos));
            Assert.Equal(2, completa.CantidadPeriodosDisponibles);
            Assert.Equal(0, completa.CantidadPeriodosPendientes);
            Assert.Equal(EstadosPlanilla.Cerrada, completa.Periodos[1].EstadoPlanillaReal);
            Assert.Empty(contexto.ChangeTracker.Entries());
        }
        finally
        {
            await MySqlForecastMovimientoPersonalIntegrationTests.LimpiarDatosAsync(token);
        }
    }

    [MySqlQaFact]
    public async Task Colaboradores_ConstruyeUnionCompletaSinInferirContrataciones()
    {
        var token = NuevoToken();

        try
        {
            var preparado = await PrepararGuardadoAsync(token);
            await CrearPlanillaRealAsync(
                preparado,
                0,
                EstadosPlanilla.Aprobada,
                $"DET-{token}",
                [
                    new(preparado.Datos.IdColaborador, 3_500m, 350m, 3_150m),
                    new(preparado.Datos.IdColaboradorExcluido, 1_200m, 120m, 1_080m)
                ],
                ajusteBrutoCabecera: 1m);

            await using var contexto = MySqlForecastMovimientoPersonalIntegrationTests.CrearContexto();
            var servicio = new ForecastComparacionRealService(contexto);
            var resultado = Assert.IsType<ComparacionForecastReal>(await servicio.CompararAsync(
                preparado.IdEscenario,
                new SolicitudCompararForecastReal
                {
                    Dimension = DimensionesComparacionRealForecast.Colaborador,
                    IdForecastPeriodo = preparado.IdsPeriodos[0]
                },
                preparado.Datos.IdActorRecursosHumanos));

            Assert.Equal(DimensionesComparacionRealForecast.Colaborador, resultado.Dimension);
            Assert.Equal(3, resultado.Filas.Count);
            var coincidente = resultado.Filas.Single(item =>
                item.Clave == $"COL:{preparado.Datos.IdColaborador}");
            Assert.True(coincidente.TieneForecast);
            Assert.True(coincidente.TieneReal);
            Assert.NotNull(coincidente.Valores.SalarioBrutoProyectado);
            Assert.Equal(3_500m, coincidente.Valores.SalarioBrutoReal);

            var soloReal = resultado.Filas.Single(item =>
                item.Clave == $"COL:{preparado.Datos.IdColaboradorExcluido}");
            Assert.False(soloReal.TieneForecast);
            Assert.True(soloReal.TieneReal);
            Assert.Null(soloReal.Valores.SalarioBrutoProyectado);
            Assert.Equal(1_200m, soloReal.Valores.SalarioBrutoReal);

            var prevista = resultado.Filas.Single(item =>
                item.Clave.StartsWith("PREV:", StringComparison.Ordinal));
            Assert.True(prevista.EsContratacionPrevista);
            Assert.True(prevista.TieneForecast);
            Assert.False(prevista.TieneReal);
            Assert.Null(prevista.Valores.SalarioBrutoReal);
            Assert.False(resultado.Disponibilidad.Periodos[0].EsConsistenteConDetalles);
            Assert.Equal(4_701m, resultado.TotalesComparables.SalarioBrutoReal);
            Assert.Empty(contexto.ChangeTracker.Entries());
        }
        finally
        {
            await MySqlForecastMovimientoPersonalIntegrationTests.LimpiarDatosAsync(token);
        }
    }

    [MySqlQaFact]
    public async Task DepartamentoYProyecto_UsanSoloAtribucionesRealesDisponibles()
    {
        var token = NuevoToken();

        try
        {
            var preparado = await PrepararGuardadoAsync(token);
            await CrearPlanillaRealAsync(
                preparado,
                0,
                EstadosPlanilla.Aprobada,
                $"DIM-{token}",
                [
                    new(preparado.Datos.IdColaborador, 3_500m, 350m, 3_150m),
                    new(preparado.Datos.IdColaboradorExcluido, 1_200m, 120m, 1_080m)
                ]);

            await using var contexto = MySqlForecastMovimientoPersonalIntegrationTests.CrearContexto();
            var servicio = new ForecastComparacionRealService(contexto);
            var porDepartamento = Assert.IsType<ComparacionForecastReal>(
                await servicio.CompararAsync(
                    preparado.IdEscenario,
                    new SolicitudCompararForecastReal
                    {
                        Dimension = DimensionesComparacionRealForecast.Departamento,
                        IdForecastPeriodo = preparado.IdsPeriodos[0]
                    },
                    preparado.Datos.IdActorRecursosHumanos));

            Assert.Equal(DimensionesComparacionRealForecast.Departamento,
                porDepartamento.Dimension);
            Assert.Equal(2, porDepartamento.Filas.Count);
            var principal = porDepartamento.Filas.Single(item =>
                item.Clave == $"DEP:{preparado.Datos.IdDepartamentoPrincipal}");
            var secundaria = porDepartamento.Filas.Single(item =>
                item.Clave == $"DEP:{preparado.Datos.IdDepartamentoSecundario}");
            Assert.True(principal.TieneForecast);
            Assert.True(principal.TieneReal);
            Assert.Equal(4_700m, principal.Valores.SalarioBrutoReal);
            Assert.Contains(
                "departamento vigente",
                Assert.IsType<string>(principal.Detalle),
                StringComparison.OrdinalIgnoreCase);
            Assert.True(secundaria.TieneForecast);
            Assert.False(secundaria.TieneReal);
            Assert.Null(secundaria.Valores.SalarioBrutoReal);
            Assert.Equal(4_700m, porDepartamento.TotalesComparables.SalarioBrutoReal);

            var porProyecto = Assert.IsType<ComparacionForecastReal>(
                await servicio.CompararAsync(
                    preparado.IdEscenario,
                    new SolicitudCompararForecastReal
                    {
                        Dimension = DimensionesComparacionRealForecast.Proyecto,
                        IdForecastPeriodo = preparado.IdsPeriodos[0]
                    },
                    preparado.Datos.IdActorRecursosHumanos));

            Assert.Equal(DimensionesComparacionRealForecast.Proyecto, porProyecto.Dimension);
            Assert.Equal(2, porProyecto.Filas.Count);
            Assert.All(porProyecto.Filas, fila =>
            {
                Assert.True(fila.TieneForecast);
                Assert.False(fila.TieneReal);
                Assert.Null(fila.Valores.SalarioBrutoReal);
                Assert.Contains(
                    "no conserva atribución",
                    Assert.IsType<string>(fila.Detalle),
                    StringComparison.OrdinalIgnoreCase);
            });
            Assert.Null(porProyecto.TotalesComparables.SalarioBrutoReal);
            Assert.Equal(
                porDepartamento.TotalesComparables.SalarioBrutoProyectado,
                porProyecto.TotalesComparables.SalarioBrutoProyectado);
            Assert.Empty(contexto.ChangeTracker.Entries());
        }
        finally
        {
            await MySqlForecastMovimientoPersonalIntegrationTests.LimpiarDatosAsync(token);
        }
    }

    [MySqlQaFact]
    public async Task Consulta_AutorizaPrimeroYRechazaEstadosPeriodoONoDisponibilidad()
    {
        var token = NuevoToken();

        try
        {
            var preparado = await MySqlForecastResultadoIntegrationTests
                .PrepararEscenarioCalculadoAsync(token);
            await using var contexto = MySqlForecastMovimientoPersonalIntegrationTests.CrearContexto();
            var servicio = new ForecastComparacionRealService(contexto);

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
                await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                    servicio.ObtenerDisponibilidadAsync(long.MaxValue, actor));
                await Assert.ThrowsAsync<UnauthorizedAccessException>(() => servicio.CompararAsync(
                    long.MaxValue,
                    SolicitudPeriodo(),
                    actor));
            }

            await Assert.ThrowsAsync<InvalidOperationException>(() => servicio.CompararAsync(
                preparado.IdEscenario,
                SolicitudPeriodo(),
                preparado.Datos.IdActorRecursosHumanos));
            await Assert.ThrowsAsync<InvalidOperationException>(() => servicio.CompararAsync(
                preparado.Datos.IdEscenarioComposicion,
                SolicitudPeriodo(),
                preparado.Datos.IdActorRecursosHumanos));
            Assert.Null(await servicio.ObtenerDisponibilidadAsync(
                preparado.Datos.IdEscenarioInactivo,
                preparado.Datos.IdActorRecursosHumanos));
            Assert.Null(await servicio.ObtenerDisponibilidadAsync(
                long.MaxValue,
                preparado.Datos.IdActorRecursosHumanos));
            await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
                servicio.ObtenerDisponibilidadAsync(-1, preparado.Datos.IdActorRecursosHumanos));

            var escenarioAnulado = await contexto.ForecastEscenarios.SingleAsync(item =>
                item.IdForecastEscenario == preparado.Datos.IdEscenarioCalculado);
            escenarioAnulado.EstadoEscenario = EstadosEscenarioForecast.Anulado;
            await contexto.SaveChangesAsync();
            contexto.ChangeTracker.Clear();
            await Assert.ThrowsAsync<InvalidOperationException>(() => servicio.CompararAsync(
                escenarioAnulado.IdForecastEscenario,
                SolicitudPeriodo(),
                preparado.Datos.IdActorRecursosHumanos));

            var escenarios = new ForecastEscenarioService(contexto);
            await escenarios.GuardarAsync(
                preparado.IdEscenario,
                preparado.Datos.IdActorRecursosHumanos);
            await CrearPlanillaRealAsync(
                preparado,
                0,
                EstadosPlanilla.Calculada,
                $"PEN-{token}",
                [new(preparado.Datos.IdColaborador, 1_000m, 100m, 900m)]);
            contexto.ChangeTracker.Clear();

            foreach (var dimension in DimensionesComparacionRealForecast.Todas.Where(
                DimensionesComparacionRealForecast.RequierePeriodo))
            {
                await Assert.ThrowsAsync<ValidationException>(() => servicio.CompararAsync(
                    preparado.IdEscenario,
                    new SolicitudCompararForecastReal
                    {
                        Dimension = dimension,
                        IdForecastPeriodo = long.MaxValue
                    },
                    preparado.Datos.IdActorRecursosHumanos));
                await Assert.ThrowsAsync<InvalidOperationException>(() => servicio.CompararAsync(
                    preparado.IdEscenario,
                    new SolicitudCompararForecastReal
                    {
                        Dimension = dimension,
                        IdForecastPeriodo = preparado.IdsPeriodos[0]
                    },
                    preparado.Datos.IdActorRecursosHumanos));
            }
            Assert.Empty(contexto.ChangeTracker.Entries());
        }
        finally
        {
            await MySqlForecastMovimientoPersonalIntegrationTests.LimpiarDatosAsync(token);
        }
    }

    [MySqlQaFact]
    public async Task CorrespondenciaAmbigua_NoSeleccionaPlanillaNiModificaForecast()
    {
        var token = NuevoToken();

        try
        {
            var preparado = await PrepararGuardadoAsync(token);
            await CrearPlanillaRealAsync(
                preparado,
                0,
                EstadosPlanilla.Aprobada,
                $"AMB1-{token}",
                [new(preparado.Datos.IdColaborador, 1_000m, 100m, 900m)]);
            await CrearPlanillaRealAsync(
                preparado,
                0,
                EstadosPlanilla.Cerrada,
                $"AMB2-{token}",
                [new(preparado.Datos.IdColaborador, 2_000m, 200m, 1_800m)]);

            await using var contexto = MySqlForecastMovimientoPersonalIntegrationTests.CrearContexto();
            var escenarioAntes = await contexto.ForecastEscenarios
                .AsNoTracking()
                .SingleAsync(item => item.IdForecastEscenario == preparado.IdEscenario);
            var valoresAntes = await contexto.ForecastDetalles
                .AsNoTracking()
                .Where(item => item.IdForecastEscenario == preparado.IdEscenario)
                .Select(item => new { item.IdForecastDetalle, item.MontoReal, item.Diferencia })
                .ToListAsync();
            var servicio = new ForecastComparacionRealService(contexto);

            var disponibilidad = Assert.IsType<DisponibilidadComparacionRealForecast>(
                await servicio.ObtenerDisponibilidadAsync(
                    preparado.IdEscenario,
                    preparado.Datos.IdActorRecursosHumanos));
            var ambiguo = disponibilidad.Periodos[0];
            Assert.False(ambiguo.EstaDisponible);
            Assert.True(ambiguo.EsAmbigua);
            Assert.Null(ambiguo.IdPlanilla);
            Assert.Null(ambiguo.Valores.SalarioBrutoReal);
            Assert.Contains("más de una", ambiguo.MensajeDisponibilidad);

            await Assert.ThrowsAsync<InvalidOperationException>(() => servicio.CompararAsync(
                preparado.IdEscenario,
                new SolicitudCompararForecastReal
                {
                    Dimension = DimensionesComparacionRealForecast.Colaborador,
                    IdForecastPeriodo = preparado.IdsPeriodos[0]
                },
                preparado.Datos.IdActorRecursosHumanos));

            var escenarioDespues = await contexto.ForecastEscenarios
                .AsNoTracking()
                .SingleAsync(item => item.IdForecastEscenario == preparado.IdEscenario);
            var valoresDespues = await contexto.ForecastDetalles
                .AsNoTracking()
                .Where(item => item.IdForecastEscenario == preparado.IdEscenario)
                .Select(item => new { item.IdForecastDetalle, item.MontoReal, item.Diferencia })
                .ToListAsync();
            Assert.Equal(escenarioAntes.EstadoEscenario, escenarioDespues.EstadoEscenario);
            Assert.Equal(escenarioAntes.FechaModificacion, escenarioDespues.FechaModificacion);
            Assert.Equal(escenarioAntes.ModificadoPor, escenarioDespues.ModificadoPor);
            Assert.Null(escenarioDespues.MontoRealTotal);
            Assert.Null(escenarioDespues.DiferenciaTotal);
            Assert.Equal(valoresAntes, valoresDespues);
            Assert.Empty(contexto.ChangeTracker.Entries());
        }
        finally
        {
            await MySqlForecastMovimientoPersonalIntegrationTests.LimpiarDatosAsync(token);
        }
    }

    internal static async Task<MySqlForecastResultadoIntegrationTests.ResultadoPreparado>
        PrepararGuardadoAsync(string token)
    {
        var preparado = await MySqlForecastResultadoIntegrationTests
            .PrepararEscenarioCalculadoAsync(token);
        await using var contexto = MySqlForecastMovimientoPersonalIntegrationTests.CrearContexto();
        await new ForecastEscenarioService(contexto).GuardarAsync(
            preparado.IdEscenario,
            preparado.Datos.IdActorRecursosHumanos);
        return preparado;
    }

    internal static async Task<long> CrearPlanillaRealAsync(
        MySqlForecastResultadoIntegrationTests.ResultadoPreparado preparado,
        int indicePeriodo,
        string estado,
        string codigo,
        IReadOnlyCollection<DetalleRealDefinicion> detalles,
        decimal ajusteBrutoCabecera = 0m,
        bool activa = true)
    {
        await using var contexto = MySqlForecastMovimientoPersonalIntegrationTests.CrearContexto();
        var periodoForecast = await contexto.ForecastPeriodos
            .AsNoTracking()
            .SingleAsync(item => item.IdForecastPeriodo == preparado.IdsPeriodos[indicePeriodo]);
        var idEstado = await contexto.EstadosPlanilla
            .Where(item => item.Nombre == estado)
            .Select(item => item.IdEstadoPlanilla)
            .SingleAsync();
        var periodo = new PeriodoPlanilla
        {
            CodigoPeriodo = $"QACR-{codigo}",
            Nombre = $"Comparación real {codigo}",
            TipoPeriodo = periodoForecast.TipoPeriodo,
            FechaInicio = periodoForecast.FechaInicio,
            FechaFin = periodoForecast.FechaFin,
            IdEstadoPlanilla = idEstado,
            FechaCreacion = DateTime.Now,
            EstadoRegistro = activa ? EstadosRegistro.Activo : EstadosRegistro.Inactivo,
            Planilla = new Planilla
            {
                IdEstadoPlanilla = idEstado,
                FechaCalculo = DateTime.Now.AddDays(-2),
                FechaAprobacion = estado is EstadosPlanilla.Aprobada or EstadosPlanilla.Cerrada
                    ? DateTime.Now.AddDays(-1)
                    : null,
                FechaCierre = estado == EstadosPlanilla.Cerrada ? DateTime.Now : null,
                SalarioBrutoTotal = detalles.Sum(item => item.SalarioBruto) + ajusteBrutoCabecera,
                DeduccionesTotal = detalles.Sum(item => item.Deducciones),
                SalarioNetoTotal = detalles.Sum(item => item.SalarioNeto),
                FechaCreacion = DateTime.Now,
                EstadoRegistro = activa ? EstadosRegistro.Activo : EstadosRegistro.Inactivo
            }
        };
        foreach (var detalle in detalles)
        {
            periodo.Planilla!.Detalles.Add(new DetallePlanilla
            {
                IdColaborador = detalle.IdColaborador,
                SalarioBase = detalle.SalarioBruto,
                SalarioProporcional = detalle.SalarioBruto,
                SalarioBruto = detalle.SalarioBruto,
                TotalDeducciones = detalle.Deducciones,
                SalarioNeto = detalle.SalarioNeto,
                FechaCreacion = DateTime.Now
            });
        }

        contexto.PeriodosPlanilla.Add(periodo);
        await contexto.SaveChangesAsync();
        return periodo.Planilla!.IdPlanilla;
    }

    internal static SolicitudCompararForecastReal SolicitudPeriodo() => new()
    {
        Dimension = DimensionesComparacionRealForecast.Periodo
    };

    private static string NuevoToken() => Guid.NewGuid().ToString("N")[..8];

    internal sealed record DetalleRealDefinicion(
        long IdColaborador,
        decimal SalarioBruto,
        decimal Deducciones,
        decimal SalarioNeto);
}
