using System.Security.Claims;
using SIGECDC.Application.Archivos;
using SIGECDC.Application.Planillas;

namespace SIGECDC.Web.Configuracion;

public static class PublicacionPlanillaEndpoints
{
    public static IEndpointRouteBuilder MapPublicacionPlanilla(this IEndpointRouteBuilder app)
    {
        app.MapGet("/empleado/colillas/{idColillaPago:long}/pdf",
            (long idColillaPago, HttpContext http, IColillaPagoService s, IGeneradorColillaPdf pdf, CancellationToken ct) =>
                Seguro(http, async () =>
                {
                    var d = await s.ObtenerDetallePropioAsync(idColillaPago, Actor(http), ct);
                    return d is null ? null : pdf.Generar(d);
                })).RequireAuthorization(p => p.RequireRole("Empleado"));

        app.MapGet("/rrhh/reportes-planilla/colillas/{idColillaPago:long}/pdf",
            (long idColillaPago, HttpContext http, IColillaPagoService s, IGeneradorColillaPdf pdf, CancellationToken ct) =>
                Seguro(http, async () =>
                {
                    var d = await s.ObtenerRevisionAsync(idColillaPago, Actor(http), ct);
                    return d is null ? null : pdf.Generar(d);
                })).RequireAuthorization(p => p.RequireRole("Administrador", "Recursos Humanos"));

        app.MapGet("/rrhh/reportes-planilla/{idPeriodoPlanilla:long}/pdf",
            (long idPeriodoPlanilla, long? ejecucion, HttpContext http, IReportePlanillaService s, IGeneradorReportePlanillaPdf pdf, CancellationToken ct) =>
                Seguro(http, async () =>
                {
                    var r = await s.ObtenerReporteAsync(idPeriodoPlanilla, ct, Actor(http));
                    return r is null || ejecucion.HasValue && r.IdEjecucionPlanilla != ejecucion ? null : pdf.Generar(r);
                })).RequireAuthorization(p => p.RequireRole("Administrador", "Recursos Humanos"));

        app.MapGet("/rrhh/reportes-planilla/{idPeriodoPlanilla:long}/excel",
            (long idPeriodoPlanilla, long? ejecucion, HttpContext http, IReportePlanillaService s, IGeneradorReportePlanillaExcel excel, CancellationToken ct) =>
                Seguro(http, async () =>
                {
                    var r = await s.ObtenerReporteAsync(idPeriodoPlanilla, ct, Actor(http));
                    return r is null || ejecucion.HasValue && r.IdEjecucionPlanilla != ejecucion ? null : excel.Generar(r);
                })).RequireAuthorization(p => p.RequireRole("Administrador", "Recursos Humanos"));
        return app;
    }

    private static string Actor(HttpContext http) => http.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";
    private static async Task<IResult> Seguro(HttpContext http, Func<Task<ArchivoDescarga?>> generar)
    {
        http.Response.Headers.CacheControl = "no-store, private";
        http.Response.Headers.Pragma = "no-cache";
        try
        {
            var archivo = await generar();
            return archivo is null ? Results.NotFound() : Results.File(archivo.Contenido, archivo.MimeType, archivo.NombreOriginal);
        }
        catch (UnauthorizedAccessException) { return Results.NotFound(); }
        catch (InvalidOperationException) { return Results.NotFound(); }
    }
}
