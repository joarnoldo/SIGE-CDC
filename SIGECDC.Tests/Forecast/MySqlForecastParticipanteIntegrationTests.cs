using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using SIGECDC.Application.Forecast;
using SIGECDC.Domain.Forecast;
using SIGECDC.Domain.RecursosHumanos;
using SIGECDC.Domain.SitioPublico;
using SIGECDC.Persistence.Forecast;
using SIGECDC.Persistence.Identity;
using SIGECDC.Tests.Activos;

namespace SIGECDC.Tests.Forecast;

[Collection("MySQL QA Miembro 2")]
public sealed class MySqlForecastParticipanteIntegrationTests
{
    [MySqlQaFact]
    public async Task GuardarComposicion_PersisteExclusionYReactivacionConSnapshotCongelado()
    {
        var token = NuevoToken();

        try
        {
            var datos = await MySqlForecastMovimientoPersonalIntegrationTests.PrepararDatosAsync(token);
            long idContratacion;
            long idAsignacion;

            await using (var preparacion = MySqlForecastMovimientoPersonalIntegrationTests.CrearContexto())
            {
                var periodo = await preparacion.ForecastPeriodos
                    .SingleAsync(item => item.IdForecastEscenario == datos.IdEscenarioComposicion
                        && item.NumeroOrden == 1);
                var contratacion = new ForecastParticipante
                {
                    IdForecastEscenario = datos.IdEscenarioComposicion,
                    CodigoParticipante = $"PREV-{token.ToUpperInvariant()}",
                    Etiqueta = datos.NombrePuestoPrincipal,
                    TipoParticipante = TiposParticipanteForecast.ContratacionPrevista,
                    IdDepartamento = datos.IdDepartamentoPrincipal,
                    IdPuesto = datos.IdPuestoPrincipal,
                    SalarioBaseMensual = 1_000m,
                    FechaInicioAplicacion = datos.FechaInicio,
                    EstaIncluido = true,
                    EstadoRegistro = EstadosRegistro.Activo
                };
                preparacion.ForecastParticipantes.Add(contratacion);
                await preparacion.SaveChangesAsync();
                var asignacion = new ForecastAsignacionProyecto
                {
                    IdForecastEscenario = datos.IdEscenarioComposicion,
                    IdForecastPeriodo = periodo.IdForecastPeriodo,
                    IdForecastParticipante = contratacion.IdForecastParticipante,
                    IdProyecto = datos.IdProyecto,
                    Porcentaje = 100m,
                    EstadoRegistro = EstadosRegistro.Activo
                };
                preparacion.ForecastAsignacionesProyecto.Add(asignacion);
                await preparacion.SaveChangesAsync();
                idContratacion = contratacion.IdForecastParticipante;
                idAsignacion = asignacion.IdForecastAsignacionProyecto;
            }

            await using var contexto = MySqlForecastMovimientoPersonalIntegrationTests.CrearContexto();
            var servicio = new ForecastParticipanteService(contexto);
            var inicial = Assert.IsType<ConfiguracionParticipantesForecast>(
                await servicio.ObtenerConfiguracionAsync(
                    datos.IdEscenarioComposicion,
                    datos.IdActorRecursosHumanos));
            var incluidoInicial = Buscar(inicial, datos.IdColaborador);
            var excluidoInicial = Buscar(inicial, datos.IdColaboradorExcluido);
            Assert.True(incluidoInicial.EstaIncluido);
            Assert.True(excluidoInicial.EstaIncluido);
            Assert.False(incluidoInicial.EstaPersistido);
            Assert.False(excluidoInicial.EstaPersistido);
            Assert.Empty(contexto.ChangeTracker.Entries());

            await servicio.GuardarComposicionAsync(
                datos.IdEscenarioComposicion,
                CrearSolicitud(inicial, datos.IdColaborador, datos.IdColaboradorExcluido),
                datos.IdActorRecursosHumanos);
            contexto.ChangeTracker.Clear();

            var persistidos = await contexto.ForecastParticipantes
                .AsNoTracking()
                .Where(item => item.IdForecastEscenario == datos.IdEscenarioComposicion
                    && (item.IdColaborador == datos.IdColaborador
                        || item.IdColaborador == datos.IdColaboradorExcluido))
                .OrderBy(item => item.IdColaborador)
                .ToListAsync();
            Assert.Equal(2, persistidos.Count);
            Assert.True(persistidos.Single(item => item.IdColaborador == datos.IdColaborador).EstaIncluido);
            var exclusionPersistida = persistidos.Single(
                item => item.IdColaborador == datos.IdColaboradorExcluido);
            Assert.False(exclusionPersistida.EstaIncluido);
            var idExclusion = exclusionPersistida.IdForecastParticipante;
            var codigoSnapshot = exclusionPersistida.CodigoParticipante;
            var etiquetaSnapshot = exclusionPersistida.Etiqueta;
            var salarioSnapshot = exclusionPersistida.SalarioBaseMensual;

            var recargada = Assert.IsType<ConfiguracionParticipantesForecast>(
                await servicio.ObtenerConfiguracionAsync(
                    datos.IdEscenarioComposicion,
                    datos.IdActorRecursosHumanos));
            Assert.False(Buscar(recargada, datos.IdColaboradorExcluido).EstaIncluido);

            var colaborador = await contexto.Colaboradores
                .SingleAsync(item => item.IdColaborador == datos.IdColaboradorExcluido);
            var contrato = await contexto.Contratos
                .SingleAsync(item => item.IdColaborador == datos.IdColaboradorExcluido);
            colaborador.Nombre = "Nombre actualizado fuera del escenario";
            contrato.SalarioBase = salarioSnapshot + 999m;
            await contexto.SaveChangesAsync();
            contexto.ChangeTracker.Clear();

            var antesReactivar = Assert.IsType<ConfiguracionParticipantesForecast>(
                await servicio.ObtenerConfiguracionAsync(
                    datos.IdEscenarioComposicion,
                    datos.IdActorRecursosHumanos));
            await servicio.GuardarComposicionAsync(
                datos.IdEscenarioComposicion,
                CrearSolicitud(antesReactivar, datos.IdColaborador, datos.IdColaboradorExcluido, true),
                datos.IdActorRecursosHumanos);
            contexto.ChangeTracker.Clear();

            var reactivada = await contexto.ForecastParticipantes
                .AsNoTracking()
                .SingleAsync(item => item.IdForecastParticipante == idExclusion);
            Assert.True(reactivada.EstaIncluido);
            Assert.Equal(codigoSnapshot, reactivada.CodigoParticipante);
            Assert.Equal(etiquetaSnapshot, reactivada.Etiqueta);
            Assert.Equal(salarioSnapshot, reactivada.SalarioBaseMensual);
            Assert.True(await contexto.ForecastParticipantes.AsNoTracking().AnyAsync(
                item => item.IdForecastParticipante == idContratacion
                    && item.TipoParticipante == TiposParticipanteForecast.ContratacionPrevista));
            Assert.True(await contexto.ForecastAsignacionesProyecto.AsNoTracking().AnyAsync(
                item => item.IdForecastAsignacionProyecto == idAsignacion));

            contrato = await contexto.Contratos
                .SingleAsync(item => item.IdColaborador == datos.IdColaboradorExcluido);
            contrato.EstadoRegistro = EstadosRegistro.Inactivo;
            await contexto.SaveChangesAsync();
            contexto.ChangeTracker.Clear();
            var noElegible = Assert.IsType<ConfiguracionParticipantesForecast>(
                await servicio.ObtenerConfiguracionAsync(
                    datos.IdEscenarioComposicion,
                    datos.IdActorRecursosHumanos));
            Assert.False(Buscar(noElegible, datos.IdColaboradorExcluido).EsElegible);
            await servicio.GuardarComposicionAsync(
                datos.IdEscenarioComposicion,
                CrearSolicitud(noElegible, datos.IdColaborador),
                datos.IdActorRecursosHumanos);
            contexto.ChangeTracker.Clear();
            var excluidaNoElegible = Assert.IsType<ConfiguracionParticipantesForecast>(
                await servicio.ObtenerConfiguracionAsync(
                    datos.IdEscenarioComposicion,
                    datos.IdActorRecursosHumanos));
            Assert.False(Buscar(excluidaNoElegible, datos.IdColaboradorExcluido).EstaIncluido);
            await Assert.ThrowsAsync<ValidationException>(() => servicio.GuardarComposicionAsync(
                datos.IdEscenarioComposicion,
                CrearSolicitud(
                    excluidaNoElegible,
                    datos.IdColaborador,
                    datos.IdColaboradorExcluido,
                    true),
                datos.IdActorRecursosHumanos));
            Assert.Empty(contexto.ChangeTracker.Entries());
        }
        finally
        {
            await MySqlForecastMovimientoPersonalIntegrationTests.LimpiarDatosAsync(token);
        }
    }

    [MySqlQaFact]
    public async Task SeguridadConcurrenciaYPersistencia_RechazanCambiosSinDatosParciales()
    {
        var token = NuevoToken();

        try
        {
            var datos = await MySqlForecastMovimientoPersonalIntegrationTests.PrepararDatosAsync(token);

            await using (var contexto = MySqlForecastMovimientoPersonalIntegrationTests.CrearContexto())
            {
                var servicio = new ForecastParticipanteService(contexto);
                await Assert.ThrowsAsync<UnauthorizedAccessException>(() => servicio.ObtenerConfiguracionAsync(
                    datos.IdEscenarioComposicion,
                    " "));
                await Assert.ThrowsAsync<UnauthorizedAccessException>(() => servicio.ObtenerConfiguracionAsync(
                    datos.IdEscenarioComposicion,
                    "usuario-inexistente"));
                await Assert.ThrowsAsync<UnauthorizedAccessException>(() => servicio.ObtenerConfiguracionAsync(
                    datos.IdEscenarioComposicion,
                    datos.IdActorInactivo));
                await Assert.ThrowsAsync<UnauthorizedAccessException>(() => servicio.ObtenerConfiguracionAsync(
                    datos.IdEscenarioComposicion,
                    datos.IdActorAdministrador));
                await Assert.ThrowsAsync<UnauthorizedAccessException>(() => servicio.ObtenerConfiguracionAsync(
                    datos.IdEscenarioComposicion,
                    datos.IdActorOperaciones));
                await Assert.ThrowsAsync<UnauthorizedAccessException>(() => servicio.ObtenerConfiguracionAsync(
                    datos.IdEscenarioComposicion,
                    datos.IdActorEmpleado));

                var calculado = Assert.IsType<ConfiguracionParticipantesForecast>(
                    await servicio.ObtenerConfiguracionAsync(
                        datos.IdEscenarioCalculado,
                        datos.IdActorRecursosHumanos));
                Assert.False(calculado.PuedeEditar);
                await Assert.ThrowsAsync<InvalidOperationException>(() => servicio.GuardarComposicionAsync(
                    datos.IdEscenarioCalculado,
                    CrearSolicitud(calculado),
                    datos.IdActorRecursosHumanos));
                Assert.Null(await servicio.ObtenerConfiguracionAsync(
                    datos.IdEscenarioInactivo,
                    datos.IdActorRecursosHumanos));
                await Assert.ThrowsAsync<KeyNotFoundException>(() => servicio.GuardarComposicionAsync(
                    datos.IdEscenarioInactivo,
                    new SolicitudGuardarParticipantesForecast { Participantes = [] },
                    datos.IdActorRecursosHumanos));
                Assert.Empty(contexto.ChangeTracker.Entries());
            }

            ConfiguracionParticipantesForecast solicitudObsoleta;
            await using (var contexto = MySqlForecastMovimientoPersonalIntegrationTests.CrearContexto())
            {
                var servicio = new ForecastParticipanteService(contexto);
                solicitudObsoleta = Assert.IsType<ConfiguracionParticipantesForecast>(
                    await servicio.ObtenerConfiguracionAsync(
                        datos.IdEscenarioComposicion,
                        datos.IdActorRecursosHumanos));
                var nuevo = new Colaborador
                {
                    CodigoColaborador = $"QAM-STL-{token}",
                    TipoIdentificacion = "QA",
                    Identificacion = $"QAM-STL-{token}",
                    Nombre = "Colaborador",
                    PrimerApellido = "Concurrente",
                    FechaIngreso = DateTime.Today.AddYears(-1),
                    IdEstadoLaboral = datos.IdEstadoLaboralActivo,
                    IdDepartamento = datos.IdDepartamentoPrincipal,
                    IdPuesto = datos.IdPuestoPrincipal,
                    FechaCreacion = DateTime.Now,
                    EstadoRegistro = EstadosRegistro.Activo
                };
                var contratoNuevo = new Contrato
                {
                    Colaborador = nuevo,
                    TipoContrato = "Indefinido",
                    FechaInicio = DateTime.Today.AddYears(-1),
                    SalarioBase = 1_800m,
                    PeriodicidadPago = "Mensual",
                    EstadoContrato = "Activo",
                    FechaCreacion = DateTime.Now,
                    EstadoRegistro = EstadosRegistro.Activo
                };
                contexto.Colaboradores.Add(nuevo);
                contexto.Contratos.Add(contratoNuevo);
                await contexto.SaveChangesAsync();
                contexto.ChangeTracker.Clear();

                await Assert.ThrowsAsync<ValidationException>(() => servicio.GuardarComposicionAsync(
                    datos.IdEscenarioComposicion,
                    CrearSolicitud(solicitudObsoleta, datos.IdColaborador),
                    datos.IdActorRecursosHumanos));
                Assert.Empty(contexto.ChangeTracker.Entries());
            }

            await using (var contexto = MySqlForecastMovimientoPersonalIntegrationTests.CrearContexto(
                new InvalidarParticipanteAntesDeGuardarInterceptor()))
            {
                var servicio = new ForecastParticipanteService(contexto);
                var actual = Assert.IsType<ConfiguracionParticipantesForecast>(
                    await servicio.ObtenerConfiguracionAsync(
                        datos.IdEscenarioComposicion,
                        datos.IdActorRecursosHumanos));
                await Assert.ThrowsAsync<DbUpdateException>(() => servicio.GuardarComposicionAsync(
                    datos.IdEscenarioComposicion,
                    CrearSolicitud(actual, datos.IdColaborador),
                    datos.IdActorRecursosHumanos));
                Assert.Empty(contexto.ChangeTracker.Entries());
            }

            await using var verificacion = MySqlForecastMovimientoPersonalIntegrationTests.CrearContexto();
            Assert.False(await verificacion.ForecastParticipantes.AsNoTracking().AnyAsync(
                item => item.IdForecastEscenario == datos.IdEscenarioComposicion));
            var escenario = await verificacion.ForecastEscenarios.AsNoTracking().SingleAsync(
                item => item.IdForecastEscenario == datos.IdEscenarioComposicion);
            Assert.Equal(0m, escenario.MontoProyectadoTotal);
            Assert.Null(escenario.FechaModificacion);
            Assert.Null(escenario.ModificadoPor);
        }
        finally
        {
            await MySqlForecastMovimientoPersonalIntegrationTests.LimpiarDatosAsync(token);
        }
    }

    [MySqlQaFact]
    public async Task ComposicionGuardada_GeneraResultadosSoloParaIncluidosYActualizaTotal()
    {
        var token = NuevoToken();

        try
        {
            var datos = await MySqlForecastMovimientoPersonalIntegrationTests.PrepararDatosAsync(token);
            await using var contexto = MySqlForecastMovimientoPersonalIntegrationTests.CrearContexto();
            var participantes = new ForecastParticipanteService(contexto);
            var inicial = Assert.IsType<ConfiguracionParticipantesForecast>(
                await participantes.ObtenerConfiguracionAsync(
                    datos.IdEscenarioComposicion,
                    datos.IdActorRecursosHumanos));
            await participantes.GuardarComposicionAsync(
                datos.IdEscenarioComposicion,
                CrearSolicitud(inicial, datos.IdColaborador),
                datos.IdActorRecursosHumanos);
            contexto.ChangeTracker.Clear();

            var idIncluido = await contexto.ForecastParticipantes
                .AsNoTracking()
                .Where(item => item.IdForecastEscenario == datos.IdEscenarioComposicion
                    && item.IdColaborador == datos.IdColaborador)
                .Select(item => item.IdForecastParticipante)
                .SingleAsync();
            var idExcluido = await contexto.ForecastParticipantes
                .AsNoTracking()
                .Where(item => item.IdForecastEscenario == datos.IdEscenarioComposicion
                    && item.IdColaborador == datos.IdColaboradorExcluido)
                .Select(item => item.IdForecastParticipante)
                .SingleAsync();
            var periodos = await contexto.ForecastPeriodos
                .AsNoTracking()
                .Where(item => item.IdForecastEscenario == datos.IdEscenarioComposicion)
                .Select(item => item.IdForecastPeriodo)
                .ToListAsync();
            contexto.ForecastAsignacionesProyecto.AddRange(periodos.Select(idPeriodo =>
                new ForecastAsignacionProyecto
                {
                    IdForecastEscenario = datos.IdEscenarioComposicion,
                    IdForecastPeriodo = idPeriodo,
                    IdForecastParticipante = idIncluido,
                    IdProyecto = datos.IdProyecto,
                    Porcentaje = 100m,
                    EstadoRegistro = EstadosRegistro.Activo
                }));
            await contexto.SaveChangesAsync();
            contexto.ChangeTracker.Clear();

            var calculo = new ForecastCalculoService(contexto);
            var resultado = await calculo.GenerarForecastAsync(
                datos.IdEscenarioComposicion,
                datos.IdActorRecursosHumanos);
            contexto.ChangeTracker.Clear();
            var detalles = await contexto.ForecastDetalles
                .AsNoTracking()
                .Where(item => item.IdForecastEscenario == datos.IdEscenarioComposicion)
                .ToListAsync();
            var escenario = await contexto.ForecastEscenarios
                .AsNoTracking()
                .SingleAsync(item => item.IdForecastEscenario == datos.IdEscenarioComposicion);

            Assert.NotEmpty(detalles);
            Assert.Contains(detalles, item => item.IdForecastParticipante == idIncluido);
            Assert.DoesNotContain(detalles, item => item.IdForecastParticipante == idExcluido);
            var sumaBruto = detalles
                .Where(item => item.Concepto == ConceptosForecast.SalarioBruto)
                .Sum(item => item.MontoProyectado);
            Assert.Equal(sumaBruto, resultado.MontoProyectadoTotal);
            Assert.Equal(sumaBruto, escenario.MontoProyectadoTotal);
            Assert.Equal(EstadosEscenarioForecast.Calculado, escenario.EstadoEscenario);
            var soloLectura = Assert.IsType<ConfiguracionParticipantesForecast>(
                await participantes.ObtenerConfiguracionAsync(
                    datos.IdEscenarioComposicion,
                    datos.IdActorRecursosHumanos));
            Assert.False(soloLectura.PuedeEditar);
            await Assert.ThrowsAsync<InvalidOperationException>(() => participantes.GuardarComposicionAsync(
                datos.IdEscenarioComposicion,
                CrearSolicitud(soloLectura, datos.IdColaborador),
                datos.IdActorRecursosHumanos));
            Assert.Empty(contexto.ChangeTracker.Entries());
        }
        finally
        {
            await MySqlForecastMovimientoPersonalIntegrationTests.LimpiarDatosAsync(token);
        }
    }

    private static ParticipanteForecastConfigurado Buscar(
        ConfiguracionParticipantesForecast configuracion,
        long idColaborador) => configuracion.Participantes.Single(
            item => item.IdColaborador == idColaborador);

    private static SolicitudGuardarParticipantesForecast CrearSolicitud(
        ConfiguracionParticipantesForecast configuracion,
        long? idIncluidoUno = null,
        long? idIncluidoDos = null,
        bool incluirSegundo = false) => new()
    {
        Participantes = configuracion.Participantes
            .Select(item => new SolicitudParticipanteForecast
            {
                IdColaborador = item.IdColaborador,
                EstaIncluido = item.IdColaborador == idIncluidoUno
                    || (incluirSegundo && item.IdColaborador == idIncluidoDos)
            })
            .ToList()
    };

    private static string NuevoToken() => Guid.NewGuid().ToString("N")[..8];

    private sealed class InvalidarParticipanteAntesDeGuardarInterceptor : SaveChangesInterceptor
    {
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            var participante = eventData.Context?.ChangeTracker
                .Entries<ForecastParticipante>()
                .FirstOrDefault(entrada => entrada.State == EntityState.Added);
            if (participante is not null)
            {
                participante.Property(item => item.IdDepartamento).CurrentValue = int.MaxValue;
            }

            return ValueTask.FromResult(result);
        }
    }
}
