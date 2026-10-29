using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace SIGECDC.Application.Identity;

public static class RolesCuentas
{
    public const string Administrador = "Administrador";
    public const string Empleado = "Empleado";
    public static IReadOnlyList<string> Oficiales { get; } = Array.AsReadOnly(new[]
        { Administrador, "Recursos Humanos", "Operaciones", Empleado });
}

// Clases, no records: las contraseñas nunca forman parte de un ToString generado.
public sealed class CrearCuenta
{
    [Required, EmailAddress, StringLength(256)] public string Email { get; set; } = "";
    [Required] public string Rol { get; set; } = RolesCuentas.Empleado;
    [Required, DataType(DataType.Password)] public string Password { get; set; } = "";
    public long? IdColaborador { get; set; }
}

public sealed class BootstrapAdministradorOpciones
{
    public bool Enabled { get; set; }
    public string Email { get; set; } = "";
    public string Password { get; set; } = "";
}

public sealed record CuentaResumen(string Id, string Email, IReadOnlyList<string> Roles,
    bool Activa, string Version, long? IdColaborador);
public sealed record ColaboradorCuenta(long Id, string Codigo, string Nombre, string? IdUsuario);
public sealed class OperacionCuentaException(string codigo, string mensaje) : InvalidOperationException(mensaje)
{
    public string Codigo { get; } = codigo;
}

public interface IAdministracionCuentasService
{
    Task<IReadOnlyList<CuentaResumen>> ListarAsync(ClaimsPrincipal actor, string? busqueda, CancellationToken ct = default);
    Task<IReadOnlyList<ColaboradorCuenta>> ColaboradoresAsync(ClaimsPrincipal actor, CancellationToken ct = default);
    Task<string> CrearAsync(ClaimsPrincipal actor, CrearCuenta solicitud, CancellationToken ct = default);
    Task CambiarRolVinculoAsync(ClaimsPrincipal actor, string id, string version, string rol, long? colaborador, CancellationToken ct = default);
    Task CambiarEstadoAsync(ClaimsPrincipal actor, string id, string version, bool activo, CancellationToken ct = default);
    Task RestablecerPasswordAsync(ClaimsPrincipal actor, string id, string version, string passwordTemporal, CancellationToken ct = default);
}

public interface IBootstrapAdministradorService
{
    Task<string> EjecutarAsync(BootstrapAdministradorOpciones opciones, CancellationToken ct = default);
}
