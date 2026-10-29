using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SIGECDC.Application.Identity;
using SIGECDC.Domain.Auditoria;
using SIGECDC.Domain.RecursosHumanos;

namespace SIGECDC.Persistence.Identity;

public sealed class AdministracionCuentasService(ApplicationDbContext db,
    UserManager<ApplicationUser> usuarios, RoleManager<IdentityRole> roles)
    : IAdministracionCuentasService, IBootstrapAdministradorService
{
    public async Task<string> EjecutarAsync(BootstrapAdministradorOpciones opciones, CancellationToken ct = default)
    {
        if (!opciones.Enabled) return "DESHABILITADO";
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);
        try
        {
            await BloquearAdministracionAsync(ct);
            if (await AdministradoresValidos().AnyAsync(ct))
            {
                await tx.CommitAsync(ct);
                return "ADMINISTRADOR_EXISTENTE";
            }
            var email = ValidarEmail(opciones.Email);
            if (string.IsNullOrWhiteSpace(opciones.Password))
                throw Error("BOOTSTRAP_CONFIGURACION", "Falta la contraseña externa del Administrador inicial.");
            await ExigirEmailDisponibleAsync(email);
            var usuario = NuevaCuenta(email);
            Exigir(await usuarios.CreateAsync(usuario, opciones.Password));
            Exigir(await usuarios.AddToRoleAsync(usuario, RolesCuentas.Administrador));
            Auditar(usuario.Id, usuario.Id, "CREAR_ADMINISTRADOR_INICIAL", new { Rol = RolesCuentas.Administrador });
            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
            return "ADMINISTRADOR_CREADO";
        }
        catch
        {
            await tx.RollbackAsync(ct);
            db.ChangeTracker.Clear();
            throw;
        }
    }

    public async Task<IReadOnlyList<CuentaResumen>> ListarAsync(ClaimsPrincipal actor, string? busqueda, CancellationToken ct = default)
    {
        await AutorizarAsync(actor);
        var filtro = (busqueda ?? "").Trim();
        var consulta = db.Users.AsNoTracking()
            .Where(u => filtro == "" || (u.Email != null && u.Email.Contains(filtro)))
            .OrderBy(u => u.Email).ThenBy(u => u.Id).Take(100);
        var cuentas = await consulta.ToListAsync(ct);
        // JOIN contra el listado acotado: sin colecciones string parametrizadas ni IN con LIMIT.
        var asignaciones = await (from u in consulta
            join ur in db.UserRoles.AsNoTracking() on u.Id equals ur.UserId
            join r in db.Roles.AsNoTracking() on ur.RoleId equals r.Id
            select new { ur.UserId, r.Name }).ToListAsync(ct);
        var vinculos = await (from u in consulta
            join c in db.Colaboradores.AsNoTracking() on u.Id equals c.IdUsuario
            select new { c.IdUsuario, c.IdColaborador }).ToListAsync(ct);
        return cuentas.Select(u => new CuentaResumen(u.Id, u.Email ?? u.UserName ?? "",
            asignaciones.Where(r => r.UserId == u.Id).Select(r => r.Name!).Order().ToArray(),
            u.EstaActivo(), u.ConcurrencyStamp ?? "", vinculos.SingleOrDefault(c => c.IdUsuario == u.Id)?.IdColaborador)).ToArray();
    }

    public async Task<IReadOnlyList<ColaboradorCuenta>> ColaboradoresAsync(ClaimsPrincipal actor, CancellationToken ct = default)
    {
        await AutorizarAsync(actor);
        return await db.Colaboradores.AsNoTracking().Where(c => c.EstadoRegistro == "Activo")
            .OrderBy(c => c.CodigoColaborador).Select(c => new ColaboradorCuenta(c.IdColaborador,
                c.CodigoColaborador, c.Nombre + " " + c.PrimerApellido + " " + c.SegundoApellido, c.IdUsuario)).ToListAsync(ct);
    }

    public async Task<string> CrearAsync(ClaimsPrincipal actor, CrearCuenta solicitud, CancellationToken ct = default)
    {
        string id = "";
        await MutarAsync(actor, null, "CREAR_CUENTA", async actorId =>
        {
            await ExigirRolAsync(solicitud.Rol);
            var email = ValidarEmail(solicitud.Email);
            await ExigirEmailDisponibleAsync(email);
            var usuario = NuevaCuenta(email);
            Exigir(await usuarios.CreateAsync(usuario, solicitud.Password ?? ""));
            Exigir(await usuarios.AddToRoleAsync(usuario, solicitud.Rol));
            await VincularAsync(usuario, solicitud.Rol, solicitud.IdColaborador, actorId, ct);
            id = usuario.Id;
            Auditar(actorId, id, "CREAR_CUENTA", new { Rol = solicitud.Rol });
            Auditar(actorId, id, "ASIGNAR_ROL_CUENTA", new { Rol = solicitud.Rol });
        }, ct);
        return id;
    }

    public Task CambiarRolVinculoAsync(ClaimsPrincipal actor, string id, string version, string rol, long? colaborador, CancellationToken ct = default)
        => MutarAsync(actor, id, "CAMBIAR_ROL_VINCULO_CUENTA", async actorId =>
        {
            var usuario = await ObjetivoAsync(id, version);
            await ExigirRolAsync(rol);
            var anteriores = await usuarios.GetRolesAsync(usuario);
            if (anteriores.Any(r => !RolesCuentas.Oficiales.Contains(r)))
                throw Error("ROL_NO_OFICIAL", "La cuenta tiene una asignación de rol no reconocida.");
            if (anteriores.Contains(RolesCuentas.Administrador) && rol != RolesCuentas.Administrador)
                await ProtegerAdministradorAsync(id, ct);
            await VincularAsync(usuario, rol, colaborador, actorId, ct);
            if (anteriores.Count != 1 || anteriores[0] != rol)
            {
                Exigir(await usuarios.RemoveFromRolesAsync(usuario, anteriores));
                Exigir(await usuarios.AddToRoleAsync(usuario, rol));
                Auditar(actorId, id, "CAMBIAR_ROL_CUENTA", new { RolesAnteriores = anteriores, Rol = rol });
            }
            usuario.FechaModificacion = DateTime.UtcNow;
            Exigir(await usuarios.UpdateSecurityStampAsync(usuario));
        }, ct);

    public Task CambiarEstadoAsync(ClaimsPrincipal actor, string id, string version, bool activo, CancellationToken ct = default)
        => MutarAsync(actor, id, activo ? "REACTIVAR_CUENTA" : "BLOQUEAR_CUENTA", async actorId =>
        {
            var usuario = await ObjetivoAsync(id, version);
            if (!activo && await usuarios.IsInRoleAsync(usuario, RolesCuentas.Administrador))
                await ProtegerAdministradorAsync(id, ct);
            usuario.EstadoRegistro = activo ? "Activo" : "Inactivo";
            usuario.FechaModificacion = DateTime.UtcNow;
            // Reactivar también libera el bloqueo temporal por intentos fallidos.
            if (activo)
            {
                Exigir(await usuarios.SetLockoutEndDateAsync(usuario, null));
                Exigir(await usuarios.ResetAccessFailedCountAsync(usuario));
            }
            Exigir(await usuarios.UpdateSecurityStampAsync(usuario));
            Auditar(actorId, id, activo ? "REACTIVAR_CUENTA" : "BLOQUEAR_CUENTA", new { Activa = activo });
        }, ct);

    public Task RestablecerPasswordAsync(ClaimsPrincipal actor, string id, string version, string passwordTemporal, CancellationToken ct = default)
        => MutarAsync(actor, id, "RESTABLECER_PASSWORD_CUENTA", async actorId =>
        {
            var usuario = await ObjetivoAsync(id, version);
            var token = await usuarios.GeneratePasswordResetTokenAsync(usuario);
            Exigir(await usuarios.ResetPasswordAsync(usuario, token, passwordTemporal ?? ""));
            Exigir(await usuarios.SetLockoutEndDateAsync(usuario, null));
            Exigir(await usuarios.ResetAccessFailedCountAsync(usuario));
            Auditar(actorId, id, "RESTABLECER_PASSWORD_CUENTA", new { Recuperacion = "Administrada" });
        }, ct);

    private async Task MutarAsync(ClaimsPrincipal actor, string? id, string accion, Func<string, Task> operacion, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);
        string? actorId = null;
        try
        {
            await BloquearAdministracionAsync(ct);
            actorId = await AutorizarAsync(actor);
            await operacion(actorId);
            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync(ct);
            db.ChangeTracker.Clear();
            if (actorId is not null && ex is OperacionCuentaException validacion)
            {
                await tx.DisposeAsync();
                Auditar(actorId, id, accion, new { Codigo = validacion.Codigo }, "RECHAZADA");
                await db.SaveChangesAsync(ct);
            }
            throw;
        }
    }

    private async Task BloquearAdministracionAsync(CancellationToken ct)
    {
        // Fila estable ya existente: serializa bootstrap, roles, último Admin y duplicados.
        var nombre = roles.NormalizeKey(RolesCuentas.Administrador);
        var filas = await db.Roles.FromSqlInterpolated($"SELECT * FROM AspNetRoles WHERE NormalizedName = {nombre} FOR UPDATE")
            .AsNoTracking().ToListAsync(ct);
        if (filas.Count != 1 || !await roles.RoleExistsAsync(RolesCuentas.Administrador))
            throw Error("ROL_ADMINISTRADOR_AUSENTE", "Falta el rol oficial Administrador. Verifique la inicialización SQL.");
    }

    private async Task<string> AutorizarAsync(ClaimsPrincipal actor)
    {
        var id = actor.FindFirstValue(ClaimTypes.NameIdentifier);
        if (actor.Identity?.IsAuthenticated != true || !actor.IsInRole(RolesCuentas.Administrador) || id is null)
            throw new UnauthorizedAccessException("Solo Administrador puede gestionar cuentas.");
        var usuario = await usuarios.FindByIdAsync(id);
        if (usuario?.EstaActivo() != true || !await usuarios.IsInRoleAsync(usuario, RolesCuentas.Administrador))
            throw new UnauthorizedAccessException("La cuenta no tiene acceso administrativo vigente.");
        var stamp = actor.FindFirstValue(usuarios.Options.ClaimsIdentity.SecurityStampClaimType);
        if (stamp is not null && stamp != usuario.SecurityStamp)
            throw new UnauthorizedAccessException("La sesión cambió. Inicie sesión nuevamente.");
        return id;
    }

    private IQueryable<ApplicationUser> AdministradoresValidos() => db.Users.Where(u => u.EstadoRegistro == "Activo"
        && u.EmailConfirmed && u.PasswordHash != null && db.UserRoles.Any(ur => ur.UserId == u.Id
            && db.Roles.Any(r => r.Id == ur.RoleId && r.Name == RolesCuentas.Administrador)));

    private async Task ProtegerAdministradorAsync(string id, CancellationToken ct)
    {
        if (!await AdministradoresValidos().AnyAsync(u => u.Id != id, ct))
            throw Error("ULTIMO_ADMINISTRADOR", "Debe permanecer al menos un Administrador activo con acceso habilitado.");
    }

    private async Task<ApplicationUser> ObjetivoAsync(string id, string version)
    {
        var usuario = await usuarios.FindByIdAsync(id) ?? throw Error("CUENTA_NO_ENCONTRADA", "No se encontró la cuenta.");
        if (string.IsNullOrWhiteSpace(version) || usuario.ConcurrencyStamp != version)
            throw Error("CUENTA_MODIFICADA", "La cuenta cambió. Actualice el listado antes de continuar.");
        return usuario;
    }

    private async Task VincularAsync(ApplicationUser usuario, string rol, long? idColaborador, string actor, CancellationToken ct)
    {
        if (idColaborador is not null && rol != RolesCuentas.Empleado)
            throw Error("VINCULO_REQUIERE_EMPLEADO", "El vínculo de información propia corresponde al rol Empleado.");
        var previos = await db.Colaboradores.Where(c => c.IdUsuario == usuario.Id).ToListAsync(ct);
        Colaborador? nuevo = null;
        if (idColaborador is not null)
        {
            nuevo = (await db.Colaboradores.FromSqlInterpolated($"SELECT * FROM Colaborador WHERE IdColaborador = {idColaborador} FOR UPDATE").ToListAsync(ct)).SingleOrDefault();
            if (nuevo is null || nuevo.EstadoRegistro != "Activo")
                throw Error("COLABORADOR_NO_DISPONIBLE", "No se encontró un colaborador disponible.");
            if (nuevo.IdUsuario is not null && nuevo.IdUsuario != usuario.Id)
                throw Error("COLABORADOR_VINCULADO", "El colaborador ya está vinculado a otra cuenta.");
        }
        foreach (var previo in previos.Where(c => c.IdColaborador != idColaborador))
        {
            previo.IdUsuario = null;
            previo.ModificadoPor = actor;
            previo.FechaModificacion = DateTime.UtcNow;
            Auditar(actor, usuario.Id, "DESVINCULAR_CUENTA_COLABORADOR", new { previo.IdColaborador });
        }
        // Libera primero el índice único antes de mover el vínculo a otro expediente.
        await db.SaveChangesAsync(ct);
        if (nuevo is not null && nuevo.IdUsuario != usuario.Id)
        {
            nuevo.IdUsuario = usuario.Id;
            nuevo.ModificadoPor = actor;
            nuevo.FechaModificacion = DateTime.UtcNow;
            Auditar(actor, usuario.Id, "VINCULAR_CUENTA_COLABORADOR", new { nuevo.IdColaborador });
        }
    }

    private async Task ExigirRolAsync(string rol)
    {
        if (!RolesCuentas.Oficiales.Contains(rol) || !await roles.RoleExistsAsync(rol))
            throw Error("ROL_NO_OFICIAL", "Seleccione uno de los cuatro roles oficiales disponibles.");
    }

    private async Task ExigirEmailDisponibleAsync(string email)
    {
        if (await usuarios.FindByNameAsync(email) is not null || await usuarios.FindByEmailAsync(email) is not null)
            throw Error("CUENTA_DUPLICADA", "Ya existe una cuenta con ese correo o usuario.");
    }

    private static string ValidarEmail(string email)
    {
        var valor = (email ?? "").Trim();
        if (valor.Length > 256 || !new EmailAddressAttribute().IsValid(valor) || valor.Length == 0)
            throw Error("EMAIL_INVALIDO", "Indique un correo válido para la cuenta.");
        return valor;
    }

    private static ApplicationUser NuevaCuenta(string email) => new()
    {
        UserName = email, Email = email,
        // Alta administrativa acreditada, no se afirma haber confirmado mediante correo.
        // Identity exige este estado para el acceso; no hay autorregistro ni envío simulado.
        EmailConfirmed = true, EstadoRegistro = "Activo", LockoutEnabled = true
    };

    private static void Exigir(IdentityResult resultado)
    {
        if (resultado.Succeeded) return;
        var codigos = resultado.Errors.Select(e => e.Code).ToArray();
        var mensaje = codigos.Any(c => c.StartsWith("Password", StringComparison.Ordinal))
            ? "La contraseña no cumple la política de seguridad configurada en Identity. Use una contraseña más larga con mayúsculas, minúsculas, números y símbolos."
            : "Identity rechazó la operación. Verifique los datos o actualice el listado si la cuenta cambió.";
        throw Error("IDENTITY_RECHAZO", mensaje);
    }

    private void Auditar(string actor, string? objetivo, string accion, object datos, string resultado = "CORRECTA")
        => db.BitacoraAuditoria.Add(new BitacoraAuditoria
        {
            IdUsuario = actor, FechaHora = DateTime.UtcNow, Entidad = "AspNetUsers", IdRegistro = objetivo,
            Accion = accion, ValoresNuevos = JsonSerializer.Serialize(new { Resultado = resultado, Datos = datos }),
            Observacion = "Administración mínima de cuentas; actor y usuario conservados por identificador."
        });

    private static OperacionCuentaException Error(string codigo, string mensaje) => new(codigo, mensaje);
}
