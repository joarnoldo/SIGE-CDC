using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using SIGECDC.Application.Forecast;
using SIGECDC.Domain.Forecast;
using SIGECDC.Domain.Operaciones;
using SIGECDC.Domain.SitioPublico;
using SIGECDC.Persistence.Forecast;
using SIGECDC.Tests.Activos;

namespace SIGECDC.Tests.Forecast;

[Collection("MySQL QA Miembro 2")]
public sealed class MySqlForecastResultadoIntegrationTests
{
    [MySqlQaFact]
    public async Task ForecastGenerado_ConsultaCuatroDimensionesConTotalesReproducibles()
    {
        var token = NuevoToken();

        try
        {
            var preparado = await PrepararEscenarioCalculadoAsync(token);
            await using var contexto = MySqlForecastMovimientoPersonalIntegrationTests.CrearContexto();
            var servicio = new ForecastResultadoService(contexto);
            var totalPersistido = await contexto.ForecastDetalles
                .AsNoTracking()
                .Where(item => item.IdForecastEscenario == preparado.IdEscenario
                    && item.EstadoRegistro == EstadosRegistro.Activo
                    && item.Concepto == ConceptosForecast.SalarioBruto)
                .SumAsync(item => item.MontoProyectado);
            contexto.ChangeTracker.Clear();

            var porColaborador = await ConsultarAsync(
                servicio,
                preparado,
                DimensionesResultadoForecast.Colaborador);
            var porDepartamento = await ConsultarAsync(
                servicio,
                preparado,
                DimensionesResultadoForecast.Departamento);
            var porProyecto = await ConsultarAsync(
                servicio,
                preparado,
                DimensionesResultadoForecast.Proyecto);
            var porPeriodo = await ConsultarAsync(
                servicio,
                preparado,
                DimensionesResultadoForecast.Periodo);

            Assert.Equal(2, porColaborador.Filas.Count);
            Assert.Equal(2, porDepartamento.Filas.Count);
            Assert.Equal(2, porProyecto.Filas.Count);
            Assert.Equal(2, porPeriodo.Filas.Count);
            Assert.Contains(porColaborador.Filas, item =>
                item.TipoParticipante == TiposParticipanteForecast.ContratacionPrevista
                && item.Etiqueta == preparado.EtiquetaContratacion);
            Assert.DoesNotContain(porColaborador.Filas, item =>
                item.Codigo.Contains("EXC", StringComparison.Ordinal));
            Assert.Contains(porDepartamento.Filas, item =>
                item.Etiqueta == preparado.NombreDepartamentoSecundario);
            Assert.Contains(porProyecto.Filas, item =>
                item.Codigo == preparado.CodigoProyectoSecundario);
            Assert.Equal([1, 2], porPeriodo.Filas.Select(item =>
                int.Parse(item.Clave[(item.Clave.IndexOf(':') + 1)..]) == preparado.IdsPeriodos[0]
                    ? 1
                    : 2));

            foreach (var consulta in new[]
                {
                    porColaborador,
                    porDepartamento,
                    porProyecto,
                    porPeriodo
                })
            {
                Assert.Equal(totalPersistido, consulta.MontoProyectadoTotalConsulta);
                Assert.Equal(totalPersistido, consulta.MontoProyectadoTotalEscenario);
                Assert.True(consulta.EsConsistenteConEscenario);
                Assert.All(consulta.Filas, fila => Assert.Equal(
                    CatalogoConceptosResultadoForecast.Todos.Count,
                    fila.Conceptos.Count));
            }

            var primerPeriodo = await servicio.ObtenerResultadosAsync(
                preparado.IdEscenario,
                new FiltroResultadosForecast
                {
                    Dimension = DimensionesResultadoForecast.Proyecto,
                    IdForecastPeriodo = preparado.IdsPeriodos[0]
                },
                preparado.Datos.IdActorRecursosHumanos);
            var subtotalEsperado = await contexto.ForecastDetalles
                .AsNoTracking()
                .Where(item => item.IdForecastEscenario == preparado.IdEscenario
                    && item.IdForecastPeriodo == preparado.IdsPeriodos[0]
                    && item.EstadoRegistro == EstadosRegistro.Activo
                    && item.Concepto == ConceptosForecast.SalarioBruto)
                .SumAsync(item => item.MontoProyectado);

            Assert.NotNull(primerPeriodo);
            Assert.Equal(subtotalEsperado, primerPeriodo.MontoProyectadoTotalConsulta);
            Assert.Null(primerPeriodo.EsConsistenteConEscenario);
            Assert.Empty(contexto.ChangeTracker.Entries());

            var escenarioDespues = await contexto.ForecastEscenarios
                .AsNoTracking()
                .SingleAsync(item => item.IdForecastEscenario == preparado.IdEscenario);
            Assert.Equal(preparado.FechaModificacion, escenarioDespues.FechaModificacion);
            Assert.Equal(preparado.Datos.IdActorRecursosHumanos, escenarioDespues.ModificadoPor);
            Assert.Equal(totalPersistido, escenarioDespues.MontoProyectadoTotal);
        }
        finally
        {
            await MySqlForecastMovimientoPersonalIntegrationTests.LimpiarDatosAsync(token);
        }
    }

    [MySqlQaFact]
    public async Task Consulta_AutorizaPrimeroYControlaEscenarioPeriodoYResultadoVacio()
    {
        var token = NuevoToken();

        try
        {
            var datos = await MySqlForecastMovimientoPersonalIntegrationTests.PrepararDatosAsync(token);
            await using var contexto = MySqlForecastMovimientoPersonalIntegrationTests.CrearContexto();
            var servicio = new ForecastResultadoService(contexto);
            var filtro = new FiltroResultadosForecast
            {
                Dimension = DimensionesResultadoForecast.Periodo
            };

            foreach (var actor in new[]
                {
                    " ",
                    "usuario-inexistente",
                    datos.IdActorInactivo,
                    datos.IdActorAdministrador,
                    datos.IdActorOperaciones,
                    datos.IdActorEmpleado
                })
            {
                await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                    servicio.ObtenerResultadosAsync(long.MaxValue, filtro, actor));
            }

            Assert.Null(await servicio.ObtenerResultadosAsync(
                long.MaxValue,
                filtro,
                datos.IdActorRecursosHumanos));
            Assert.Null(await servicio.ObtenerResultadosAsync(
                datos.IdEscenarioInactivo,
                filtro,
                datos.IdActorRecursosHumanos));

            var vacio = Assert.IsType<ConsultaResultadosForecast>(
                await servicio.ObtenerResultadosAsync(
                    datos.IdEscenarioCalculado,
                    filtro,
                    datos.IdActorRecursosHumanos));
            Assert.Empty(vacio.Filas);
            Assert.Null(vacio.FechaCalculo);
            Assert.Equal(0m, vacio.MontoProyectadoTotalConsulta);

            await Assert.ThrowsAsync<ValidationException>(() => servicio.ObtenerResultadosAsync(
                datos.IdEscenarioCalculado,
                new FiltroResultadosForecast
                {
                    Dimension = DimensionesResultadoForecast.Periodo,
                    IdForecastPeriodo = long.MaxValue
                },
                datos.IdActorRecursosHumanos));
            await Assert.ThrowsAsync<ValidationException>(() => servicio.ObtenerResultadosAsync(
                datos.IdEscenarioCalculado,
                new FiltroResultadosForecast { Dimension = "Costo" },
                datos.IdActorRecursosHumanos));
            Assert.Empty(contexto.ChangeTracker.Entries());
        }
        finally
        {
            await MySqlForecastMovimientoPersonalIntegrationTests.LimpiarDatosAsync(token);
        }
    }

    [MySqlQaFact]
    public async Task DetallesInactivos_NoSeMuestranYPeriodoSinDetalleConservaTotalCero()
    {
        var token = NuevoToken();

        try
        {
            var preparado = await PrepararEscenarioCalculadoAsync(token);
            await using (var alteracion = MySqlForecastMovimientoPersonalIntegrationTests.CrearContexto())
            {
                var detallesSegundoPeriodo = await alteracion.ForecastDetalles
                    .Where(item => item.IdForecastEscenario == preparado.IdEscenario
                        && item.IdForecastPeriodo == preparado.IdsPeriodos[1])
                    .ToListAsync();
                foreach (var detalle in detallesSegundoPeriodo)
                {
                    detalle.EstadoRegistro = EstadosRegistro.Inactivo;
                }

                await alteracion.SaveChangesAsync();
            }

            await using var contexto = MySqlForecastMovimientoPersonalIntegrationTests.CrearContexto();
            var servicio = new ForecastResultadoService(contexto);
            var resultado = Assert.IsType<ConsultaResultadosForecast>(
                await servicio.ObtenerResultadosAsync(
                    preparado.IdEscenario,
                    new FiltroResultadosForecast
                    {
                        Dimension = DimensionesResultadoForecast.Periodo
                    },
                    preparado.Datos.IdActorRecursosHumanos));

            Assert.Equal(2, resultado.Filas.Count);
            var segundoPeriodo = resultado.Filas.Single(item =>
                item.Clave == $"{DimensionesResultadoForecast.Periodo}:{preparado.IdsPeriodos[1]}");
            Assert.Equal(0m, segundoPeriodo.SalarioBrutoProyectado);
            Assert.Equal(0m, segundoPeriodo.DeduccionesProyectadas);
            Assert.Equal(0m, segundoPeriodo.SalarioNetoProyectado);
            Assert.All(segundoPeriodo.Conceptos, item => Assert.Equal(0m, item.MontoProyectado));
            Assert.False(resultado.EsConsistenteConEscenario);
            Assert.Empty(contexto.ChangeTracker.Entries());
        }
        finally
        {
            await MySqlForecastMovimientoPersonalIntegrationTests.LimpiarDatosAsync(token);
        }
    }

    private static async Task<ConsultaResultadosForecast> ConsultarAsync(
        ForecastResultadoService servicio,
        ResultadoPreparado preparado,
        string dimension) => Assert.IsType<ConsultaResultadosForecast>(
            await servicio.ObtenerResultadosAsync(
                preparado.IdEscenario,
                new FiltroResultadosForecast { Dimension = dimension },
                preparado.Datos.IdActorRecursosHumanos));

    internal static async Task<ResultadoPreparado> PrepararEscenarioCalculadoAsync(string token)
    {
        var datos = await MySqlForecastMovimientoPersonalIntegrationTests.PrepararDatosAsync(token);
        await using var contexto = MySqlForecastMovimientoPersonalIntegrationTests.CrearContexto();
        var proyectoPrincipal = await contexto.Proyectos
            .AsNoTracking()
            .SingleAsync(item => item.IdProyecto == datos.IdProyecto);
        var departamentoSecundario = await contexto.Departamentos
            .AsNoTracking()
            .SingleAsync(item => item.IdDepartamento == datos.IdDepartamentoSecundario);
        var puestoSecundario = await contexto.Puestos
            .AsNoTracking()
            .SingleAsync(item => item.IdPuesto == datos.IdPuestoSecundario);
        var periodos = await contexto.ForecastPeriodos
            .AsNoTracking()
            .Where(item => item.IdForecastEscenario == datos.IdEscenarioBorrador)
            .OrderBy(item => item.NumeroOrden)
            .ToListAsync();
        var codigoProyectoSecundario = $"QAR-{token}";
        var proyectoSecundario = new Proyecto
        {
            CodigoProyecto = codigoProyectoSecundario,
            NombreProyecto = $"Proyecto resultados {token}",
            IdEstadoProyecto = proyectoPrincipal.IdEstadoProyecto,
            FechaInicio = periodos[0].FechaInicio.AddMonths(-1),
            FechaFinEstimada = periodos[^1].FechaFin.AddMonths(1),
            FechaCreacion = DateTime.Now,
            EstadoRegistro = EstadosRegistro.Activo
        };
        var etiquetaContratacion = puestoSecundario.Nombre;
        var contratacion = new ForecastParticipante
        {
            IdForecastEscenario = datos.IdEscenarioBorrador,
            CodigoParticipante = $"PREV-{token.ToUpperInvariant()}",
            Etiqueta = etiquetaContratacion,
            TipoParticipante = TiposParticipanteForecast.ContratacionPrevista,
            IdDepartamento = departamentoSecundario.IdDepartamento,
            IdPuesto = puestoSecundario.IdPuesto,
            SalarioBaseMensual = 2_000m,
            FechaInicioAplicacion = periodos[0].FechaInicio,
            EstaIncluido = true,
            EstadoRegistro = EstadosRegistro.Activo
        };
        contexto.Proyectos.Add(proyectoSecundario);
        contexto.ForecastParticipantes.Add(contratacion);
        await contexto.SaveChangesAsync();

        contexto.ForecastAsignacionesProyecto.AddRange(
            CrearAsignacion(datos.IdEscenarioBorrador, periodos[0].IdForecastPeriodo, datos.IdParticipanteRealIncluido, datos.IdProyecto, 60m),
            CrearAsignacion(datos.IdEscenarioBorrador, periodos[0].IdForecastPeriodo, datos.IdParticipanteRealIncluido, proyectoSecundario.IdProyecto, 40m),
            CrearAsignacion(datos.IdEscenarioBorrador, periodos[1].IdForecastPeriodo, datos.IdParticipanteRealIncluido, datos.IdProyecto, 100m),
            CrearAsignacion(datos.IdEscenarioBorrador, periodos[0].IdForecastPeriodo, contratacion.IdForecastParticipante, proyectoSecundario.IdProyecto, 100m),
            CrearAsignacion(datos.IdEscenarioBorrador, periodos[1].IdForecastPeriodo, contratacion.IdForecastParticipante, proyectoSecundario.IdProyecto, 100m));
        await contexto.SaveChangesAsync();
        contexto.ChangeTracker.Clear();

        var calculo = new ForecastCalculoService(contexto);
        await calculo.GenerarForecastAsync(
            datos.IdEscenarioBorrador,
            datos.IdActorRecursosHumanos);
        contexto.ChangeTracker.Clear();
        var escenario = await contexto.ForecastEscenarios
            .AsNoTracking()
            .SingleAsync(item => item.IdForecastEscenario == datos.IdEscenarioBorrador);

        return new ResultadoPreparado(
            datos,
            datos.IdEscenarioBorrador,
            periodos.Select(item => item.IdForecastPeriodo).ToList(),
            codigoProyectoSecundario,
            departamentoSecundario.Nombre,
            etiquetaContratacion,
            escenario.FechaModificacion);
    }

    private static ForecastAsignacionProyecto CrearAsignacion(
        long idEscenario,
        long idPeriodo,
        long idParticipante,
        long idProyecto,
        decimal porcentaje) => new()
    {
        IdForecastEscenario = idEscenario,
        IdForecastPeriodo = idPeriodo,
        IdForecastParticipante = idParticipante,
        IdProyecto = idProyecto,
        Porcentaje = porcentaje,
        EstadoRegistro = EstadosRegistro.Activo
    };

    private static string NuevoToken() => Guid.NewGuid().ToString("N")[..8];

    internal sealed record ResultadoPreparado(
        MySqlForecastMovimientoPersonalIntegrationTests.DatosQa Datos,
        long IdEscenario,
        IReadOnlyList<long> IdsPeriodos,
        string CodigoProyectoSecundario,
        string NombreDepartamentoSecundario,
        string EtiquetaContratacion,
        DateTime? FechaModificacion);
}
