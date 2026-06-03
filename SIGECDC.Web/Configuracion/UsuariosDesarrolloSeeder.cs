using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SIGECDC.Persistence.Identity;

namespace SIGECDC.Web.Configuracion;

public static class UsuariosDesarrolloSeeder
{
    private static readonly UsuarioDesarrollo[] Usuarios =
    [
        new("admin@sige-cdc.local", "Administrador"),
        new("rrhh@sige-cdc.local", "Recursos Humanos"),
        new("operaciones@sige-cdc.local", "Operaciones"),
        new("empleado@sige-cdc.local", "Empleado")
    ];

    public static async Task SembrarUsuariosDesarrolloAsync(this WebApplication app)
    {
        if (!app.Environment.IsDevelopment())
        {
            return;
        }

        await using var scope = app.Services.CreateAsyncScope();
        var servicios = scope.ServiceProvider;
        var logger = servicios
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger("UsuariosDesarrolloSeeder");

        var password = app.Configuration["UsuariosDesarrollo:Password"];
        if (string.IsNullOrWhiteSpace(password))
        {
            logger.LogWarning("No se crearon usuarios de desarrollo porque falta configurar el secret UsuariosDesarrollo:Password.");
            return;
        }

        try
        {
            var userManager = servicios.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = servicios.GetRequiredService<RoleManager<IdentityRole>>();

            if (await userManager.Users.AnyAsync())
            {
                logger.LogInformation("No se crearon usuarios de desarrollo porque ya existen usuarios registrados.");
                return;
            }

            foreach (var usuario in Usuarios)
            {
                if (!await roleManager.RoleExistsAsync(usuario.Rol))
                {
                    logger.LogWarning("No se crearon usuarios de desarrollo porque no existe el rol oficial {Rol}.", usuario.Rol);
                    return;
                }
            }

            foreach (var usuario in Usuarios)
            {
                var applicationUser = new ApplicationUser
                {
                    UserName = usuario.CorreoElectronico,
                    Email = usuario.CorreoElectronico,
                    EmailConfirmed = true
                };

                var resultadoCreacion = await userManager.CreateAsync(applicationUser, password);
                if (!resultadoCreacion.Succeeded)
                {
                    logger.LogWarning(
                        "No se pudo crear el usuario de desarrollo {Correo}. Errores: {Errores}",
                        usuario.CorreoElectronico,
                        string.Join("; ", resultadoCreacion.Errors.Select(error => error.Description)));
                    continue;
                }

                var resultadoRol = await userManager.AddToRoleAsync(applicationUser, usuario.Rol);
                if (!resultadoRol.Succeeded)
                {
                    logger.LogWarning(
                        "No se pudo asignar el rol {Rol} al usuario de desarrollo {Correo}. Errores: {Errores}",
                        usuario.Rol,
                        usuario.CorreoElectronico,
                        string.Join("; ", resultadoRol.Errors.Select(error => error.Description)));
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "No se pudieron sembrar los usuarios de desarrollo.");
        }
    }

    private sealed record UsuarioDesarrollo(string CorreoElectronico, string Rol);
}
