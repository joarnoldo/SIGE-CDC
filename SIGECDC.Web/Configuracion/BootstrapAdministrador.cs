using SIGECDC.Application.Identity;

namespace SIGECDC.Web.Configuracion;

public static class BootstrapAdministrador
{
    public static async Task EjecutarBootstrapAdministradorAsync(this WebApplication app)
    {
        var opciones = new BootstrapAdministradorOpciones();
        app.Configuration.GetSection("BootstrapAdmin").Bind(opciones);
        if (!opciones.Enabled) return;
        await using var scope = app.Services.CreateAsyncScope();
        try
        {
            var resultado = await scope.ServiceProvider.GetRequiredService<IBootstrapAdministradorService>().EjecutarAsync(opciones);
            app.Logger.LogInformation("Bootstrap de Administrador: {Resultado}.", resultado);
        }
        catch (Exception ex)
        {
            var codigo = ex is OperacionCuentaException validacion ? validacion.Codigo : "BOOTSTRAP_ERROR_PERSISTENCIA";
            // No serializar opciones, excepciones de provider ni credenciales externas.
            app.Logger.LogCritical("Bootstrap de Administrador rechazado: {Codigo}.", codigo);
            throw new InvalidOperationException($"No se completó el bootstrap de Administrador ({codigo}). Revise la configuración externa y el esquema.");
        }
        finally { opciones.Password = ""; }
    }
}
