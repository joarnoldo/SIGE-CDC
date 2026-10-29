namespace SIGECDC.Web.Components.Account;

public sealed class ContencionEliminacionCuentaMiddleware(RequestDelegate siguiente)
{
    public const string RutaEliminacion = "/Account/Manage/DeletePersonalData";

    public const string Mensaje =
        "La eliminación de cuentas no está disponible. "
        + "La administración de la cuenta debe gestionarse mediante el proceso administrativo autorizado.";

    public async Task InvokeAsync(HttpContext contexto)
    {
        if (EsRutaEliminacion(contexto.Request.Path)
            && !HttpMethods.IsGet(contexto.Request.Method)
            && !HttpMethods.IsHead(contexto.Request.Method))
        {
            contexto.Response.StatusCode = StatusCodes.Status403Forbidden;
            contexto.Response.ContentType = "text/plain; charset=utf-8";
            contexto.Response.Headers.CacheControl = "no-store";
            await contexto.Response.WriteAsync(Mensaje, contexto.RequestAborted);
            return;
        }

        await siguiente(contexto);
    }

    private static bool EsRutaEliminacion(PathString ruta)
    {
        var valor = ruta.Value?.TrimEnd('/');
        return string.Equals(valor, RutaEliminacion, StringComparison.OrdinalIgnoreCase);
    }
}

public static class ContencionEliminacionCuentaExtensions
{
    public static IApplicationBuilder UseContencionEliminacionCuenta(
        this IApplicationBuilder aplicacion)
    {
        ArgumentNullException.ThrowIfNull(aplicacion);
        return aplicacion.UseMiddleware<ContencionEliminacionCuentaMiddleware>();
    }
}
