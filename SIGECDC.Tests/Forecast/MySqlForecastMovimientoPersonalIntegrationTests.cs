using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using SIGECDC.Application.Forecast;
using SIGECDC.Domain.Forecast;
using SIGECDC.Domain.Operaciones;
using SIGECDC.Domain.Planillas;
using SIGECDC.Domain.RecursosHumanos;
using SIGECDC.Domain.SitioPublico;
using SIGECDC.Persistence.Forecast;
using SIGECDC.Persistence.Identity;
using SIGECDC.Tests.Activos;

namespace SIGECDC.Tests.Forecast;

[Collection("MySQL QA Miembro 2")]
public sealed class MySqlForecastMovimientoPersonalIntegrationTests
{
    [MySqlQaFact]
    public async Task Contrataciones_RegistrarEditarYDesactivar_ConservaIdentidadYSnapshotAnonimo()
    {
        var token = NuevoToken();

        try
        {
            var datos = await PrepararDatosAsync(token);
            await using var contexto = CrearContexto();
            var servicio = new ForecastMovimientoPersonalService(contexto);
            var solicitud = CrearSolicitud(
                datos.IdDepartamentoPrincipal,
                datos.IdPuestoPrincipal,
                1_250_000.50m,
                datos.FechaInicio);

            var idPrimera = await servicio.RegistrarContratacionPrevistaAsync(
                datos.IdEscenarioBorrador,
                solicitud,
                datos.IdActorRecursosHumanos);
            var idSegunda = await servicio.RegistrarContratacionPrevistaAsync(
                datos.IdEscenarioBorrador,
                solicitud,
                datos.IdActorRecursosHumanos);

            contexto.ChangeTracker.Clear();
            var iniciales = await contexto.ForecastParticipantes
                .AsNoTracking()
                .Where(item => item.IdForecastParticipante == idPrimera
                    || item.IdForecastParticipante == idSegunda)
                .OrderBy(item => item.IdForecastParticipante)
                .ToListAsync();
            Assert.Equal(2, iniciales.Count);
            Assert.NotEqual(iniciales[0].CodigoParticipante, iniciales[1].CodigoParticipante);
            Assert.All(iniciales, participante =>
            {
                Assert.Matches(new Regex("^PREV-[0-9A-F]{32}$"), participante.CodigoParticipante);
                Assert.Equal(TiposParticipanteForecast.ContratacionPrevista, participante.TipoParticipante);
                Assert.Null(participante.IdColaborador);
                Assert.Equal(datos.NombrePuestoPrincipal, participante.Etiqueta);
                Assert.True(participante.EstaIncluido);
                Assert.Equal(EstadosRegistro.Activo, participante.EstadoRegistro);
                Assert.Null(participante.FechaSalidaPrevista);
            });

            var codigoOriginal = iniciales.Single(item => item.IdForecastParticipante == idPrimera)
                .CodigoParticipante;
            var actualizada = CrearSolicitud(
                datos.IdDepartamentoSecundario,
                datos.IdPuestoSecundario,
                975_000.25m,
                datos.FechaSegundoPeriodoFin);
            await servicio.ActualizarContratacionPrevistaAsync(
                datos.IdEscenarioBorrador,
                idPrimera,
                actualizada,
                datos.IdActorRecursosHumanos);

            contexto.ChangeTracker.Clear();
            var participanteActualizado = await contexto.ForecastParticipantes
                .AsNoTracking()
                .SingleAsync(item => item.IdForecastParticipante == idPrimera);
            Assert.Equal(codigoOriginal, participanteActualizado.CodigoParticipante);
            Assert.Equal(datos.NombrePuestoSecundario, participanteActualizado.Etiqueta);
            Assert.Equal(datos.IdDepartamentoSecundario, participanteActualizado.IdDepartamento);
            Assert.Equal(datos.IdPuestoSecundario, participanteActualizado.IdPuesto);
            Assert.Equal(975_000.25m, participanteActualizado.SalarioBaseMensual);
            Assert.Equal(datos.FechaSegundoPeriodoFin, participanteActualizado.FechaInicioAplicacion);

            await servicio.DesactivarContratacionPrevistaAsync(
                datos.IdEscenarioBorrador,
                idPrimera,
                datos.IdActorRecursosHumanos);

            contexto.ChangeTracker.Clear();
            var desactivada = await contexto.ForecastParticipantes
                .AsNoTracking()
                .SingleAsync(item => item.IdForecastParticipante == idPrimera);
            var escenario = await contexto.ForecastEscenarios
                .AsNoTracking()
                .SingleAsync(item => item.IdForecastEscenario == datos.IdEscenarioBorrador);
            var configuracion = await servicio.ObtenerConfiguracionAsync(
                datos.IdEscenarioBorrador,
                datos.IdActorRecursosHumanos);

            Assert.False(desactivada.EstaIncluido);
            Assert.Equal(EstadosRegistro.Inactivo, desactivada.EstadoRegistro);
            Assert.Equal(codigoOriginal, desactivada.CodigoParticipante);
            Assert.NotNull(escenario.FechaModificacion);
            Assert.Equal(datos.IdActorRecursosHumanos, escenario.ModificadoPor);
            Assert.NotNull(configuracion);
            Assert.DoesNotContain(
                configuracion.Contrataciones,
                item => item.IdForecastParticipante == idPrimera);
            Assert.Contains(
                configuracion.Contrataciones,
                item => item.IdForecastParticipante == idSegunda);
            Assert.Empty(contexto.ChangeTracker.Entries());
        }
        finally
        {
            await LimpiarDatosAsync(token);
        }
    }

    [MySqlQaFact]
    public async Task Salidas_RegistrarLimpiarYRechazarEventosIncompatibles_NoModificaExpediente()
    {
        var token = NuevoToken();

        try
        {
            var datos = await PrepararDatosAsync(token);
            await using var contexto = CrearContexto();
            var servicio = new ForecastMovimientoPersonalService(contexto);
            var colaboradorAntes = await contexto.Colaboradores
                .AsNoTracking()
                .SingleAsync(item => item.IdColaborador == datos.IdColaborador);

            await servicio.GuardarSalidaPrevistaAsync(
                datos.IdEscenarioBorrador,
                datos.IdParticipanteRealIncluido,
                datos.FechaSegundoPeriodoInicio,
                datos.IdActorRecursosHumanos);
            contexto.ChangeTracker.Clear();
            Assert.Equal(
                datos.FechaSegundoPeriodoInicio,
                await contexto.ForecastParticipantes
                    .AsNoTracking()
                    .Where(item => item.IdForecastParticipante == datos.IdParticipanteRealIncluido)
                    .Select(item => item.FechaSalidaPrevista)
                    .SingleAsync());

            await servicio.GuardarSalidaPrevistaAsync(
                datos.IdEscenarioBorrador,
                datos.IdParticipanteRealIncluido,
                null,
                datos.IdActorRecursosHumanos);
            contexto.ChangeTracker.Clear();
            Assert.Null(await contexto.ForecastParticipantes
                .AsNoTracking()
                .Where(item => item.IdForecastParticipante == datos.IdParticipanteRealIncluido)
                .Select(item => item.FechaSalidaPrevista)
                .SingleAsync());

            var idContratacion = await servicio.RegistrarContratacionPrevistaAsync(
                datos.IdEscenarioBorrador,
                CrearSolicitud(
                    datos.IdDepartamentoPrincipal,
                    datos.IdPuestoPrincipal,
                    800_000m,
                    datos.FechaInicio),
                datos.IdActorRecursosHumanos);
            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                servicio.GuardarSalidaPrevistaAsync(
                    datos.IdEscenarioBorrador,
                    idContratacion,
                    datos.FechaSegundoPeriodoInicio,
                    datos.IdActorRecursosHumanos));
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                servicio.GuardarSalidaPrevistaAsync(
                    datos.IdEscenarioBorrador,
                    datos.IdParticipanteRealExcluido,
                    datos.FechaSegundoPeriodoInicio,
                    datos.IdActorRecursosHumanos));
            await Assert.ThrowsAsync<ValidationException>(() =>
                servicio.GuardarSalidaPrevistaAsync(
                    datos.IdEscenarioBorrador,
                    datos.IdParticipanteRealIncluido,
                    datos.FechaEnEspacio,
                    datos.IdActorRecursosHumanos));

            await servicio.GuardarSalidaPrevistaAsync(
                datos.IdEscenarioBorrador,
                datos.IdParticipanteRealExcluido,
                null,
                datos.IdActorRecursosHumanos);
            contexto.ChangeTracker.Clear();

            var colaboradorDespues = await contexto.Colaboradores
                .AsNoTracking()
                .SingleAsync(item => item.IdColaborador == datos.IdColaborador);
            Assert.Equal(colaboradorAntes.FechaSalida, colaboradorDespues.FechaSalida);
            Assert.Equal(colaboradorAntes.IdEstadoLaboral, colaboradorDespues.IdEstadoLaboral);
            Assert.Equal(colaboradorAntes.EstadoRegistro, colaboradorDespues.EstadoRegistro);
            Assert.Equal(colaboradorAntes.FechaModificacion, colaboradorDespues.FechaModificacion);
            Assert.Empty(contexto.ChangeTracker.Entries());
        }
        finally
        {
            await LimpiarDatosAsync(token);
        }
    }

    [MySqlQaFact]
    public async Task SeguridadEstadosCatalogosYFechas_RechazanCambiosSinDatosParciales()
    {
        var token = NuevoToken();

        try
        {
            var datos = await PrepararDatosAsync(token);
            var solicitudValida = CrearSolicitud(
                datos.IdDepartamentoPrincipal,
                datos.IdPuestoPrincipal,
                500_000m,
                datos.FechaInicio);

            await using (var contexto = CrearContexto())
            {
                var servicio = new ForecastMovimientoPersonalService(contexto);
                foreach (var actor in new[]
                {
                    " ",
                    $"inexistente-{token}",
                    datos.IdActorInactivo,
                    datos.IdActorAdministrador,
                    datos.IdActorOperaciones,
                    datos.IdActorEmpleado
                })
                {
                    await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                        servicio.ObtenerConfiguracionAsync(datos.IdEscenarioBorrador, actor));
                }

                var calculado = await servicio.ObtenerConfiguracionAsync(
                    datos.IdEscenarioCalculado,
                    datos.IdActorRecursosHumanos);
                Assert.NotNull(calculado);
                Assert.False(calculado.PuedeEditar);
                Assert.Null(await servicio.ObtenerConfiguracionAsync(
                    datos.IdEscenarioInactivo,
                    datos.IdActorRecursosHumanos));
                await Assert.ThrowsAsync<InvalidOperationException>(() =>
                    servicio.RegistrarContratacionPrevistaAsync(
                        datos.IdEscenarioCalculado,
                        solicitudValida,
                        datos.IdActorRecursosHumanos));
            }

            await using (var contexto = CrearContexto())
            {
                var servicio = new ForecastMovimientoPersonalService(contexto);
                var solicitudesInvalidas = new[]
                {
                    CrearSolicitud(
                        datos.IdDepartamentoInactivo,
                        datos.IdPuestoPrincipal,
                        500_000m,
                        datos.FechaInicio),
                    CrearSolicitud(
                        datos.IdDepartamentoPrincipal,
                        datos.IdPuestoInactivo,
                        500_000m,
                        datos.FechaInicio),
                    CrearSolicitud(
                        datos.IdDepartamentoPrincipal,
                        datos.IdPuestoSecundario,
                        500_000m,
                        datos.FechaInicio),
                    CrearSolicitud(
                        datos.IdDepartamentoPrincipal,
                        datos.IdPuestoPrincipal,
                        500_000m,
                        datos.FechaEnEspacio)
                };
                foreach (var solicitud in solicitudesInvalidas)
                {
                    await Assert.ThrowsAsync<ValidationException>(() =>
                        servicio.RegistrarContratacionPrevistaAsync(
                            datos.IdEscenarioBorrador,
                            solicitud,
                            datos.IdActorRecursosHumanos));
                    Assert.Empty(contexto.ChangeTracker.Entries());
                }
            }

            await using var verificacion = CrearContexto();
            Assert.False(await verificacion.ForecastParticipantes
                .AsNoTracking()
                .AnyAsync(item => item.IdForecastEscenario == datos.IdEscenarioBorrador
                    && item.TipoParticipante == TiposParticipanteForecast.ContratacionPrevista));
            var escenario = await verificacion.ForecastEscenarios
                .AsNoTracking()
                .SingleAsync(item => item.IdForecastEscenario == datos.IdEscenarioBorrador);
            Assert.Null(escenario.FechaModificacion);
            Assert.Null(escenario.ModificadoPor);
        }
        finally
        {
            await LimpiarDatosAsync(token);
        }
    }

    [MySqlQaFact]
    public async Task FalloDePersistencia_RevierteContratacionYMetadatosDelEscenario()
    {
        var token = NuevoToken();

        try
        {
            var datos = await PrepararDatosAsync(token);
            await using (var contexto = CrearContexto(new InvalidarContratacionAntesDeGuardarInterceptor()))
            {
                var servicio = new ForecastMovimientoPersonalService(contexto);
                await Assert.ThrowsAsync<DbUpdateException>(() =>
                    servicio.RegistrarContratacionPrevistaAsync(
                        datos.IdEscenarioBorrador,
                        CrearSolicitud(
                            datos.IdDepartamentoPrincipal,
                            datos.IdPuestoPrincipal,
                            500_000m,
                            datos.FechaInicio),
                        datos.IdActorRecursosHumanos));
                Assert.Empty(contexto.ChangeTracker.Entries());
            }

            await using var verificacion = CrearContexto();
            Assert.False(await verificacion.ForecastParticipantes
                .AsNoTracking()
                .AnyAsync(item => item.IdForecastEscenario == datos.IdEscenarioBorrador
                    && item.TipoParticipante == TiposParticipanteForecast.ContratacionPrevista));
            var escenario = await verificacion.ForecastEscenarios
                .AsNoTracking()
                .SingleAsync(item => item.IdForecastEscenario == datos.IdEscenarioBorrador);
            Assert.Null(escenario.FechaModificacion);
            Assert.Null(escenario.ModificadoPor);
        }
        finally
        {
            await LimpiarDatosAsync(token);
        }
    }

    [MySqlQaFact]
    public async Task GenerarForecast_AplicaAltaSalidaYDesactivacion_EnResultadosPersistidos()
    {
        var token = NuevoToken();

        try
        {
            var datos = await PrepararDatosAsync(token);
            var escenario = await CrearEscenarioCalculoAsync(datos, token);

            long idContratacionActiva;
            long idContratacionDesactivada;
            await using (var contexto = CrearContexto())
            {
                var movimientos = new ForecastMovimientoPersonalService(contexto);
                idContratacionActiva = await movimientos.RegistrarContratacionPrevistaAsync(
                    escenario.IdEscenario,
                    CrearSolicitud(
                        datos.IdDepartamentoPrincipal,
                        datos.IdPuestoPrincipal,
                        3_100m,
                        escenario.InicioSegundoPeriodo.AddDays(15)),
                    datos.IdActorRecursosHumanos);
                idContratacionDesactivada = await movimientos.RegistrarContratacionPrevistaAsync(
                    escenario.IdEscenario,
                    CrearSolicitud(
                        datos.IdDepartamentoPrincipal,
                        datos.IdPuestoPrincipal,
                        2_000m,
                        escenario.InicioPrimerPeriodo),
                    datos.IdActorRecursosHumanos);
                await movimientos.DesactivarContratacionPrevistaAsync(
                    escenario.IdEscenario,
                    idContratacionDesactivada,
                    datos.IdActorRecursosHumanos);
                await movimientos.GuardarSalidaPrevistaAsync(
                    escenario.IdEscenario,
                    escenario.IdParticipanteReal,
                    escenario.InicioTercerPeriodo,
                    datos.IdActorRecursosHumanos);
            }

            await CompletarAsignacionesAsync(
                escenario.IdEscenario,
                datos.IdProyecto,
                [escenario.IdParticipanteReal, idContratacionActiva]);

            ResultadoGeneracionForecast resultado;
            await using (var contexto = CrearContexto())
            {
                resultado = await new ForecastCalculoService(contexto).GenerarForecastAsync(
                    escenario.IdEscenario,
                    datos.IdActorRecursosHumanos);
            }

            await using var verificacion = CrearContexto();
            var detalles = await verificacion.ForecastDetalles
                .AsNoTracking()
                .Where(item => item.IdForecastEscenario == escenario.IdEscenario)
                .ToListAsync();
            var escenarioPersistido = await verificacion.ForecastEscenarios
                .AsNoTracking()
                .SingleAsync(item => item.IdForecastEscenario == escenario.IdEscenario);
            var colaborador = await verificacion.Colaboradores
                .AsNoTracking()
                .SingleAsync(item => item.IdColaborador == datos.IdColaborador);

            var brutoReal = BrutosPorPeriodo(detalles, escenario.IdParticipanteReal, escenario.IdsPeriodos);
            var brutoContratacion = BrutosPorPeriodo(detalles, idContratacionActiva, escenario.IdsPeriodos);
            var diasSegundoPeriodo = (escenario.FinSegundoPeriodo - escenario.InicioSegundoPeriodo).Days + 1;
            var diasActivosContratacion = (escenario.FinSegundoPeriodo
                - escenario.InicioSegundoPeriodo.AddDays(15)).Days + 1;
            var parcialEsperado = Math.Round(
                3_100m * diasActivosContratacion / diasSegundoPeriodo,
                2,
                MidpointRounding.AwayFromZero);
            var totalEsperado = 3_100m + 3_100m + parcialEsperado + 3_100m;

            Assert.Equal([3_100m, 3_100m, 0m], brutoReal);
            Assert.Equal([0m, parcialEsperado, 3_100m], brutoContratacion);
            Assert.DoesNotContain(detalles, item => item.IdForecastParticipante == idContratacionDesactivada);
            Assert.Equal(48, detalles.Count);
            Assert.Equal(totalEsperado, resultado.MontoProyectadoTotal);
            Assert.Equal(totalEsperado, escenarioPersistido.MontoProyectadoTotal);
            Assert.Equal(
                totalEsperado,
                detalles.Where(item => item.Concepto == ConceptosForecast.SalarioBruto)
                    .Sum(item => item.MontoProyectado));
            Assert.Equal(EstadosEscenarioForecast.Calculado, escenarioPersistido.EstadoEscenario);
            Assert.Null(colaborador.FechaSalida);
            Assert.Equal(datos.IdEstadoLaboralActivo, colaborador.IdEstadoLaboral);

            var consulta = await new ForecastMovimientoPersonalService(verificacion)
                .ObtenerConfiguracionAsync(escenario.IdEscenario, datos.IdActorRecursosHumanos);
            Assert.NotNull(consulta);
            Assert.False(consulta.PuedeEditar);
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                new ForecastMovimientoPersonalService(verificacion).GuardarSalidaPrevistaAsync(
                    escenario.IdEscenario,
                    escenario.IdParticipanteReal,
                    null,
                    datos.IdActorRecursosHumanos));
        }
        finally
        {
            await LimpiarDatosAsync(token);
        }
    }

    internal static async Task<DatosQa> PrepararDatosAsync(string token)
    {
        await using var contexto = CrearContexto();
        var nombresRoles = new[] { "Recursos Humanos", "Administrador", "Operaciones", "Empleado" };
        var roles = (await contexto.Roles.AsNoTracking().ToListAsync())
            .Where(rol => nombresRoles.Contains(rol.Name!, StringComparer.Ordinal))
            .ToDictionary(rol => rol.Name!, rol => rol.Id, StringComparer.Ordinal);
        var idEstadoLaboralActivo = await contexto.EstadosLaborales
            .Where(item => item.Nombre == "Activo")
            .Select(item => item.IdEstadoLaboral)
            .SingleAsync();
        var idEstadoPlanillaAprobada = await contexto.EstadosPlanilla
            .Where(item => item.Nombre == EstadosPlanilla.Aprobada)
            .Select(item => item.IdEstadoPlanilla)
            .SingleAsync();
        var idEstadoProyecto = await contexto.EstadosProyecto
            .Where(item => item.Nombre == EstadosProyecto.EnEjecucion)
            .Select(item => item.IdEstadoProyecto)
            .SingleAsync();

        var actorRrhh = CrearUsuario(token, "rrhh", true);
        var actorInactivo = CrearUsuario(token, "inactivo", false);
        var actorAdmin = CrearUsuario(token, "admin", true);
        var actorOperaciones = CrearUsuario(token, "operaciones", true);
        var actorEmpleado = CrearUsuario(token, "empleado", true);
        contexto.Users.AddRange(actorRrhh, actorInactivo, actorAdmin, actorOperaciones, actorEmpleado);
        contexto.UserRoles.AddRange(
            CrearRol(actorRrhh.Id, roles["Recursos Humanos"]),
            CrearRol(actorInactivo.Id, roles["Recursos Humanos"]),
            CrearRol(actorAdmin.Id, roles["Administrador"]),
            CrearRol(actorOperaciones.Id, roles["Operaciones"]),
            CrearRol(actorEmpleado.Id, roles["Empleado"]));

        var departamentoPrincipal = new Departamento
        {
            Nombre = $"QA Mov Principal {token}",
            EstadoRegistro = EstadosRegistro.Activo
        };
        var departamentoSecundario = new Departamento
        {
            Nombre = $"QA Mov Secundario {token}",
            EstadoRegistro = EstadosRegistro.Activo
        };
        var departamentoInactivo = new Departamento
        {
            Nombre = $"QA Mov Inactivo {token}",
            EstadoRegistro = EstadosRegistro.Inactivo
        };
        var puestoPrincipal = new Puesto
        {
            Nombre = $"QA Puesto Principal {token}",
            Departamento = departamentoPrincipal,
            EstadoRegistro = EstadosRegistro.Activo
        };
        var puestoSecundario = new Puesto
        {
            Nombre = $"QA Puesto Secundario {token}",
            Departamento = departamentoSecundario,
            EstadoRegistro = EstadosRegistro.Activo
        };
        var puestoInactivo = new Puesto
        {
            Nombre = $"QA Puesto Inactivo {token}",
            Departamento = departamentoPrincipal,
            EstadoRegistro = EstadosRegistro.Inactivo
        };
        var colaborador = new Colaborador
        {
            CodigoColaborador = $"QAM-{token}",
            TipoIdentificacion = "QA",
            Identificacion = $"QAM-{token}",
            Nombre = "Colaborador",
            PrimerApellido = "Movimientos",
            FechaIngreso = DateTime.Today.AddYears(-2),
            IdEstadoLaboral = idEstadoLaboralActivo,
            Departamento = departamentoPrincipal,
            Puesto = puestoPrincipal,
            FechaCreacion = DateTime.Now,
            EstadoRegistro = EstadosRegistro.Activo
        };
        var colaboradorExcluido = new Colaborador
        {
            CodigoColaborador = $"QAM-EXC-{token}",
            TipoIdentificacion = "QA",
            Identificacion = $"QAM-EXC-{token}",
            Nombre = "Colaborador",
            PrimerApellido = "Excluido",
            FechaIngreso = DateTime.Today.AddYears(-1),
            IdEstadoLaboral = idEstadoLaboralActivo,
            Departamento = departamentoPrincipal,
            Puesto = puestoPrincipal,
            FechaCreacion = DateTime.Now,
            EstadoRegistro = EstadosRegistro.Activo
        };
        var contratoColaborador = new Contrato
        {
            Colaborador = colaborador,
            TipoContrato = "Indefinido",
            FechaInicio = DateTime.Today.AddYears(-2),
            SalarioBase = 3_100m,
            Jornada = "Completa",
            PeriodicidadPago = "Mensual",
            EstadoContrato = "Activo",
            FechaCreacion = DateTime.Now,
            EstadoRegistro = EstadosRegistro.Activo
        };
        var contratoColaboradorExcluido = new Contrato
        {
            Colaborador = colaboradorExcluido,
            TipoContrato = "Indefinido",
            FechaInicio = DateTime.Today.AddYears(-1),
            SalarioBase = 2_500m,
            Jornada = "Completa",
            PeriodicidadPago = "Mensual",
            EstadoContrato = "Activo",
            FechaCreacion = DateTime.Now,
            EstadoRegistro = EstadosRegistro.Activo
        };
        var proyecto = new Proyecto
        {
            CodigoProyecto = $"QAM-{token}",
            NombreProyecto = $"Proyecto movimientos {token}",
            IdEstadoProyecto = idEstadoProyecto,
            FechaInicio = DateTime.Today.AddYears(-1),
            FechaFinEstimada = DateTime.Today.AddYears(3),
            FechaCreacion = DateTime.Now,
            EstadoRegistro = EstadosRegistro.Activo
        };
        contexto.Departamentos.AddRange(
            departamentoPrincipal,
            departamentoSecundario,
            departamentoInactivo);
        contexto.Puestos.AddRange(puestoPrincipal, puestoSecundario, puestoInactivo);
        contexto.Colaboradores.AddRange(colaborador, colaboradorExcluido);
        contexto.Contratos.AddRange(contratoColaborador, contratoColaboradorExcluido);
        contexto.Proyectos.Add(proyecto);

        var periodoHistorico = new PeriodoPlanilla
        {
            CodigoPeriodo = $"QAM-HIS-{token}",
            Nombre = $"Histórico movimientos {token}",
            TipoPeriodo = TiposPeriodoPlanilla.Mensual,
            FechaInicio = new DateTime(2026, 1, 1),
            FechaFin = new DateTime(2026, 1, 31),
            IdEstadoPlanilla = idEstadoPlanillaAprobada,
            FechaCreacion = DateTime.Now,
            EstadoRegistro = EstadosRegistro.Activo,
            Planilla = new Planilla
            {
                IdEstadoPlanilla = idEstadoPlanillaAprobada,
                FechaCalculo = DateTime.Now.AddDays(-2),
                FechaAprobacion = DateTime.Now.AddDays(-1),
                SalarioBrutoTotal = 3_100m,
                DeduccionesTotal = 0m,
                SalarioNetoTotal = 3_100m,
                FechaCreacion = DateTime.Now,
                EstadoRegistro = EstadosRegistro.Activo
            }
        };
        periodoHistorico.Planilla.Detalles.Add(new DetallePlanilla
        {
            Colaborador = colaborador,
            SalarioBase = 3_100m,
            SalarioProporcional = 3_100m,
            SalarioBruto = 3_100m,
            SalarioNeto = 3_100m,
            FechaCreacion = DateTime.Now
        });
        contexto.PeriodosPlanilla.Add(periodoHistorico);
        await contexto.SaveChangesAsync();

        var fechaInicio = PrimerDiaMes(DateTime.Today.AddMonths(2));
        var fechaPrimerPeriodoFin = fechaInicio.AddDays(9);
        var fechaSegundoPeriodoInicio = fechaInicio.AddDays(15);
        var fechaSegundoPeriodoFin = fechaInicio.AddDays(24);
        var escenarioBorrador = CrearEscenario(
            token,
            "BOR",
            EstadosEscenarioForecast.Borrador,
            EstadosRegistro.Activo,
            fechaInicio,
            fechaSegundoPeriodoFin,
            periodoHistorico.Planilla.IdPlanilla);
        escenarioBorrador.Periodos.Add(CrearPeriodo(1, fechaInicio, fechaPrimerPeriodoFin));
        escenarioBorrador.Periodos.Add(CrearPeriodo(2, fechaSegundoPeriodoInicio, fechaSegundoPeriodoFin));
        var participanteIncluido = CrearParticipanteReal(
            token,
            "INC",
            colaborador,
            departamentoPrincipal,
            puestoPrincipal,
            fechaInicio,
            true);
        var participanteExcluido = CrearParticipanteReal(
            token,
            "EXC",
            colaboradorExcluido,
            departamentoPrincipal,
            puestoPrincipal,
            fechaInicio,
            false);
        escenarioBorrador.Participantes.Add(participanteIncluido);
        escenarioBorrador.Participantes.Add(participanteExcluido);

        var escenarioCalculado = CrearEscenario(
            token,
            "CAL",
            EstadosEscenarioForecast.Calculado,
            EstadosRegistro.Activo,
            fechaInicio,
            fechaPrimerPeriodoFin,
            periodoHistorico.Planilla.IdPlanilla);
        escenarioCalculado.Periodos.Add(CrearPeriodo(1, fechaInicio, fechaPrimerPeriodoFin));
        var escenarioInactivo = CrearEscenario(
            token,
            "INA",
            EstadosEscenarioForecast.Borrador,
            EstadosRegistro.Inactivo,
            fechaInicio,
            fechaPrimerPeriodoFin,
            periodoHistorico.Planilla.IdPlanilla);
        escenarioInactivo.Periodos.Add(CrearPeriodo(1, fechaInicio, fechaPrimerPeriodoFin));
        var escenarioComposicion = CrearEscenario(
            token,
            "COM",
            EstadosEscenarioForecast.Borrador,
            EstadosRegistro.Activo,
            fechaInicio,
            fechaSegundoPeriodoFin,
            periodoHistorico.Planilla.IdPlanilla);
        escenarioComposicion.Periodos.Add(CrearPeriodo(1, fechaInicio, fechaPrimerPeriodoFin));
        escenarioComposicion.Periodos.Add(CrearPeriodo(2, fechaSegundoPeriodoInicio, fechaSegundoPeriodoFin));
        contexto.ForecastEscenarios.AddRange(
            escenarioBorrador,
            escenarioCalculado,
            escenarioInactivo,
            escenarioComposicion);
        await contexto.SaveChangesAsync();

        return new DatosQa(
            actorRrhh.Id,
            actorInactivo.Id,
            actorAdmin.Id,
            actorOperaciones.Id,
            actorEmpleado.Id,
            departamentoPrincipal.IdDepartamento,
            departamentoSecundario.IdDepartamento,
            departamentoInactivo.IdDepartamento,
            puestoPrincipal.IdPuesto,
            puestoSecundario.IdPuesto,
            puestoInactivo.IdPuesto,
            puestoPrincipal.Nombre,
            puestoSecundario.Nombre,
            colaborador.IdColaborador,
            colaboradorExcluido.IdColaborador,
            idEstadoLaboralActivo,
            proyecto.IdProyecto,
            periodoHistorico.Planilla.IdPlanilla,
            escenarioBorrador.IdForecastEscenario,
            escenarioCalculado.IdForecastEscenario,
            escenarioInactivo.IdForecastEscenario,
            escenarioComposicion.IdForecastEscenario,
            participanteIncluido.IdForecastParticipante,
            participanteExcluido.IdForecastParticipante,
            fechaInicio,
            fechaInicio.AddDays(12),
            fechaSegundoPeriodoInicio,
            fechaSegundoPeriodoFin);
    }

    private static async Task<EscenarioCalculoQa> CrearEscenarioCalculoAsync(DatosQa datos, string token)
    {
        await using var contexto = CrearContexto();
        var inicio = PrimerDiaMes(DateTime.Today.AddMonths(4));
        var periodos = Enumerable.Range(0, 3)
            .Select(indice => new ForecastPeriodo
            {
                NumeroOrden = indice + 1,
                TipoPeriodo = TiposPeriodoPlanilla.Mensual,
                FechaInicio = inicio.AddMonths(indice),
                FechaFin = inicio.AddMonths(indice + 1).AddDays(-1),
                EstadoRegistro = EstadosRegistro.Activo
            })
            .ToList();
        var escenario = CrearEscenario(
            token,
            "E2E",
            EstadosEscenarioForecast.Borrador,
            EstadosRegistro.Activo,
            periodos[0].FechaInicio,
            periodos[^1].FechaFin,
            datos.IdPlanillaAprobada);
        foreach (var periodo in periodos)
        {
            escenario.Periodos.Add(periodo);
        }

        escenario.Participantes.Add(new ForecastParticipante
        {
            CodigoParticipante = $"REAL-E2E-{token}",
            Etiqueta = "Colaborador Movimientos",
            TipoParticipante = TiposParticipanteForecast.Colaborador,
            IdColaborador = datos.IdColaborador,
            IdDepartamento = datos.IdDepartamentoPrincipal,
            IdPuesto = datos.IdPuestoPrincipal,
            SalarioBaseMensual = 3_100m,
            FechaInicioAplicacion = periodos[0].FechaInicio,
            EstaIncluido = true,
            EstadoRegistro = EstadosRegistro.Activo
        });
        contexto.ForecastEscenarios.Add(escenario);
        await contexto.SaveChangesAsync();

        return new EscenarioCalculoQa(
            escenario.IdForecastEscenario,
            escenario.Participantes.Single().IdForecastParticipante,
            periodos.Select(item => item.IdForecastPeriodo).ToList(),
            periodos[0].FechaInicio,
            periodos[1].FechaInicio,
            periodos[1].FechaFin,
            periodos[2].FechaInicio);
    }

    private static async Task CompletarAsignacionesAsync(
        long idEscenario,
        long idProyecto,
        IReadOnlyCollection<long> idsParticipantes)
    {
        await using var contexto = CrearContexto();
        var idsPeriodos = await contexto.ForecastPeriodos
            .AsNoTracking()
            .Where(item => item.IdForecastEscenario == idEscenario
                && item.EstadoRegistro == EstadosRegistro.Activo)
            .Select(item => item.IdForecastPeriodo)
            .ToListAsync();
        contexto.ForecastAsignacionesProyecto.AddRange(
            idsPeriodos.SelectMany(idPeriodo => idsParticipantes.Select(idParticipante =>
                new ForecastAsignacionProyecto
                {
                    IdForecastEscenario = idEscenario,
                    IdForecastPeriodo = idPeriodo,
                    IdForecastParticipante = idParticipante,
                    IdProyecto = idProyecto,
                    Porcentaje = 100m,
                    EstadoRegistro = EstadosRegistro.Activo
                })));
        await contexto.SaveChangesAsync();
    }

    private static IReadOnlyList<decimal> BrutosPorPeriodo(
        IReadOnlyCollection<ForecastDetalle> detalles,
        long idParticipante,
        IReadOnlyList<long> idsPeriodos) => idsPeriodos
        .Select(idPeriodo => detalles
            .Where(item => item.IdForecastParticipante == idParticipante
                && item.IdForecastPeriodo == idPeriodo
                && item.Concepto == ConceptosForecast.SalarioBruto)
            .Sum(item => item.MontoProyectado))
        .ToList();

    private static ForecastEscenario CrearEscenario(
        string token,
        string sufijo,
        string estadoEscenario,
        string estadoRegistro,
        DateTime inicio,
        DateTime fin,
        long idPlanilla) => new()
    {
        Nombre = $"QAM-{sufijo}-{token}",
        Descripcion = $"Escenario movimientos {token}",
        FechaInicioProyeccion = inicio,
        FechaFinProyeccion = fin,
        PeriodosHistoricosConsiderados = 1,
        EstadoEscenario = estadoEscenario,
        MontoProyectadoTotal = 0m,
        FechaCreacion = DateTime.Now,
        EstadoRegistro = estadoRegistro,
        FuentesHistoricas =
        [
            new ForecastFuenteHistorica
            {
                IdPlanilla = idPlanilla,
                NumeroOrden = 1
            }
        ]
    };

    private static ForecastPeriodo CrearPeriodo(int orden, DateTime inicio, DateTime fin) => new()
    {
        NumeroOrden = orden,
        TipoPeriodo = TiposPeriodoPlanilla.Mensual,
        FechaInicio = inicio,
        FechaFin = fin,
        EstadoRegistro = EstadosRegistro.Activo
    };

    private static ForecastParticipante CrearParticipanteReal(
        string token,
        string sufijo,
        Colaborador? colaborador,
        Departamento departamento,
        Puesto puesto,
        DateTime fechaInicio,
        bool incluido) => new()
    {
        CodigoParticipante = $"REAL-{sufijo}-{token}",
        Etiqueta = $"Colaborador {sufijo}",
        TipoParticipante = TiposParticipanteForecast.Colaborador,
        IdColaborador = colaborador?.IdColaborador,
        Colaborador = colaborador,
        IdDepartamento = departamento.IdDepartamento,
        IdPuesto = puesto.IdPuesto,
        SalarioBaseMensual = 3_100m,
        FechaInicioAplicacion = fechaInicio,
        EstaIncluido = incluido,
        EstadoRegistro = EstadosRegistro.Activo
    };

    private static SolicitudContratacionPrevistaForecast CrearSolicitud(
        int idDepartamento,
        int idPuesto,
        decimal salario,
        DateTime fecha) => new()
    {
        IdDepartamento = idDepartamento,
        IdPuesto = idPuesto,
        SalarioBaseMensual = salario,
        FechaInicioAplicacion = fecha
    };

    private static ApplicationUser CrearUsuario(string token, string tipo, bool activo)
    {
        var correo = $"forecast-mov-{tipo}-{token}@qa.local";
        return new ApplicationUser
        {
            Id = $"forecast-mov-{tipo}-{token}",
            UserName = correo,
            NormalizedUserName = correo.ToUpperInvariant(),
            Email = correo,
            NormalizedEmail = correo.ToUpperInvariant(),
            EmailConfirmed = true,
            EstadoRegistro = activo ? EstadosRegistro.Activo : EstadosRegistro.Inactivo,
            FechaCreacion = DateTime.Now,
            SecurityStamp = Guid.NewGuid().ToString("N")
        };
    }

    private static IdentityUserRole<string> CrearRol(string idUsuario, string idRol) => new()
    {
        UserId = idUsuario,
        RoleId = idRol
    };

    internal static ApplicationDbContext CrearContexto(SaveChangesInterceptor? interceptor = null)
    {
        var cadena = Environment.GetEnvironmentVariable(MySqlQaFactAttribute.VariableConexion);
        if (string.IsNullOrWhiteSpace(cadena)
            || !cadena.Contains("SIGE_CDC_DB_QA_MIEMBRO2", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "Las pruebas de forecast solo pueden ejecutarse contra SIGE_CDC_DB_QA_MIEMBRO2.");
        }

        var opciones = new DbContextOptionsBuilder<ApplicationDbContext>().UseMySQL(cadena);
        if (interceptor is not null)
        {
            opciones.AddInterceptors(interceptor);
        }

        return new ApplicationDbContext(opciones.Options);
    }

    internal static async Task LimpiarDatosAsync(string token)
    {
        await using var contexto = CrearContexto();
        var escenarios = await contexto.ForecastEscenarios
            .Where(item => item.Nombre.Contains(token))
            .ToListAsync();
        contexto.ForecastEscenarios.RemoveRange(escenarios);
        await contexto.SaveChangesAsync();

        var periodos = await contexto.PeriodosPlanilla
            .Include(item => item.Planilla)
            .Where(item => item.CodigoPeriodo.Contains(token))
            .ToListAsync();
        contexto.Planillas.RemoveRange(periodos
            .Where(item => item.Planilla is not null)
            .Select(item => item.Planilla!));
        await contexto.SaveChangesAsync();
        contexto.PeriodosPlanilla.RemoveRange(periodos);
        await contexto.SaveChangesAsync();

        var colaboradores = await contexto.Colaboradores
            .Where(item => item.CodigoColaborador.Contains(token))
            .ToListAsync();
        var idsColaboradores = colaboradores.Select(item => item.IdColaborador).ToList();
        var contratos = await contexto.Contratos
            .Where(item => idsColaboradores.Contains(item.IdColaborador))
            .ToListAsync();
        contexto.Contratos.RemoveRange(contratos);
        await contexto.SaveChangesAsync();
        contexto.Colaboradores.RemoveRange(colaboradores);
        var proyectos = await contexto.Proyectos
            .Where(item => item.CodigoProyecto.Contains(token))
            .ToListAsync();
        contexto.Proyectos.RemoveRange(proyectos);
        await contexto.SaveChangesAsync();

        var puestos = await contexto.Puestos
            .Where(item => item.Nombre.Contains(token))
            .ToListAsync();
        contexto.Puestos.RemoveRange(puestos);
        await contexto.SaveChangesAsync();
        var departamentos = await contexto.Departamentos
            .Where(item => item.Nombre.Contains(token))
            .ToListAsync();
        contexto.Departamentos.RemoveRange(departamentos);

        var rolesUsuarios = await contexto.UserRoles
            .Where(item => item.UserId.Contains(token))
            .ToListAsync();
        contexto.UserRoles.RemoveRange(rolesUsuarios);
        var usuarios = await contexto.Users
            .Where(item => item.Id.Contains(token))
            .ToListAsync();
        contexto.Users.RemoveRange(usuarios);
        await contexto.SaveChangesAsync();
    }

    private static DateTime PrimerDiaMes(DateTime fecha) => new(fecha.Year, fecha.Month, 1);

    private static string NuevoToken() => Guid.NewGuid().ToString("N")[..8];

    private sealed class InvalidarContratacionAntesDeGuardarInterceptor : SaveChangesInterceptor
    {
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            var participante = eventData.Context?.ChangeTracker
                .Entries<ForecastParticipante>()
                .FirstOrDefault(entrada => entrada.State == EntityState.Added
                    && entrada.Entity.TipoParticipante == TiposParticipanteForecast.ContratacionPrevista);
            if (participante is not null)
            {
                participante.Property(item => item.IdDepartamento).CurrentValue = int.MaxValue;
            }

            return ValueTask.FromResult(result);
        }
    }

    internal sealed record DatosQa(
        string IdActorRecursosHumanos,
        string IdActorInactivo,
        string IdActorAdministrador,
        string IdActorOperaciones,
        string IdActorEmpleado,
        int IdDepartamentoPrincipal,
        int IdDepartamentoSecundario,
        int IdDepartamentoInactivo,
        int IdPuestoPrincipal,
        int IdPuestoSecundario,
        int IdPuestoInactivo,
        string NombrePuestoPrincipal,
        string NombrePuestoSecundario,
        long IdColaborador,
        long IdColaboradorExcluido,
        int IdEstadoLaboralActivo,
        long IdProyecto,
        long IdPlanillaAprobada,
        long IdEscenarioBorrador,
        long IdEscenarioCalculado,
        long IdEscenarioInactivo,
        long IdEscenarioComposicion,
        long IdParticipanteRealIncluido,
        long IdParticipanteRealExcluido,
        DateTime FechaInicio,
        DateTime FechaEnEspacio,
        DateTime FechaSegundoPeriodoInicio,
        DateTime FechaSegundoPeriodoFin);

    private sealed record EscenarioCalculoQa(
        long IdEscenario,
        long IdParticipanteReal,
        IReadOnlyList<long> IdsPeriodos,
        DateTime InicioPrimerPeriodo,
        DateTime InicioSegundoPeriodo,
        DateTime FinSegundoPeriodo,
        DateTime InicioTercerPeriodo);
}
