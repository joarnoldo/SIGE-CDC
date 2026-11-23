namespace SIGECDC.Web.Configuracion;

public sealed class ContencionFuncionesFinancierasMiddleware(
    RequestDelegate siguiente,
    DisponibilidadFuncionesFinancieras disponibilidad)
{
    public const string MensajePlanilla =
        "La planilla de SIGE-CDC no está habilitada en el piloto productivo ni autorizada "
        + "para determinar o ejecutar pagos reales. Su uso requiere habilitación explícita "
        + "para validación técnica o demostración académica.";

    public const string MensajeForecast =
        "El forecast de SIGE-CDC no está habilitado como herramienta financiera en el piloto productivo. "
        + "Su uso requiere habilitación explícita para validación técnica o demostración académica.";

    private static readonly PathString[] PrefijosPlanilla =
    [
        new("/rrhh/periodos-planilla"),
        new("/rrhh/incidencias-planilla"),
        new("/rrhh/parametros-planilla"),
        new("/rrhh/reportes-planilla"),
        new("/empleado/colillas")
    ];

    private static readonly PathString PrefijoForecast = new("/rrhh/forecast");

    public static bool EsRutaFinanciera(PathString ruta)
        => PrefijosPlanilla.Any(prefijo => ruta.StartsWithSegments(prefijo))
            || ruta.StartsWithSegments(PrefijoForecast);

    public async Task InvokeAsync(HttpContext contexto)
    {
        if (!disponibilidad.PlanillaDemostrativaHabilitada
            && PrefijosPlanilla.Any(prefijo => contexto.Request.Path.StartsWithSegments(prefijo)))
        {
            await RechazarAsync(contexto, MensajePlanilla);
            return;
        }

        if (!disponibilidad.ForecastExperimentalHabilitado
            && contexto.Request.Path.StartsWithSegments(PrefijoForecast))
        {
            await RechazarAsync(contexto, MensajeForecast);
            return;
        }

        await siguiente(contexto);
    }

    private static async Task RechazarAsync(HttpContext contexto, string mensaje)
    {
        contexto.Response.StatusCode = StatusCodes.Status403Forbidden;
        contexto.Response.ContentType = "text/plain; charset=utf-8";
        contexto.Response.Headers.CacheControl = "no-store";
        await contexto.Response.WriteAsync(mensaje, contexto.RequestAborted);
    }
}

public static class ContencionFuncionesFinancierasExtensions
{
    public static IApplicationBuilder UseContencionFuncionesFinancieras(
        this IApplicationBuilder aplicacion)
    {
        ArgumentNullException.ThrowIfNull(aplicacion);
        return aplicacion.UseMiddleware<ContencionFuncionesFinancierasMiddleware>();
    }
}
