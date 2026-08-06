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
public sealed class MySqlForecastComparacionIntegrationTests
{
    [MySqlQaFact]
    public async Task GuardarYComparar_ConservaGrafoMuestraDiferenciasYTransicionaEstados()
    {
        var token = NuevoToken();

        try
        {
            var preparado = await MySqlForecastResultadoIntegrationTests
                .PrepararEscenarioCalculadoAsync(token);
            await AsegurarParametroBaseAsync(preparado.IdEscenario);
            var idAlternativo = await DuplicarEscenarioCalculadoAsync(
                preparado.IdEscenario,
                token,
                excluirContratacionPrevista: true);

            await using var contexto = MySqlForecastMovimientoPersonalIntegrationTests.CrearContexto();
            var escenarios = new ForecastEscenarioService(contexto);
            var comparaciones = new ForecastComparacionEscenarioService(contexto);
            var conteosAntes = await ObtenerConteosAsync(
                contexto,
                preparado.IdEscenario,
                idAlternativo);

            await escenarios.GuardarAsync(
                preparado.IdEscenario,
                preparado.Datos.IdActorRecursosHumanos);
            await escenarios.GuardarAsync(
                idAlternativo,
                preparado.Datos.IdActorRecursosHumanos);
            contexto.ChangeTracker.Clear();

            var guardados = await contexto.ForecastEscenarios
                .AsNoTracking()
                .Where(item => item.IdForecastEscenario == preparado.IdEscenario
                    || item.IdForecastEscenario == idAlternativo)
                .ToListAsync();
            Assert.All(guardados, item => Assert.Equal(
                EstadosEscenarioForecast.Guardado,
                item.EstadoEscenario));

            var opciones = await comparaciones.ObtenerEscenariosElegiblesAsync(
                preparado.Datos.IdActorRecursosHumanos);
            Assert.Contains(opciones, item => item.IdForecastEscenario == preparado.IdEscenario);
            Assert.Contains(opciones, item => item.IdForecastEscenario == idAlternativo);
            Assert.DoesNotContain(opciones, item =>
                item.IdForecastEscenario == preparado.Datos.IdEscenarioComposicion);

            ComparacionEscenariosForecast? porProyecto = null;
            foreach (var dimension in DimensionesResultadoForecast.Todas)
            {
                var resultado = await comparaciones.CompararAsync(
                    new SolicitudCompararEscenariosForecast
                    {
                        IdEscenarioBase = preparado.IdEscenario,
                        IdEscenarioAlternativo = idAlternativo,
                        Dimension = dimension
                    },
                    preparado.Datos.IdActorRecursosHumanos);

                Assert.Equal(EstadosEscenarioForecast.Comparado, resultado.EscenarioBase.EstadoEscenario);
                Assert.Equal(EstadosEscenarioForecast.Comparado, resultado.EscenarioAlternativo.EstadoEscenario);
                Assert.NotEmpty(resultado.Filas);
                Assert.Equal(2, resultado.Periodos.Count);
                Assert.All(resultado.Filas, fila => Assert.Equal(
                    CatalogoConceptosResultadoForecast.Todos.Count,
                    fila.Conceptos.Count));

                if (dimension == DimensionesResultadoForecast.Proyecto)
                {
                    porProyecto = resultado;
                }
            }

            Assert.NotNull(porProyecto);
            Assert.NotEqual(0m, porProyecto.Totales.DiferenciaSalarioBruto);
            Assert.Equal(
                porProyecto.Totales.SalarioBrutoAlternativo
                    - porProyecto.Totales.SalarioBrutoBase,
                porProyecto.Totales.DiferenciaSalarioBruto);
            var ajuste = Assert.Single(porProyecto.Parametros, item =>
                item.Codigo == CodigosParametroForecast.AjusteSalarial);
            Assert.True(ajuste.EstaHabilitadoBase);
            Assert.True(ajuste.EstaHabilitadoAlternativo);
            Assert.Equal(2m, ajuste.ValorBase);
            Assert.Equal(5m, ajuste.ValorAlternativo);

            var porColaborador = await comparaciones.CompararAsync(
                new SolicitudCompararEscenariosForecast
                {
                    IdEscenarioBase = preparado.IdEscenario,
                    IdEscenarioAlternativo = idAlternativo,
                    Dimension = DimensionesResultadoForecast.Colaborador
                },
                preparado.Datos.IdActorRecursosHumanos);
            Assert.Contains(porColaborador.Filas, fila =>
                fila.Clave.StartsWith("PREV:", StringComparison.Ordinal)
                && fila.Valores.SalarioBrutoBase > 0m
                && fila.Valores.SalarioBrutoAlternativo == 0m);

            var primerPeriodo = await comparaciones.CompararAsync(
                new SolicitudCompararEscenariosForecast
                {
                    IdEscenarioBase = preparado.IdEscenario,
                    IdEscenarioAlternativo = idAlternativo,
                    Dimension = DimensionesResultadoForecast.Proyecto,
                    NumeroOrdenPeriodo = 1
                },
                preparado.Datos.IdActorRecursosHumanos);
            Assert.Equal(1, primerPeriodo.NumeroOrdenPeriodo);
            Assert.NotEmpty(primerPeriodo.Filas);

            var invertida = await comparaciones.CompararAsync(
                new SolicitudCompararEscenariosForecast
                {
                    IdEscenarioBase = idAlternativo,
                    IdEscenarioAlternativo = preparado.IdEscenario,
                    Dimension = DimensionesResultadoForecast.Proyecto
                },
                preparado.Datos.IdActorRecursosHumanos);
            Assert.Equal(
                -porProyecto.Totales.DiferenciaSalarioBruto,
                invertida.Totales.DiferenciaSalarioBruto);

            contexto.ChangeTracker.Clear();
            var comparados = await contexto.ForecastEscenarios
                .AsNoTracking()
                .Where(item => item.IdForecastEscenario == preparado.IdEscenario
                    || item.IdForecastEscenario == idAlternativo)
                .ToListAsync();
            Assert.All(comparados, item => Assert.Equal(
                EstadosEscenarioForecast.Comparado,
                item.EstadoEscenario));
            Assert.All(comparados, item => Assert.Null(item.MontoRealTotal));
            Assert.All(comparados, item => Assert.Null(item.DiferenciaTotal));
            var detalles = await contexto.ForecastDetalles
                .AsNoTracking()
                .Where(item => item.IdForecastEscenario == preparado.IdEscenario
                    || item.IdForecastEscenario == idAlternativo)
                .ToListAsync();
            Assert.All(detalles, item =>
            {
                Assert.Null(item.MontoReal);
                Assert.Null(item.Diferencia);
            });
            Assert.Equal(
                conteosAntes,
                await ObtenerConteosAsync(contexto, preparado.IdEscenario, idAlternativo));
        }
        finally
        {
            await MySqlForecastMovimientoPersonalIntegrationTests.LimpiarDatosAsync(token);
        }
    }

    [MySqlQaFact]
    public async Task Guardar_RechazaEscenarioIncompletoYEsIdempotenteAlFinalizar()
    {
        var token = NuevoToken();

        try
        {
            var preparado = await MySqlForecastResultadoIntegrationTests
                .PrepararEscenarioCalculadoAsync(token);
            await using var contexto = MySqlForecastMovimientoPersonalIntegrationTests.CrearContexto();
            var servicio = new ForecastEscenarioService(contexto);

            await Assert.ThrowsAsync<InvalidOperationException>(() => servicio.GuardarAsync(
                preparado.Datos.IdEscenarioComposicion,
                preparado.Datos.IdActorRecursosHumanos));
            await Assert.ThrowsAsync<InvalidOperationException>(() => servicio.GuardarAsync(
                preparado.Datos.IdEscenarioCalculado,
                preparado.Datos.IdActorRecursosHumanos));
            await Assert.ThrowsAsync<KeyNotFoundException>(() => servicio.GuardarAsync(
                long.MaxValue,
                preparado.Datos.IdActorRecursosHumanos));

            await servicio.GuardarAsync(
                preparado.IdEscenario,
                preparado.Datos.IdActorRecursosHumanos);
            contexto.ChangeTracker.Clear();
            var primeraModificacion = await contexto.ForecastEscenarios
                .AsNoTracking()
                .Where(item => item.IdForecastEscenario == preparado.IdEscenario)
                .Select(item => item.FechaModificacion)
                .SingleAsync();
            await servicio.GuardarAsync(
                preparado.IdEscenario,
                preparado.Datos.IdActorRecursosHumanos);
            var segundaModificacion = await contexto.ForecastEscenarios
                .AsNoTracking()
                .Where(item => item.IdForecastEscenario == preparado.IdEscenario)
                .Select(item => item.FechaModificacion)
                .SingleAsync();

            Assert.Equal(primeraModificacion, segundaModificacion);
            Assert.Empty(contexto.ChangeTracker.Entries());
        }
        finally
        {
            await MySqlForecastMovimientoPersonalIntegrationTests.LimpiarDatosAsync(token);
        }
    }

    [MySqlQaFact]
    public async Task Comparar_RechazaSeguridadIdentidadYPeriodosIncompatiblesSinCambiarEstados()
    {
        var token = NuevoToken();

        try
        {
            var preparado = await MySqlForecastResultadoIntegrationTests
                .PrepararEscenarioCalculadoAsync(token);
            var idAlternativo = await DuplicarEscenarioCalculadoAsync(
                preparado.IdEscenario,
                token,
                excluirContratacionPrevista: false);
            await using var contexto = MySqlForecastMovimientoPersonalIntegrationTests.CrearContexto();
            var escenarios = new ForecastEscenarioService(contexto);
            var comparaciones = new ForecastComparacionEscenarioService(contexto);
            await escenarios.GuardarAsync(
                preparado.IdEscenario,
                preparado.Datos.IdActorRecursosHumanos);
            await escenarios.GuardarAsync(
                idAlternativo,
                preparado.Datos.IdActorRecursosHumanos);
            contexto.ChangeTracker.Clear();

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
                    comparaciones.ObtenerEscenariosElegiblesAsync(actor));
                await Assert.ThrowsAsync<UnauthorizedAccessException>(() => comparaciones.CompararAsync(
                    SolicitudValida(preparado.IdEscenario, idAlternativo),
                    actor));
            }

            await Assert.ThrowsAsync<ValidationException>(() => comparaciones.CompararAsync(
                SolicitudValida(preparado.IdEscenario, preparado.IdEscenario),
                preparado.Datos.IdActorRecursosHumanos));

            var periodoAlternativo = await contexto.ForecastPeriodos
                .SingleAsync(item => item.IdForecastEscenario == idAlternativo
                    && item.NumeroOrden == 2);
            periodoAlternativo.FechaFin = periodoAlternativo.FechaFin.AddDays(1);
            await contexto.SaveChangesAsync();
            contexto.ChangeTracker.Clear();

            await Assert.ThrowsAsync<ValidationException>(() => comparaciones.CompararAsync(
                SolicitudValida(preparado.IdEscenario, idAlternativo),
                preparado.Datos.IdActorRecursosHumanos));

            var estados = await contexto.ForecastEscenarios
                .AsNoTracking()
                .Where(item => item.IdForecastEscenario == preparado.IdEscenario
                    || item.IdForecastEscenario == idAlternativo)
                .Select(item => item.EstadoEscenario)
                .ToListAsync();
            Assert.All(estados, estado => Assert.Equal(EstadosEscenarioForecast.Guardado, estado));
            Assert.Empty(contexto.ChangeTracker.Entries());
        }
        finally
        {
            await MySqlForecastMovimientoPersonalIntegrationTests.LimpiarDatosAsync(token);
        }
    }

    private static async Task AsegurarParametroBaseAsync(long idEscenario)
    {
        await using var contexto = MySqlForecastMovimientoPersonalIntegrationTests.CrearContexto();
        var parametro = await contexto.ForecastParametros.FirstOrDefaultAsync(item =>
            item.IdForecastEscenario == idEscenario
            && item.Codigo == CodigosParametroForecast.AjusteSalarial);
        if (parametro is null)
        {
            contexto.ForecastParametros.Add(new ForecastParametro
            {
                IdForecastEscenario = idEscenario,
                Codigo = CodigosParametroForecast.AjusteSalarial,
                Nombre = "Ajuste salarial",
                TipoParametro = TiposParametroPlanilla.Porcentaje,
                ValorDecimal = 2m,
                EstadoRegistro = EstadosRegistro.Activo
            });
        }
        else
        {
            parametro.TipoParametro = TiposParametroPlanilla.Porcentaje;
            parametro.ValorDecimal = 2m;
            parametro.EstadoRegistro = EstadosRegistro.Activo;
        }

        await contexto.SaveChangesAsync();
    }

    private static async Task<long> DuplicarEscenarioCalculadoAsync(
        long idOrigen,
        string token,
        bool excluirContratacionPrevista)
    {
        await using var contexto = MySqlForecastMovimientoPersonalIntegrationTests.CrearContexto();
        var origen = await contexto.ForecastEscenarios.AsNoTracking()
            .SingleAsync(item => item.IdForecastEscenario == idOrigen);
        var periodosOrigen = await contexto.ForecastPeriodos.AsNoTracking()
            .Where(item => item.IdForecastEscenario == idOrigen)
            .OrderBy(item => item.NumeroOrden)
            .ToListAsync();
        var fuentesOrigen = await contexto.ForecastFuentesHistoricas.AsNoTracking()
            .Where(item => item.IdForecastEscenario == idOrigen)
            .OrderBy(item => item.NumeroOrden)
            .ToListAsync();
        var parametrosOrigen = await contexto.ForecastParametros.AsNoTracking()
            .Where(item => item.IdForecastEscenario == idOrigen)
            .ToListAsync();
        var participantesOrigen = await contexto.ForecastParticipantes.AsNoTracking()
            .Where(item => item.IdForecastEscenario == idOrigen)
            .ToListAsync();
        var asignacionesOrigen = await contexto.ForecastAsignacionesProyecto.AsNoTracking()
            .Where(item => item.IdForecastEscenario == idOrigen)
            .ToListAsync();
        var detallesOrigen = await contexto.ForecastDetalles.AsNoTracking()
            .Where(item => item.IdForecastEscenario == idOrigen)
            .ToListAsync();
        var idsContrataciones = participantesOrigen
            .Where(item => item.TipoParticipante == TiposParticipanteForecast.ContratacionPrevista)
            .Select(item => item.IdForecastParticipante)
            .ToHashSet();
        var participantesIncluidos = participantesOrigen
            .Where(item => !excluirContratacionPrevista
                || !idsContrataciones.Contains(item.IdForecastParticipante))
            .ToList();

        var alternativo = new ForecastEscenario
        {
            Nombre = $"Alternativo QA {token}",
            Descripcion = "Escenario alternativo para comparación QA",
            FechaInicioProyeccion = origen.FechaInicioProyeccion,
            FechaFinProyeccion = origen.FechaFinProyeccion,
            PeriodosHistoricosConsiderados = origen.PeriodosHistoricosConsiderados,
            EstadoEscenario = EstadosEscenarioForecast.Calculado,
            FechaCalculo = origen.FechaCalculo,
            FechaCreacion = DateTime.Now,
            CreadoPor = origen.CreadoPor,
            EstadoRegistro = EstadosRegistro.Activo
        };
        foreach (var periodo in periodosOrigen)
        {
            alternativo.Periodos.Add(new ForecastPeriodo
            {
                NumeroOrden = periodo.NumeroOrden,
                TipoPeriodo = periodo.TipoPeriodo,
                FechaInicio = periodo.FechaInicio,
                FechaFin = periodo.FechaFin,
                EstadoRegistro = periodo.EstadoRegistro
            });
        }
        foreach (var fuente in fuentesOrigen)
        {
            alternativo.FuentesHistoricas.Add(new ForecastFuenteHistorica
            {
                IdPlanilla = fuente.IdPlanilla,
                NumeroOrden = fuente.NumeroOrden
            });
        }
        foreach (var parametro in parametrosOrigen)
        {
            alternativo.Parametros.Add(new ForecastParametro
            {
                Codigo = parametro.Codigo,
                Nombre = parametro.Nombre,
                TipoParametro = parametro.TipoParametro,
                ValorDecimal = parametro.Codigo == CodigosParametroForecast.AjusteSalarial
                    ? 5m
                    : parametro.ValorDecimal,
                ValorTexto = parametro.ValorTexto,
                Descripcion = parametro.Descripcion,
                EstadoRegistro = parametro.EstadoRegistro
            });
        }
        foreach (var participante in participantesIncluidos)
        {
            alternativo.Participantes.Add(new ForecastParticipante
            {
                CodigoParticipante = participante.CodigoParticipante,
                Etiqueta = participante.Etiqueta,
                TipoParticipante = participante.TipoParticipante,
                IdColaborador = participante.IdColaborador,
                IdDepartamento = participante.IdDepartamento,
                IdPuesto = participante.IdPuesto,
                SalarioBaseMensual = participante.SalarioBaseMensual,
                FechaInicioAplicacion = participante.FechaInicioAplicacion,
                FechaSalidaPrevista = participante.FechaSalidaPrevista,
                EstaIncluido = participante.EstaIncluido,
                EstadoRegistro = participante.EstadoRegistro
            });
        }

        contexto.ForecastEscenarios.Add(alternativo);
        await contexto.SaveChangesAsync();

        var periodosNuevos = alternativo.Periodos.ToDictionary(item => item.NumeroOrden);
        var participantesNuevos = alternativo.Participantes.ToDictionary(
            item => item.CodigoParticipante,
            StringComparer.Ordinal);
        var periodosPorId = periodosOrigen.ToDictionary(item => item.IdForecastPeriodo);
        var participantesPorId = participantesOrigen.ToDictionary(item => item.IdForecastParticipante);
        var asignacionesNuevas = new List<ForecastAsignacionProyecto>();
        foreach (var asignacion in asignacionesOrigen)
        {
            var participanteOrigen = participantesPorId[asignacion.IdForecastParticipante];
            if (!participantesNuevos.TryGetValue(
                    participanteOrigen.CodigoParticipante,
                    out var participanteNuevo))
            {
                continue;
            }

            asignacionesNuevas.Add(new ForecastAsignacionProyecto
            {
                IdForecastEscenario = alternativo.IdForecastEscenario,
                IdForecastPeriodo = periodosNuevos[periodosPorId[asignacion.IdForecastPeriodo].NumeroOrden].IdForecastPeriodo,
                IdForecastParticipante = participanteNuevo.IdForecastParticipante,
                IdProyecto = asignacion.IdProyecto,
                Porcentaje = asignacion.Porcentaje,
                EstadoRegistro = asignacion.EstadoRegistro
            });
        }
        contexto.ForecastAsignacionesProyecto.AddRange(asignacionesNuevas);
        await contexto.SaveChangesAsync();

        var asignacionesPorClave = asignacionesNuevas.ToDictionary(item => (
            item.IdForecastPeriodo,
            item.IdForecastParticipante,
            item.IdProyecto));
        var detallesNuevos = new List<ForecastDetalle>();
        foreach (var detalle in detallesOrigen)
        {
            var participanteOrigen = participantesPorId[detalle.IdForecastParticipante];
            if (!participantesNuevos.TryGetValue(
                    participanteOrigen.CodigoParticipante,
                    out var participanteNuevo))
            {
                continue;
            }

            var idPeriodoNuevo = periodosNuevos[
                periodosPorId[detalle.IdForecastPeriodo].NumeroOrden].IdForecastPeriodo;
            var asignacionNueva = asignacionesPorClave[(
                idPeriodoNuevo,
                participanteNuevo.IdForecastParticipante,
                detalle.IdProyecto)];
            detallesNuevos.Add(new ForecastDetalle
            {
                IdForecastEscenario = alternativo.IdForecastEscenario,
                IdForecastPeriodo = asignacionNueva.IdForecastPeriodo,
                IdForecastParticipante = asignacionNueva.IdForecastParticipante,
                IdProyecto = asignacionNueva.IdProyecto,
                Concepto = detalle.Concepto,
                MontoBase = detalle.MontoBase,
                MontoAjuste = detalle.MontoAjuste,
                MontoProyectado = detalle.MontoProyectado,
                MontoReal = null,
                Diferencia = null,
                Observaciones = detalle.Observaciones,
                EstadoRegistro = detalle.EstadoRegistro
            });
        }
        contexto.ForecastDetalles.AddRange(detallesNuevos);
        alternativo.MontoProyectadoTotal = detallesNuevos
            .Where(item => item.Concepto == ConceptosForecast.SalarioBruto
                && item.EstadoRegistro == EstadosRegistro.Activo)
            .Sum(item => item.MontoProyectado);
        await contexto.SaveChangesAsync();
        return alternativo.IdForecastEscenario;
    }

    private static SolicitudCompararEscenariosForecast SolicitudValida(
        long idBase,
        long idAlternativo) => new()
    {
        IdEscenarioBase = idBase,
        IdEscenarioAlternativo = idAlternativo,
        Dimension = DimensionesResultadoForecast.Periodo
    };

    private static async Task<ConteosGrafo> ObtenerConteosAsync(
        Microsoft.EntityFrameworkCore.DbContext contextoBase,
        long idBase,
        long idAlternativo)
    {
        var contexto = (SIGECDC.Persistence.Identity.ApplicationDbContext)contextoBase;
        var ids = new[] { idBase, idAlternativo };
        return new ConteosGrafo(
            await contexto.ForecastPeriodos.AsNoTracking().CountAsync(item => ids.Contains(item.IdForecastEscenario)),
            await contexto.ForecastFuentesHistoricas.AsNoTracking().CountAsync(item => ids.Contains(item.IdForecastEscenario)),
            await contexto.ForecastParametros.AsNoTracking().CountAsync(item => ids.Contains(item.IdForecastEscenario)),
            await contexto.ForecastParticipantes.AsNoTracking().CountAsync(item => ids.Contains(item.IdForecastEscenario)),
            await contexto.ForecastAsignacionesProyecto.AsNoTracking().CountAsync(item => ids.Contains(item.IdForecastEscenario)),
            await contexto.ForecastDetalles.AsNoTracking().CountAsync(item => ids.Contains(item.IdForecastEscenario)));
    }

    private static string NuevoToken() => Guid.NewGuid().ToString("N")[..8];

    private sealed record ConteosGrafo(
        int Periodos,
        int Fuentes,
        int Parametros,
        int Participantes,
        int Asignaciones,
        int Detalles);
}
