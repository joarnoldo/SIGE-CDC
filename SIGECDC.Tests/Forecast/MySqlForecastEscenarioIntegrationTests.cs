using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using SIGECDC.Application.Forecast;
using SIGECDC.Domain.Forecast;
using SIGECDC.Domain.Planillas;
using SIGECDC.Domain.SitioPublico;
using SIGECDC.Persistence.Forecast;
using SIGECDC.Persistence.Identity;
using SIGECDC.Tests.Activos;

namespace SIGECDC.Tests.Forecast;

[Collection("MySQL QA Miembro 2")]
public sealed class MySqlForecastEscenarioIntegrationTests
{
    [MySqlQaFact]
    public async Task FuentesDisponibles_FiltraEstadosRegistrosYOrdenaPorFecha()
    {
        var token = NuevoToken();

        try
        {
            var datos = await PrepararDatosAsync(token);
            await using var contexto = CrearContexto();
            var servicio = new ForecastEscenarioService(contexto);

            var fuentes = await servicio.ObtenerFuentesHistoricasDisponiblesAsync(
                datos.IdActorRecursosHumanos);
            var fuentesPrueba = fuentes
                .Where(fuente => fuente.CodigoPeriodo.Contains(token))
                .ToList();

            Assert.Equal(2, fuentesPrueba.Count);
            Assert.Equal(datos.IdPlanillaCerrada, fuentesPrueba[0].IdPlanilla);
            Assert.Equal(datos.IdPlanillaAprobada, fuentesPrueba[1].IdPlanilla);
            Assert.Equal(EstadosPlanilla.Cerrada, fuentesPrueba[0].EstadoPlanilla);
            Assert.Equal(EstadosPlanilla.Aprobada, fuentesPrueba[1].EstadoPlanilla);
            Assert.DoesNotContain(fuentesPrueba, fuente =>
                fuente.IdPlanilla == datos.IdPlanillaBorrador
                || fuente.IdPlanilla == datos.IdPlanillaCalculada
                || fuente.IdPlanilla == datos.IdPlanillaEnRevision
                || fuente.IdPlanilla == datos.IdPlanillaAnulada
                || fuente.IdPlanilla == datos.IdPlanillaInactiva
                || fuente.IdPlanilla == datos.IdPlanillaPeriodoInactivo);
            Assert.Empty(contexto.ChangeTracker.Entries());
        }
        finally
        {
            await LimpiarDatosAsync(token);
        }
    }

    [MySqlQaFact]
    public async Task CrearYConsultar_PersisteGrafoDerivadoSinSeguimientoDeLectura()
    {
        var token = NuevoToken();

        try
        {
            var datos = await PrepararDatosAsync(token);
            await using var contexto = CrearContexto();
            var servicio = new ForecastEscenarioService(contexto);
            var nombre = $"Forecast QA {token}";
            var solicitud = CrearSolicitudValida(datos, nombre, variosPeriodos: true);
            var inicioEsperado = solicitud.Periodos[0].FechaInicio!.Value.Date;
            var finEsperado = solicitud.Periodos[^1].FechaFin!.Value.Date;

            var idEscenario = await servicio.CrearBorradorAsync(
                solicitud,
                datos.IdActorRecursosHumanos);

            contexto.ChangeTracker.Clear();
            var detalle = await servicio.ObtenerDetalleAsync(
                idEscenario,
                datos.IdActorRecursosHumanos);
            var escenarios = await servicio.ObtenerEscenariosAsync(
                datos.IdActorRecursosHumanos);

            Assert.NotNull(detalle);
            Assert.Equal(nombre, detalle.Nombre);
            Assert.Equal("Descripción QA", detalle.Descripcion);
            Assert.Equal(inicioEsperado, detalle.FechaInicioProyeccion);
            Assert.Equal(finEsperado, detalle.FechaFinProyeccion);
            Assert.Equal(2, detalle.PeriodosHistoricosConsiderados);
            Assert.Equal(EstadosEscenarioForecast.Borrador, detalle.EstadoEscenario);
            Assert.Equal(0m, detalle.MontoProyectadoTotal);
            Assert.Null(detalle.MontoRealTotal);
            Assert.Null(detalle.DiferenciaTotal);
            Assert.Null(detalle.FechaCalculo);
            Assert.Equal(datos.IdActorRecursosHumanos, detalle.CreadoPor);
            Assert.Equal(EstadosRegistro.Activo, detalle.EstadoRegistro);
            Assert.Equal(2, detalle.Periodos.Count);
            Assert.Equal([1, 2], detalle.Periodos.Select(periodo => periodo.NumeroOrden));
            Assert.Equal(
                [TiposPeriodoPlanilla.Quincenal, TiposPeriodoPlanilla.Mensual],
                detalle.Periodos.Select(periodo => periodo.TipoPeriodo));
            Assert.Equal(2, detalle.FuentesHistoricas.Count);
            Assert.Equal([1, 2], detalle.FuentesHistoricas.Select(fuente => fuente.NumeroOrden));
            Assert.Equal(
                [datos.IdPlanillaAprobada, datos.IdPlanillaCerrada],
                detalle.FuentesHistoricas.Select(fuente => fuente.IdPlanilla));

            var resumen = Assert.Single(escenarios, escenario =>
                escenario.IdForecastEscenario == idEscenario);
            Assert.Equal(2, resumen.CantidadPeriodos);
            Assert.Equal(2, resumen.CantidadFuentesHistoricas);
            Assert.Null(await servicio.ObtenerDetalleAsync(
                long.MaxValue,
                datos.IdActorRecursosHumanos));
            Assert.Empty(contexto.ChangeTracker.Entries());
        }
        finally
        {
            await LimpiarDatosAsync(token);
        }
    }

    [MySqlQaFact]
    public async Task Seguridad_RechazaActorVacioInexistenteInactivoOSinRol()
    {
        var token = NuevoToken();

        try
        {
            var datos = await PrepararDatosAsync(token);
            await using var contexto = CrearContexto();
            var servicio = new ForecastEscenarioService(contexto);
            var solicitud = CrearSolicitudValida(
                datos,
                $"Forecast seguridad {token}",
                variosPeriodos: false);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                servicio.ObtenerFuentesHistoricasDisponiblesAsync(" "));
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                servicio.ObtenerEscenariosAsync(datos.IdActorSinRolRecursosHumanos));
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                servicio.ObtenerDetalleAsync(1, datos.IdActorInactivo));
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                servicio.CrearBorradorAsync(
                    solicitud,
                    $"usuario-inexistente-{token}"));

            var fuentes = await servicio.ObtenerFuentesHistoricasDisponiblesAsync(
                datos.IdActorRecursosHumanos);
            Assert.Contains(fuentes, fuente => fuente.IdPlanilla == datos.IdPlanillaAprobada);
            Assert.False(await contexto.ForecastEscenarios
                .AsNoTracking()
                .AnyAsync(escenario => escenario.Nombre.Contains(token)));
        }
        finally
        {
            await LimpiarDatosAsync(token);
        }
    }

    [MySqlQaFact]
    public async Task FallosDeFuenteYClaveForanea_NoDejanGrafoParcial()
    {
        var token = NuevoToken();

        try
        {
            var datos = await PrepararDatosAsync(token);
            var nombreFuenteInvalida = $"Forecast fuente inválida {token}";

            await using (var contexto = CrearContexto())
            {
                var servicio = new ForecastEscenarioService(contexto);
                var solicitud = CrearSolicitudValida(
                    datos,
                    nombreFuenteInvalida,
                    variosPeriodos: false);
                solicitud.IdPlanillasHistoricas =
                    [datos.IdPlanillaAprobada, datos.IdPlanillaBorrador];

                await Assert.ThrowsAsync<ArgumentException>(() =>
                    servicio.CrearBorradorAsync(
                        solicitud,
                        datos.IdActorRecursosHumanos));
                Assert.Empty(contexto.ChangeTracker.Entries());
            }

            var nombreFalloFk = $"Forecast fallo FK {token}";
            await using (var contexto = CrearContexto(new InvalidarFuenteAntesDeGuardarInterceptor()))
            {
                var servicio = new ForecastEscenarioService(contexto);
                var solicitud = CrearSolicitudValida(
                    datos,
                    nombreFalloFk,
                    variosPeriodos: false);

                await Assert.ThrowsAsync<DbUpdateException>(() =>
                    servicio.CrearBorradorAsync(
                        solicitud,
                        datos.IdActorRecursosHumanos));
                Assert.Empty(contexto.ChangeTracker.Entries());
            }

            await using var verificacion = CrearContexto();
            Assert.False(await verificacion.ForecastEscenarios
                .AsNoTracking()
                .AnyAsync(escenario => escenario.Nombre == nombreFuenteInvalida
                    || escenario.Nombre == nombreFalloFk));
            Assert.False(await verificacion.ForecastPeriodos
                .AsNoTracking()
                .AnyAsync(periodo => periodo.ForecastEscenario != null
                    && periodo.ForecastEscenario.Nombre.Contains(token)));
            Assert.False(await verificacion.ForecastFuentesHistoricas
                .AsNoTracking()
                .AnyAsync(fuente => fuente.ForecastEscenario != null
                    && fuente.ForecastEscenario.Nombre.Contains(token)));
        }
        finally
        {
            await LimpiarDatosAsync(token);
        }
    }

    private static async Task<DatosQa> PrepararDatosAsync(string token)
    {
        await using var contexto = CrearContexto();
        var roles = await contexto.Roles
            .AsNoTracking()
            .Where(rol => rol.Name == "Recursos Humanos" || rol.Name == "Empleado")
            .ToDictionaryAsync(rol => rol.Name!, rol => rol.Id);

        var idRolRecursosHumanos = roles["Recursos Humanos"];
        var idRolEmpleado = roles["Empleado"];
        var actorRecursosHumanos = CrearUsuario(token, "rrhh", activo: true);
        var actorSinRolRecursosHumanos = CrearUsuario(token, "empleado", activo: true);
        var actorInactivo = CrearUsuario(token, "inactivo", activo: false);
        contexto.Users.AddRange(
            actorRecursosHumanos,
            actorSinRolRecursosHumanos,
            actorInactivo);
        contexto.UserRoles.AddRange(
            new IdentityUserRole<string>
            {
                UserId = actorRecursosHumanos.Id,
                RoleId = idRolRecursosHumanos
            },
            new IdentityUserRole<string>
            {
                UserId = actorSinRolRecursosHumanos.Id,
                RoleId = idRolEmpleado
            },
            new IdentityUserRole<string>
            {
                UserId = actorInactivo.Id,
                RoleId = idRolRecursosHumanos
            });

        var estados = await contexto.EstadosPlanilla
            .AsNoTracking()
            .Where(estado => estado.Nombre == EstadosPlanilla.Borrador
                || estado.Nombre == EstadosPlanilla.Calculada
                || estado.Nombre == EstadosPlanilla.EnRevision
                || estado.Nombre == EstadosPlanilla.Aprobada
                || estado.Nombre == EstadosPlanilla.Cerrada
                || estado.Nombre == "Anulada")
            .ToDictionaryAsync(estado => estado.Nombre, estado => estado.IdEstadoPlanilla);

        var definiciones = new[]
        {
            new DefinicionPlanilla("BOR", EstadosPlanilla.Borrador, false, false, 0),
            new DefinicionPlanilla("CAL", EstadosPlanilla.Calculada, false, false, 1),
            new DefinicionPlanilla("REV", EstadosPlanilla.EnRevision, false, false, 2),
            new DefinicionPlanilla("APR", EstadosPlanilla.Aprobada, false, false, 3),
            new DefinicionPlanilla("CER", EstadosPlanilla.Cerrada, false, false, 4),
            new DefinicionPlanilla("ANU", "Anulada", false, false, 5),
            new DefinicionPlanilla("PIN", EstadosPlanilla.Aprobada, true, false, 6),
            new DefinicionPlanilla("RIN", EstadosPlanilla.Cerrada, false, true, 7)
        };
        var fechaBase = new DateTime(2025, 1, 1);
        var periodos = new List<PeriodoPlanilla>();

        foreach (var definicion in definiciones)
        {
            var fechaInicio = fechaBase.AddMonths(definicion.DesplazamientoMeses);
            var periodo = new PeriodoPlanilla
            {
                CodigoPeriodo = $"QAF-{definicion.Codigo}-{token}",
                Nombre = $"Forecast histórico {definicion.Codigo} {token}",
                TipoPeriodo = TiposPeriodoPlanilla.Mensual,
                FechaInicio = fechaInicio,
                FechaFin = fechaInicio.AddMonths(1).AddDays(-1),
                IdEstadoPlanilla = estados[definicion.Estado],
                FechaCreacion = DateTime.Now,
                EstadoRegistro = definicion.PeriodoInactivo
                    ? EstadosRegistro.Inactivo
                    : EstadosRegistro.Activo
            };
            periodo.Planilla = new Planilla
            {
                IdEstadoPlanilla = estados[definicion.Estado],
                FechaCalculo = definicion.Estado == EstadosPlanilla.Borrador
                    ? null
                    : fechaInicio.AddMonths(1),
                FechaAprobacion = definicion.Estado is EstadosPlanilla.Aprobada or EstadosPlanilla.Cerrada
                    ? fechaInicio.AddMonths(1).AddHours(1)
                    : null,
                FechaCierre = definicion.Estado == EstadosPlanilla.Cerrada
                    ? fechaInicio.AddMonths(1).AddHours(2)
                    : null,
                FechaCreacion = DateTime.Now,
                EstadoRegistro = definicion.PlanillaInactiva
                    ? EstadosRegistro.Inactivo
                    : EstadosRegistro.Activo
            };
            periodos.Add(periodo);
        }

        contexto.PeriodosPlanilla.AddRange(periodos);
        await contexto.SaveChangesAsync();

        long ObtenerId(string codigo) => periodos
            .Single(periodo => periodo.CodigoPeriodo.Contains($"-{codigo}-"))
            .Planilla!.IdPlanilla;

        return new DatosQa(
            actorRecursosHumanos.Id,
            actorSinRolRecursosHumanos.Id,
            actorInactivo.Id,
            ObtenerId("APR"),
            ObtenerId("CER"),
            ObtenerId("BOR"),
            ObtenerId("CAL"),
            ObtenerId("REV"),
            ObtenerId("ANU"),
            ObtenerId("PIN"),
            ObtenerId("RIN"));
    }

    private static SolicitudCrearEscenarioForecast CrearSolicitudValida(
        DatosQa datos,
        string nombre,
        bool variosPeriodos)
    {
        var inicio = DateTime.Today.AddMonths(2).Date;
        var periodos = new List<SolicitudPeriodoForecast>
        {
            new()
            {
                TipoPeriodo = TiposPeriodoPlanilla.Quincenal,
                FechaInicio = inicio,
                FechaFin = inicio.AddDays(14)
            }
        };

        if (variosPeriodos)
        {
            var segundoInicio = inicio.AddMonths(1);
            periodos.Add(new SolicitudPeriodoForecast
            {
                TipoPeriodo = TiposPeriodoPlanilla.Mensual.ToLowerInvariant(),
                FechaInicio = segundoInicio,
                FechaFin = segundoInicio.AddMonths(1).AddDays(-1)
            });
        }

        return new SolicitudCrearEscenarioForecast
        {
            Nombre = $"  {nombre}  ",
            Descripcion = "  Descripción QA  ",
            Periodos = periodos,
            IdPlanillasHistoricas =
                [datos.IdPlanillaAprobada, datos.IdPlanillaCerrada]
        };
    }

    private static ApplicationUser CrearUsuario(string token, string tipo, bool activo)
    {
        var correo = $"forecast-{tipo}-{token}@qa.local";
        return new ApplicationUser
        {
            Id = $"forecast-{tipo}-{token}",
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

    private static ApplicationDbContext CrearContexto(
        SaveChangesInterceptor? interceptor = null)
    {
        var cadena = Environment.GetEnvironmentVariable(MySqlQaFactAttribute.VariableConexion);
        if (string.IsNullOrWhiteSpace(cadena)
            || !cadena.Contains("SIGE_CDC_DB_QA_MIEMBRO2", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "Las pruebas de forecast solo pueden ejecutarse contra SIGE_CDC_DB_QA_MIEMBRO2.");
        }

        var opciones = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseMySQL(cadena);
        if (interceptor is not null)
        {
            opciones.AddInterceptors(interceptor);
        }

        return new ApplicationDbContext(opciones.Options);
    }

    private static async Task LimpiarDatosAsync(string token)
    {
        await using var contexto = CrearContexto();

        var escenarios = await contexto.ForecastEscenarios
            .Where(escenario => escenario.Nombre.Contains(token))
            .ToListAsync();
        contexto.ForecastEscenarios.RemoveRange(escenarios);
        await contexto.SaveChangesAsync();

        var periodos = await contexto.PeriodosPlanilla
            .Include(periodo => periodo.Planilla)
            .Where(periodo => periodo.CodigoPeriodo.Contains(token))
            .ToListAsync();
        contexto.Planillas.RemoveRange(periodos
            .Where(periodo => periodo.Planilla is not null)
            .Select(periodo => periodo.Planilla!));
        await contexto.SaveChangesAsync();
        contexto.PeriodosPlanilla.RemoveRange(periodos);

        var rolesUsuarios = await contexto.UserRoles
            .Where(usuarioRol => usuarioRol.UserId.Contains(token))
            .ToListAsync();
        contexto.UserRoles.RemoveRange(rolesUsuarios);
        var usuarios = await contexto.Users
            .Where(usuario => usuario.Id.Contains(token))
            .ToListAsync();
        contexto.Users.RemoveRange(usuarios);
        await contexto.SaveChangesAsync();
    }

    private static string NuevoToken() => Guid.NewGuid().ToString("N")[..8];

    private sealed class InvalidarFuenteAntesDeGuardarInterceptor : SaveChangesInterceptor
    {
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            var fuente = eventData.Context?.ChangeTracker
                .Entries<ForecastFuenteHistorica>()
                .FirstOrDefault(entrada => entrada.State == EntityState.Added);
            if (fuente is not null)
            {
                fuente.Entity.IdPlanilla = long.MaxValue;
            }

            return ValueTask.FromResult(result);
        }
    }

    private sealed record DefinicionPlanilla(
        string Codigo,
        string Estado,
        bool PlanillaInactiva,
        bool PeriodoInactivo,
        int DesplazamientoMeses);

    private sealed record DatosQa(
        string IdActorRecursosHumanos,
        string IdActorSinRolRecursosHumanos,
        string IdActorInactivo,
        long IdPlanillaAprobada,
        long IdPlanillaCerrada,
        long IdPlanillaBorrador,
        long IdPlanillaCalculada,
        long IdPlanillaEnRevision,
        long IdPlanillaAnulada,
        long IdPlanillaInactiva,
        long IdPlanillaPeriodoInactivo);
}
