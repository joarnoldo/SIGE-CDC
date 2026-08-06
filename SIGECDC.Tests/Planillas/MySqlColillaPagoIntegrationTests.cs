using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SIGECDC.Application.RecursosHumanos;
using SIGECDC.Domain.Planillas;
using SIGECDC.Domain.RecursosHumanos;
using SIGECDC.Domain.SitioPublico;
using SIGECDC.Persistence.Identity;
using SIGECDC.Persistence.Planillas;
using SIGECDC.Persistence.RecursosHumanos;
using SIGECDC.Tests.Activos;

namespace SIGECDC.Tests.Planillas;

[Collection("MySQL QA Miembro 2")]
public sealed class MySqlColillaPagoIntegrationTests
{
    [MySqlQaFact]
    public async Task RecuperacionYPortal_AislanEmpleadoYValidanVinculoDeCuenta()
    {
        await using var contexto = CrearContexto();
        var marca = Guid.NewGuid().ToString("N")[..8];

        try
        {
            var datos = await PrepararPlanillaHistoricaAsync(contexto, marca);
            var servicioColaboradores = new ColaboradorService(contexto);
            var servicioColillas = new ColillaPagoService(contexto);

            var cuentasDisponibles = await servicioColaboradores
                .ObtenerCuentasEmpleadoDisponiblesAsync(datos.IdColaboradorUno);
            Assert.Contains(cuentasDisponibles, cuenta => cuenta.IdUsuario == datos.IdEmpleadoUno);
            Assert.Contains(cuentasDisponibles, cuenta => cuenta.IdUsuario == datos.IdEmpleadoDos);
            Assert.DoesNotContain(cuentasDisponibles, cuenta => cuenta.IdUsuario == datos.IdEmpleadoInactivo);
            Assert.DoesNotContain(cuentasDisponibles, cuenta => cuenta.IdUsuario == datos.IdUsuarioOperaciones);

            await servicioColaboradores.VincularCuentaEmpleadoAsync(
                datos.IdColaboradorUno,
                new SolicitudVinculoCuentaColaborador { IdUsuario = datos.IdEmpleadoUno },
                datos.IdActor);
            await servicioColaboradores.VincularCuentaEmpleadoAsync(
                datos.IdColaboradorDos,
                new SolicitudVinculoCuentaColaborador { IdUsuario = datos.IdEmpleadoDos },
                datos.IdActor);

            var portalPendiente = await servicioColillas.ObtenerPortalPropioAsync(datos.IdEmpleadoUno);
            Assert.True(portalPendiente.TieneColaboradorVinculado);
            Assert.Empty(portalPendiente.Colillas);
            Assert.Equal(1, portalPendiente.CantidadPeriodosPendientes);

            var cuentaDuplicada = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                servicioColaboradores.VincularCuentaEmpleadoAsync(
                    datos.IdColaboradorDos,
                    new SolicitudVinculoCuentaColaborador { IdUsuario = datos.IdEmpleadoUno },
                    datos.IdActor));
            Assert.Contains("otro colaborador", cuentaDuplicada.Message, StringComparison.OrdinalIgnoreCase);

            var cuentaInactiva = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                servicioColaboradores.VincularCuentaEmpleadoAsync(
                    datos.IdColaboradorUno,
                    new SolicitudVinculoCuentaColaborador { IdUsuario = datos.IdEmpleadoInactivo },
                    datos.IdActor));
            Assert.Contains("activa", cuentaInactiva.Message, StringComparison.OrdinalIgnoreCase);

            var rolInvalido = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                servicioColaboradores.VincularCuentaEmpleadoAsync(
                    datos.IdColaboradorUno,
                    new SolicitudVinculoCuentaColaborador { IdUsuario = datos.IdUsuarioOperaciones },
                    datos.IdActor));
            Assert.Contains("rol Empleado", rolInvalido.Message, StringComparison.OrdinalIgnoreCase);

            var creadas = await servicioColillas.GenerarPendientesAsync(
                datos.IdPeriodoPlanilla,
                datos.IdActor);
            var segundaEjecucion = await servicioColillas.GenerarPendientesAsync(
                datos.IdPeriodoPlanilla,
                datos.IdActor);

            Assert.Equal(2, creadas);
            Assert.Equal(0, segundaEjecucion);

            var metadatos = await contexto.ColillasPago
                .AsNoTracking()
                .OrderBy(colilla => colilla.IdDetallePlanilla)
                .ToListAsync();
            var metadatosPrueba = metadatos
                .Where(colilla => datos.IdsDetalles.Contains(colilla.IdDetallePlanilla))
                .ToList();
            Assert.Equal(2, metadatosPrueba.Count);
            Assert.All(metadatosPrueba, colilla =>
            {
                Assert.Matches("^COL-[0-9A-F]{32}$", colilla.CodigoColilla);
                Assert.Null(colilla.RutaArchivo);
                Assert.Equal(datos.IdActor, colilla.GeneradoPor);
                Assert.Equal("Activo", colilla.EstadoRegistro);
            });

            var portalUno = await servicioColillas.ObtenerPortalPropioAsync(datos.IdEmpleadoUno);
            var portalDos = await servicioColillas.ObtenerPortalPropioAsync(datos.IdEmpleadoDos);
            var portalSinVinculo = await servicioColillas.ObtenerPortalPropioAsync(datos.IdEmpleadoSinVinculo);

            Assert.Single(portalUno.Colillas);
            Assert.Single(portalDos.Colillas);
            Assert.Equal(0, portalUno.CantidadPeriodosPendientes);
            Assert.NotEqual(portalUno.Colillas[0].IdColillaPago, portalDos.Colillas[0].IdColillaPago);
            Assert.False(portalSinVinculo.TieneColaboradorVinculado);
            Assert.Empty(portalSinVinculo.Colillas);

            var detallePropio = await servicioColillas.ObtenerDetallePropioAsync(
                portalUno.Colillas[0].IdColillaPago,
                datos.IdEmpleadoUno);
            var detalleAjeno = await servicioColillas.ObtenerDetallePropioAsync(
                portalDos.Colillas[0].IdColillaPago,
                datos.IdEmpleadoUno);

            Assert.NotNull(detallePropio);
            Assert.Equal(500_000m, detallePropio.SalarioBase);
            Assert.False(string.IsNullOrWhiteSpace(detallePropio.Departamento));
            Assert.Null(detalleAjeno);

            await servicioColaboradores.VincularCuentaEmpleadoAsync(
                datos.IdColaboradorUno,
                new SolicitudVinculoCuentaColaborador { IdUsuario = null },
                datos.IdActor);
            Assert.False((await servicioColillas.ObtenerPortalPropioAsync(datos.IdEmpleadoUno))
                .TieneColaboradorVinculado);
        }
        finally
        {
            await LimpiarDatosAsync(contexto, marca);
        }
    }

    [MySqlQaFact]
    public async Task CalcularYReabrir_CreaUnaColillaPorDetalleYLaEliminaEnCascada()
    {
        await using var contexto = CrearContexto();
        var marca = Guid.NewGuid().ToString("N")[..8];

        try
        {
            var datos = await PrepararCalculoAsync(contexto, marca);
            var servicio = new PlanillaService(contexto);

            await servicio.CalcularPlanillaAsync(datos.IdPeriodoPlanilla, datos.IdActor);

            var planilla = await contexto.Planillas
                .AsNoTracking()
                .SingleAsync(registro => registro.IdPeriodoPlanilla == datos.IdPeriodoPlanilla);
            var detalles = await contexto.DetallesPlanilla
                .AsNoTracking()
                .Include(registro => registro.ColillaPago)
                .Where(registro => registro.IdPlanilla == planilla.IdPlanilla)
                .ToListAsync();

            Assert.NotEmpty(detalles);
            var detalle = Assert.Single(
                detalles,
                registro => registro.IdColaborador == datos.IdColaborador);

            Assert.NotNull(detalle.ColillaPago);
            Assert.Null(detalle.ColillaPago.RutaArchivo);
            Assert.Equal(datos.IdActor, detalle.ColillaPago.GeneradoPor);
            Assert.Matches("^COL-[0-9A-F]{32}$", detalle.ColillaPago.CodigoColilla);
            Assert.All(detalles, registro => Assert.NotNull(registro.ColillaPago));
            Assert.Equal(
                detalles.Count,
                detalles.Select(registro => registro.ColillaPago!.IdColillaPago).Distinct().Count());

            var resumen = await servicio.ObtenerDetallePeriodoAsync(datos.IdPeriodoPlanilla);
            Assert.NotNull(resumen);
            Assert.Equal(detalles.Count, resumen.CantidadColillas);
            Assert.Equal(0, resumen.CantidadColillasPendientes);

            var idsColillas = detalles
                .Select(registro => registro.ColillaPago!.IdColillaPago)
                .ToArray();
            await servicio.ReabrirPlanillaAsync(datos.IdPeriodoPlanilla, datos.IdActor);

            Assert.False(await contexto.DetallesPlanilla
                .AsNoTracking()
                .AnyAsync(registro => registro.IdPlanilla == planilla.IdPlanilla));
            Assert.False(await contexto.ColillasPago
                .AsNoTracking()
                .AnyAsync(registro => idsColillas.Contains(registro.IdColillaPago)));
        }
        finally
        {
            await LimpiarDatosAsync(contexto, marca);
        }
    }

    private static ApplicationDbContext CrearContexto()
    {
        var cadena = Environment.GetEnvironmentVariable(MySqlQaFactAttribute.VariableConexion);
        if (string.IsNullOrWhiteSpace(cadena)
            || !cadena.Contains("SIGE_CDC_DB_QA_MIEMBRO2", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "Las pruebas de integración solo pueden ejecutarse contra SIGE_CDC_DB_QA_MIEMBRO2.");
        }

        return new ApplicationDbContext(
            new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseMySQL(cadena)
                .Options);
    }

    private static async Task<DatosPlanillaHistorica> PrepararPlanillaHistoricaAsync(
        ApplicationDbContext contexto,
        string marca)
    {
        var catalogos = await ObtenerCatalogosAsync(contexto);
        var roles = await contexto.Roles
            .AsNoTracking()
            .Where(rol => rol.Name == "Empleado"
                || rol.Name == "Recursos Humanos"
                || rol.Name == "Operaciones")
            .ToDictionaryAsync(rol => rol.Name!, rol => rol.Id);

        var actor = CrearUsuario($"qa-rh007-{marca}-actor", true);
        var empleadoUno = CrearUsuario($"qa-rh007-{marca}-e1", true);
        var empleadoDos = CrearUsuario($"qa-rh007-{marca}-e2", true);
        var empleadoSinVinculo = CrearUsuario($"qa-rh007-{marca}-e3", true);
        var empleadoInactivo = CrearUsuario($"qa-rh007-{marca}-ei", false);
        var usuarioOperaciones = CrearUsuario($"qa-rh007-{marca}-op", true);
        contexto.Users.AddRange(
            actor,
            empleadoUno,
            empleadoDos,
            empleadoSinVinculo,
            empleadoInactivo,
            usuarioOperaciones);
        await contexto.SaveChangesAsync();

        contexto.UserRoles.AddRange(
            new IdentityUserRole<string> { UserId = actor.Id, RoleId = roles["Recursos Humanos"] },
            new IdentityUserRole<string> { UserId = empleadoUno.Id, RoleId = roles["Empleado"] },
            new IdentityUserRole<string> { UserId = empleadoDos.Id, RoleId = roles["Empleado"] },
            new IdentityUserRole<string> { UserId = empleadoSinVinculo.Id, RoleId = roles["Empleado"] },
            new IdentityUserRole<string> { UserId = empleadoInactivo.Id, RoleId = roles["Empleado"] },
            new IdentityUserRole<string> { UserId = usuarioOperaciones.Id, RoleId = roles["Operaciones"] });

        var colaboradorUno = CrearColaborador(marca, "A", catalogos, actor.Id);
        var colaboradorDos = CrearColaborador(marca, "B", catalogos, actor.Id);
        contexto.Colaboradores.AddRange(colaboradorUno, colaboradorDos);

        var periodo = new PeriodoPlanilla
        {
            CodigoPeriodo = $"QA7-H-{marca}",
            Nombre = $"Histórico QA {marca}",
            TipoPeriodo = "Quincenal",
            FechaInicio = new DateTime(2026, 6, 1),
            FechaFin = new DateTime(2026, 6, 15),
            IdEstadoPlanilla = catalogos.IdEstadoCerrada,
            FechaCreacion = DateTime.Now,
            CreadoPor = actor.Id,
            EstadoRegistro = "Activo"
        };
        contexto.PeriodosPlanilla.Add(periodo);
        await contexto.SaveChangesAsync();

        var planilla = new Planilla
        {
            IdPeriodoPlanilla = periodo.IdPeriodoPlanilla,
            IdEstadoPlanilla = catalogos.IdEstadoCerrada,
            FechaCalculo = new DateTime(2026, 6, 16, 8, 0, 0),
            FechaAprobacion = new DateTime(2026, 6, 16, 9, 0, 0),
            FechaCierre = new DateTime(2026, 6, 16, 10, 0, 0),
            SalarioBrutoTotal = 1_125_000m,
            DeduccionesTotal = 95_000m,
            SalarioNetoTotal = 1_030_000m,
            FechaCreacion = DateTime.Now,
            CreadoPor = actor.Id,
            EstadoRegistro = "Activo"
        };
        contexto.Planillas.Add(planilla);
        await contexto.SaveChangesAsync();

        var detalleUno = CrearDetalle(planilla.IdPlanilla, colaboradorUno.IdColaborador, 500_000m, 515_000m);
        var detalleDos = CrearDetalle(planilla.IdPlanilla, colaboradorDos.IdColaborador, 600_000m, 515_000m);
        contexto.DetallesPlanilla.AddRange(detalleUno, detalleDos);
        await contexto.SaveChangesAsync();

        return new DatosPlanillaHistorica(
            actor.Id,
            empleadoUno.Id,
            empleadoDos.Id,
            empleadoSinVinculo.Id,
            empleadoInactivo.Id,
            usuarioOperaciones.Id,
            colaboradorUno.IdColaborador,
            colaboradorDos.IdColaborador,
            periodo.IdPeriodoPlanilla,
            [detalleUno.IdDetallePlanilla, detalleDos.IdDetallePlanilla]);
    }

    private static async Task<DatosCalculo> PrepararCalculoAsync(
        ApplicationDbContext contexto,
        string marca)
    {
        var catalogos = await ObtenerCatalogosAsync(contexto);
        var actor = CrearUsuario($"qa-rh007-{marca}-calc", true);
        contexto.Users.Add(actor);
        var colaborador = CrearColaborador(marca, "C", catalogos, actor.Id);
        contexto.Colaboradores.Add(colaborador);
        await contexto.SaveChangesAsync();

        contexto.Contratos.Add(new Contrato
        {
            IdColaborador = colaborador.IdColaborador,
            TipoContrato = "Indefinido",
            FechaInicio = new DateTime(2026, 1, 1),
            SalarioBase = 800_000m,
            Jornada = "Tiempo completo",
            PeriodicidadPago = "Quincenal",
            EstadoContrato = "Activo",
            FechaCreacion = DateTime.Now,
            CreadoPor = actor.Id,
            EstadoRegistro = "Activo"
        });

        var periodo = new PeriodoPlanilla
        {
            CodigoPeriodo = $"QA7-C-{marca}",
            Nombre = $"Cálculo QA {marca}",
            TipoPeriodo = "Quincenal",
            FechaInicio = new DateTime(2026, 8, 1),
            FechaFin = new DateTime(2026, 8, 15),
            IdEstadoPlanilla = catalogos.IdEstadoBorrador,
            FechaCreacion = DateTime.Now,
            CreadoPor = actor.Id,
            EstadoRegistro = "Activo"
        };
        contexto.PeriodosPlanilla.Add(periodo);
        await contexto.SaveChangesAsync();

        return new DatosCalculo(actor.Id, periodo.IdPeriodoPlanilla, colaborador.IdColaborador);
    }

    private static async Task<CatalogosQa> ObtenerCatalogosAsync(ApplicationDbContext contexto)
    {
        var estadoLaboral = await contexto.EstadosLaborales
            .AsNoTracking()
            .SingleAsync(estado => estado.Nombre == EstadosLaborales.Activo
                && estado.EstadoRegistro == "Activo");
        var puesto = await contexto.Puestos
            .AsNoTracking()
            .Where(registro => registro.EstadoRegistro == "Activo"
                && registro.Departamento != null
                && registro.Departamento.EstadoRegistro == "Activo")
            .OrderBy(registro => registro.IdPuesto)
            .Select(registro => new { registro.IdPuesto, IdDepartamento = registro.IdDepartamento!.Value })
            .FirstAsync();
        var estadosPlanilla = await contexto.EstadosPlanilla
            .AsNoTracking()
            .Where(estado => estado.Nombre == EstadosPlanilla.Borrador
                || estado.Nombre == EstadosPlanilla.Cerrada)
            .ToDictionaryAsync(estado => estado.Nombre, estado => estado.IdEstadoPlanilla);

        return new CatalogosQa(
            estadoLaboral.IdEstadoLaboral,
            puesto.IdDepartamento,
            puesto.IdPuesto,
            estadosPlanilla[EstadosPlanilla.Borrador],
            estadosPlanilla[EstadosPlanilla.Cerrada]);
    }

    private static ApplicationUser CrearUsuario(string id, bool activo)
    {
        var correo = $"{id}@sige.local";
        return new ApplicationUser
        {
            Id = id,
            UserName = correo,
            NormalizedUserName = correo.ToUpperInvariant(),
            Email = correo,
            NormalizedEmail = correo.ToUpperInvariant(),
            EmailConfirmed = true,
            SecurityStamp = Guid.NewGuid().ToString(),
            ConcurrencyStamp = Guid.NewGuid().ToString(),
            EstadoRegistro = activo ? EstadosRegistro.Activo : "Inactivo",
            FechaCreacion = DateTime.Now
        };
    }

    private static Colaborador CrearColaborador(
        string marca,
        string letra,
        CatalogosQa catalogos,
        string idActor)
    {
        return new Colaborador
        {
            CodigoColaborador = $"QA7{letra}-{marca}",
            TipoIdentificacion = "Cédula",
            Identificacion = $"QA7-{letra}-{marca}",
            Nombre = $"Empleado {letra}",
            PrimerApellido = "Prueba",
            SegundoApellido = "Ñúñez",
            FechaIngreso = new DateTime(2026, 1, 1),
            IdEstadoLaboral = catalogos.IdEstadoLaboral,
            IdDepartamento = catalogos.IdDepartamento,
            IdPuesto = catalogos.IdPuesto,
            FechaCreacion = DateTime.Now,
            CreadoPor = idActor,
            EstadoRegistro = "Activo"
        };
    }

    private static DetallePlanilla CrearDetalle(
        long idPlanilla,
        long idColaborador,
        decimal salarioBase,
        decimal salarioNeto)
    {
        return new DetallePlanilla
        {
            IdPlanilla = idPlanilla,
            IdColaborador = idColaborador,
            SalarioBase = salarioBase,
            SalarioProporcional = salarioBase / 2,
            TotalHorasExtra = 15_000m,
            TotalBonos = 10_000m,
            TotalBeneficiosConfigurables = 5_000m,
            TotalAusencias = 2_500m,
            SalarioBruto = salarioNeto + 47_500m,
            TotalDeducciones = 47_500m,
            SalarioNeto = salarioNeto,
            CostoPatronalEstimado = 0m,
            FechaCreacion = DateTime.Now
        };
    }

    private static async Task LimpiarDatosAsync(ApplicationDbContext contexto, string marca)
    {
        contexto.ChangeTracker.Clear();

        var auditoria = await contexto.BitacoraAuditoria
            .Where(registro => registro.IdUsuario != null
                && registro.IdUsuario.StartsWith("qa-rh007-"))
            .ToListAsync();
        contexto.BitacoraAuditoria.RemoveRange(auditoria);
        await contexto.SaveChangesAsync();

        var planillas = await contexto.Planillas
            .Where(planilla => planilla.PeriodoPlanilla != null
                && planilla.PeriodoPlanilla.CodigoPeriodo.Contains(marca))
            .ToListAsync();
        contexto.Planillas.RemoveRange(planillas);
        await contexto.SaveChangesAsync();

        var periodos = await contexto.PeriodosPlanilla
            .Where(periodo => periodo.CodigoPeriodo.Contains(marca))
            .ToListAsync();
        contexto.PeriodosPlanilla.RemoveRange(periodos);

        var colaboradores = await contexto.Colaboradores
            .Where(colaborador => colaborador.CodigoColaborador.Contains(marca))
            .ToListAsync();
        var idsColaboradores = colaboradores.Select(colaborador => colaborador.IdColaborador).ToList();
        var contratos = await contexto.Contratos
            .Where(contrato => idsColaboradores.Contains(contrato.IdColaborador))
            .ToListAsync();
        contexto.Contratos.RemoveRange(contratos);
        contexto.Colaboradores.RemoveRange(colaboradores);
        await contexto.SaveChangesAsync();

        var usuarios = await contexto.Users
            .Where(usuario => usuario.Id.StartsWith("qa-rh007-"))
            .ToListAsync();
        var usuariosRoles = await contexto.UserRoles
            .Where(usuarioRol => usuarioRol.UserId.StartsWith("qa-rh007-"))
            .ToListAsync();
        contexto.UserRoles.RemoveRange(usuariosRoles);
        contexto.Users.RemoveRange(usuarios);
        await contexto.SaveChangesAsync();
    }

    private sealed record CatalogosQa(
        int IdEstadoLaboral,
        int IdDepartamento,
        int IdPuesto,
        int IdEstadoBorrador,
        int IdEstadoCerrada);

    private sealed record DatosCalculo(
        string IdActor,
        long IdPeriodoPlanilla,
        long IdColaborador);

    private sealed record DatosPlanillaHistorica(
        string IdActor,
        string IdEmpleadoUno,
        string IdEmpleadoDos,
        string IdEmpleadoSinVinculo,
        string IdEmpleadoInactivo,
        string IdUsuarioOperaciones,
        long IdColaboradorUno,
        long IdColaboradorDos,
        long IdPeriodoPlanilla,
        IReadOnlyList<long> IdsDetalles);
}
