using System.ComponentModel.DataAnnotations;
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
public sealed class MySqlForecastParametroIntegrationTests
{
    [MySqlQaFact]
    public async Task GuardarYConsultar_PersisteCatalogoMixtoYMetadatosDelEscenario()
    {
        var token = NuevoToken();

        try
        {
            var datos = await PrepararDatosAsync(token);
            await using var contexto = CrearContexto();
            var servicio = new ForecastParametroService(contexto);

            var inicial = await servicio.ObtenerConfiguracionAsync(
                datos.IdEscenarioBorrador,
                datos.IdActorRecursosHumanos);

            Assert.NotNull(inicial);
            Assert.True(inicial.PuedeEditar);
            Assert.Equal(4, inicial.Parametros.Count);
            Assert.All(inicial.Parametros, parametro =>
            {
                Assert.Null(parametro.IdForecastParametro);
                Assert.False(parametro.EstaHabilitado);
                Assert.Null(parametro.ValorDecimal);
            });
            Assert.Empty(contexto.ChangeTracker.Entries());

            var solicitud = SolicitudGuardarParametrosForecastTests.CrearSolicitudValida();
            await servicio.GuardarConfiguracionAsync(
                datos.IdEscenarioBorrador,
                solicitud,
                datos.IdActorRecursosHumanos);

            contexto.ChangeTracker.Clear();
            var configuracion = await servicio.ObtenerConfiguracionAsync(
                datos.IdEscenarioBorrador,
                datos.IdActorRecursosHumanos);
            var filas = await contexto.ForecastParametros
                .AsNoTracking()
                .Where(parametro => parametro.IdForecastEscenario == datos.IdEscenarioBorrador)
                .OrderBy(parametro => parametro.IdForecastParametro)
                .ToListAsync();
            var escenario = await contexto.ForecastEscenarios
                .AsNoTracking()
                .SingleAsync(item => item.IdForecastEscenario == datos.IdEscenarioBorrador);

            Assert.NotNull(configuracion);
            Assert.Equal(
                CodigosParametroForecast.Definiciones.Select(definicion => definicion.Codigo),
                configuracion.Parametros.Select(parametro => parametro.Codigo));
            Assert.All(configuracion.Parametros, parametro => Assert.True(parametro.EstaHabilitado));
            Assert.Equal(4, filas.Count);
            Assert.Equal(
                CodigosParametroForecast.Definiciones.Select(definicion => definicion.Codigo).ToHashSet(),
                filas.Select(parametro => parametro.Codigo).ToHashSet());
            Assert.All(filas, parametro =>
            {
                Assert.Null(parametro.ValorTexto);
                Assert.Null(parametro.Descripcion);
                Assert.Equal(EstadosRegistro.Activo, parametro.EstadoRegistro);
            });
            Assert.NotNull(escenario.FechaModificacion);
            Assert.Equal(datos.IdActorRecursosHumanos, escenario.ModificadoPor);
            Assert.Empty(contexto.ChangeTracker.Entries());
        }
        finally
        {
            await LimpiarDatosAsync(token);
        }
    }

    [MySqlQaFact]
    public async Task ActualizarDesactivarYReactivar_ReutilizaFilasYPreservaCodigosAjenos()
    {
        var token = NuevoToken();

        try
        {
            var datos = await PrepararDatosAsync(token);
            await using var contexto = CrearContexto();
            contexto.ForecastParametros.Add(new ForecastParametro
            {
                IdForecastEscenario = datos.IdEscenarioBorrador,
                Codigo = $"NO_ADMINISTRADO_{token}",
                Nombre = "Parámetro no administrado",
                TipoParametro = TiposParametroPlanilla.Texto,
                ValorTexto = "conservar",
                EstadoRegistro = EstadosRegistro.Activo
            });
            await contexto.SaveChangesAsync();
            contexto.ChangeTracker.Clear();

            var servicio = new ForecastParametroService(contexto);
            var solicitud = SolicitudGuardarParametrosForecastTests.CrearSolicitudValida();
            await servicio.GuardarConfiguracionAsync(
                datos.IdEscenarioBorrador,
                solicitud,
                datos.IdActorRecursosHumanos);
            contexto.ChangeTracker.Clear();

            var bonoInicial = await contexto.ForecastParametros
                .AsNoTracking()
                .SingleAsync(parametro => parametro.IdForecastEscenario == datos.IdEscenarioBorrador
                    && parametro.Codigo == CodigosParametroForecast.BonosEstimados);

            solicitud.Parametros[0].ValorDecimal = 6.1250m;
            solicitud.Parametros[1].TipoParametro = TiposParametroPlanilla.Porcentaje;
            solicitud.Parametros[1].ValorDecimal = 3.5000m;
            solicitud.Parametros[2].EstaHabilitado = false;
            solicitud.Parametros[2].ValorDecimal = 99.9999m;
            await servicio.GuardarConfiguracionAsync(
                datos.IdEscenarioBorrador,
                solicitud,
                datos.IdActorRecursosHumanos);
            contexto.ChangeTracker.Clear();

            var bonoInactivo = await contexto.ForecastParametros
                .AsNoTracking()
                .SingleAsync(parametro => parametro.IdForecastEscenario == datos.IdEscenarioBorrador
                    && parametro.Codigo == CodigosParametroForecast.BonosEstimados);
            Assert.Equal(bonoInicial.IdForecastParametro, bonoInactivo.IdForecastParametro);
            Assert.Equal(bonoInicial.TipoParametro, bonoInactivo.TipoParametro);
            Assert.Equal(bonoInicial.ValorDecimal, bonoInactivo.ValorDecimal);
            Assert.Equal(EstadosRegistro.Inactivo, bonoInactivo.EstadoRegistro);

            solicitud.Parametros[2].EstaHabilitado = true;
            solicitud.Parametros[2].TipoParametro = TiposParametroPlanilla.Monto;
            solicitud.Parametros[2].ValorDecimal = 12_345.6789m;
            await servicio.GuardarConfiguracionAsync(
                datos.IdEscenarioBorrador,
                solicitud,
                datos.IdActorRecursosHumanos);
            contexto.ChangeTracker.Clear();

            var filas = await contexto.ForecastParametros
                .AsNoTracking()
                .Where(parametro => parametro.IdForecastEscenario == datos.IdEscenarioBorrador)
                .ToListAsync();
            var bonoReactivado = Assert.Single(filas, parametro =>
                parametro.Codigo == CodigosParametroForecast.BonosEstimados);
            var noAdministrado = Assert.Single(filas, parametro =>
                parametro.Codigo == $"NO_ADMINISTRADO_{token}");

            Assert.Equal(bonoInicial.IdForecastParametro, bonoReactivado.IdForecastParametro);
            Assert.Equal(TiposParametroPlanilla.Monto, bonoReactivado.TipoParametro);
            Assert.Equal(12_345.6789m, bonoReactivado.ValorDecimal);
            Assert.Equal(EstadosRegistro.Activo, bonoReactivado.EstadoRegistro);
            Assert.Equal("conservar", noAdministrado.ValorTexto);
            Assert.Equal(5, filas.Count);
        }
        finally
        {
            await LimpiarDatosAsync(token);
        }
    }

    [MySqlQaFact]
    public async Task EstadosEIdentificadores_ControlanLecturaYEdicionSinDatosParciales()
    {
        var token = NuevoToken();

        try
        {
            var datos = await PrepararDatosAsync(token);
            await using var contexto = CrearContexto();
            var servicio = new ForecastParametroService(contexto);
            var solicitud = SolicitudGuardarParametrosForecastTests.CrearSolicitudValida();

            var calculado = await servicio.ObtenerConfiguracionAsync(
                datos.IdEscenarioCalculado,
                datos.IdActorRecursosHumanos);
            Assert.NotNull(calculado);
            Assert.False(calculado.PuedeEditar);
            Assert.Null(await servicio.ObtenerConfiguracionAsync(
                datos.IdEscenarioInactivo,
                datos.IdActorRecursosHumanos));
            Assert.Null(await servicio.ObtenerConfiguracionAsync(
                long.MaxValue,
                datos.IdActorRecursosHumanos));

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                servicio.GuardarConfiguracionAsync(
                    datos.IdEscenarioCalculado,
                    solicitud,
                    datos.IdActorRecursosHumanos));
            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                servicio.GuardarConfiguracionAsync(
                    datos.IdEscenarioInactivo,
                    solicitud,
                    datos.IdActorRecursosHumanos));
            await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
                servicio.ObtenerConfiguracionAsync(0, datos.IdActorRecursosHumanos));

            Assert.False(await contexto.ForecastParametros
                .AsNoTracking()
                .AnyAsync(parametro => parametro.IdForecastEscenario == datos.IdEscenarioCalculado
                    || parametro.IdForecastEscenario == datos.IdEscenarioInactivo));
        }
        finally
        {
            await LimpiarDatosAsync(token);
        }
    }

    [MySqlQaFact]
    public async Task SeguridadValidacionYFalloDePersistencia_NoDejanCambios()
    {
        var token = NuevoToken();

        try
        {
            var datos = await PrepararDatosAsync(token);
            var solicitud = SolicitudGuardarParametrosForecastTests.CrearSolicitudValida();

            await using (var contexto = CrearContexto())
            {
                var servicio = new ForecastParametroService(contexto);
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

                var invalida = SolicitudGuardarParametrosForecastTests.CrearSolicitudValida();
                invalida.Parametros.RemoveAt(3);
                await Assert.ThrowsAsync<ValidationException>(() =>
                    servicio.GuardarConfiguracionAsync(
                        datos.IdEscenarioBorrador,
                        invalida,
                        datos.IdActorRecursosHumanos));
                Assert.Empty(contexto.ChangeTracker.Entries());
            }

            await using (var contexto = CrearContexto(new InvalidarParametroAntesDeGuardarInterceptor()))
            {
                var servicio = new ForecastParametroService(contexto);
                await Assert.ThrowsAsync<DbUpdateException>(() =>
                    servicio.GuardarConfiguracionAsync(
                        datos.IdEscenarioBorrador,
                        solicitud,
                        datos.IdActorRecursosHumanos));
                Assert.Empty(contexto.ChangeTracker.Entries());
            }

            await using var verificacion = CrearContexto();
            Assert.False(await verificacion.ForecastParametros
                .AsNoTracking()
                .AnyAsync(parametro => parametro.IdForecastEscenario == datos.IdEscenarioBorrador));
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

    private static async Task<DatosQa> PrepararDatosAsync(string token)
    {
        await using var contexto = CrearContexto();
        var nombresRoles = new[] { "Recursos Humanos", "Administrador", "Operaciones", "Empleado" };
        var roles = (await contexto.Roles
            .AsNoTracking()
            .ToListAsync())
            .Where(rol => nombresRoles.Contains(rol.Name!, StringComparer.Ordinal))
            .ToDictionary(rol => rol.Name!, rol => rol.Id, StringComparer.Ordinal);

        var actorRecursosHumanos = CrearUsuario(token, "rrhh", activo: true);
        var actorInactivo = CrearUsuario(token, "inactivo", activo: false);
        var actorAdministrador = CrearUsuario(token, "admin", activo: true);
        var actorOperaciones = CrearUsuario(token, "operaciones", activo: true);
        var actorEmpleado = CrearUsuario(token, "empleado", activo: true);
        contexto.Users.AddRange(
            actorRecursosHumanos,
            actorInactivo,
            actorAdministrador,
            actorOperaciones,
            actorEmpleado);
        contexto.UserRoles.AddRange(
            CrearRol(actorRecursosHumanos.Id, roles["Recursos Humanos"]),
            CrearRol(actorInactivo.Id, roles["Recursos Humanos"]),
            CrearRol(actorAdministrador.Id, roles["Administrador"]),
            CrearRol(actorOperaciones.Id, roles["Operaciones"]),
            CrearRol(actorEmpleado.Id, roles["Empleado"]));

        var fechaInicio = DateTime.Today.AddMonths(2).Date;
        var escenarioBorrador = CrearEscenario(token, "Borrador", EstadosEscenarioForecast.Borrador, true, fechaInicio);
        var escenarioCalculado = CrearEscenario(token, "Calculado", EstadosEscenarioForecast.Calculado, true, fechaInicio.AddMonths(1));
        var escenarioInactivo = CrearEscenario(token, "Inactivo", EstadosEscenarioForecast.Borrador, false, fechaInicio.AddMonths(2));
        contexto.ForecastEscenarios.AddRange(
            escenarioBorrador,
            escenarioCalculado,
            escenarioInactivo);
        await contexto.SaveChangesAsync();

        return new DatosQa(
            actorRecursosHumanos.Id,
            actorInactivo.Id,
            actorAdministrador.Id,
            actorOperaciones.Id,
            actorEmpleado.Id,
            escenarioBorrador.IdForecastEscenario,
            escenarioCalculado.IdForecastEscenario,
            escenarioInactivo.IdForecastEscenario);
    }

    private static ForecastEscenario CrearEscenario(
        string token,
        string sufijo,
        string estado,
        bool activo,
        DateTime fechaInicio) => new()
    {
        Nombre = $"Forecast parámetros {sufijo} {token}",
        Descripcion = $"Escenario temporal {token}",
        FechaInicioProyeccion = fechaInicio,
        FechaFinProyeccion = fechaInicio.AddDays(14),
        PeriodosHistoricosConsiderados = 1,
        EstadoEscenario = estado,
        MontoProyectadoTotal = 0m,
        FechaCreacion = DateTime.Now,
        EstadoRegistro = activo ? EstadosRegistro.Activo : EstadosRegistro.Inactivo
    };

    private static ApplicationUser CrearUsuario(string token, string tipo, bool activo)
    {
        var correo = $"forecast-parametro-{tipo}-{token}@qa.local";
        return new ApplicationUser
        {
            Id = $"forecast-parametro-{tipo}-{token}",
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

    private static ApplicationDbContext CrearContexto(SaveChangesInterceptor? interceptor = null)
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

    private sealed class InvalidarParametroAntesDeGuardarInterceptor : SaveChangesInterceptor
    {
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            var parametro = eventData.Context?.ChangeTracker
                .Entries<ForecastParametro>()
                .FirstOrDefault(entrada => entrada.State == EntityState.Added);
            if (parametro is not null)
            {
                parametro.Property(item => item.IdForecastEscenario).CurrentValue = long.MaxValue;
            }

            return ValueTask.FromResult(result);
        }
    }

    private sealed record DatosQa(
        string IdActorRecursosHumanos,
        string IdActorInactivo,
        string IdActorAdministrador,
        string IdActorOperaciones,
        string IdActorEmpleado,
        long IdEscenarioBorrador,
        long IdEscenarioCalculado,
        long IdEscenarioInactivo);
}
