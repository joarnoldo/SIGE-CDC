using System.Data;
using Microsoft.EntityFrameworkCore;
using SIGECDC.Persistence.Identity;

namespace SIGECDC.Web.Configuracion;

public static class ValidacionConexionMySql
{
    private static readonly string[] TablasIdentityRequeridas =
    [
        "AspNetUsers",
        "AspNetRoles",
        "AspNetUserRoles"
    ];

    public static async Task ValidarConexionMySqlDesarrolloAsync(this WebApplication app)
    {
        if (!app.Environment.IsDevelopment())
        {
            return;
        }

        await using var scope = app.Services.CreateAsyncScope();
        var contexto = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var logger = scope.ServiceProvider
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger("ValidacionConexionMySql");
        var conexion = contexto.Database.GetDbConnection();

        try
        {
            await conexion.OpenAsync();
            await ValidarTablasIdentityAsync(conexion);
            logger.LogInformation("Conexion de desarrollo a MySQL validada correctamente.");
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                "No se pudo abrir o validar la conexion local a MySQL con ConnectionStrings:DefaultConnection. " +
                "Verifique que MySQL este iniciado, que localhost:3306 acepte el usuario configurado, que la contrasena guardada en User Secrets sea correcta, " +
                "que la base SIGE_CDC_DB exista, que existan las tablas Identity requeridas y que la cadena incluya AllowPublicKeyRetrieval=True;SslMode=Disabled. " +
                "La contrasena no se muestra por seguridad.",
                ex);
        }
        finally
        {
            if (conexion.State != ConnectionState.Closed)
            {
                await conexion.CloseAsync();
            }
        }
    }

    private static async Task ValidarTablasIdentityAsync(System.Data.Common.DbConnection conexion)
    {
        await using var comando = conexion.CreateCommand();
        comando.CommandText = """
            SELECT COUNT(*)
            FROM information_schema.tables
            WHERE table_schema = DATABASE()
              AND table_name IN ('AspNetUsers', 'AspNetRoles', 'AspNetUserRoles');
            """;

        var resultado = await comando.ExecuteScalarAsync();
        var totalTablas = Convert.ToInt32(resultado);

        if (totalTablas != TablasIdentityRequeridas.Length)
        {
            throw new InvalidOperationException(
                "La base SIGE_CDC_DB no contiene todas las tablas Identity requeridas: " +
                string.Join(", ", TablasIdentityRequeridas) + ".");
        }
    }
}
