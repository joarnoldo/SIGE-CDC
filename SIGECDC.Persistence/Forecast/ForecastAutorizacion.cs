using Microsoft.EntityFrameworkCore;
using SIGECDC.Domain.SitioPublico;
using SIGECDC.Persistence.Identity;

namespace SIGECDC.Persistence.Forecast;

internal static class ForecastAutorizacion
{
    private const string RolRecursosHumanos = "Recursos Humanos";

    public static async Task<string> ValidarRecursosHumanosAsync(
        ApplicationDbContext contexto,
        string idUsuarioActual,
        CancellationToken cancellationToken)
    {
        var idActor = string.IsNullOrWhiteSpace(idUsuarioActual)
            ? null
            : idUsuarioActual.Trim();

        if (idActor is null || idActor.Length > 255)
        {
            throw new UnauthorizedAccessException(
                "La cuenta actual no está autorizada para gestionar forecast.");
        }

        var autorizado = await contexto.Users
            .AsNoTracking()
            .AnyAsync(usuario => usuario.Id == idActor
                && usuario.EstadoRegistro == EstadosRegistro.Activo
                && contexto.UserRoles.Any(usuarioRol => usuarioRol.UserId == usuario.Id
                    && contexto.Roles.Any(rol => rol.Id == usuarioRol.RoleId
                        && rol.Name == RolRecursosHumanos)),
                cancellationToken);

        if (!autorizado)
        {
            throw new UnauthorizedAccessException(
                "La cuenta actual no está autorizada para gestionar forecast.");
        }

        return idActor;
    }
}
