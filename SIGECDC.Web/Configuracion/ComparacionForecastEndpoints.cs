using System.Security.Claims;
using SIGECDC.Application.Forecast;

namespace SIGECDC.Web.Configuracion;

public static class ComparacionForecastEndpoints
{
    public static IEndpointRouteBuilder MapComparacionForecast(this IEndpointRouteBuilder app)
    {
        foreach(var formato in new[]{"pdf","excel"})
        {
            var extension=formato;
            app.MapGet("/rrhh/forecast/{id:long}/comparacion-real/"+extension,
                async Task<IResult>(long id,HttpContext http,IForecastComparacionSnapshotService servicio,IGeneradorComparacionForecast generador,CancellationToken ct)=>
                {
                    http.Response.Headers.CacheControl="no-store, private";http.Response.Headers.Pragma="no-cache";
                    try
                    {
                        var consulta=await servicio.LeerAsync(id,http.User.FindFirstValue(ClaimTypes.NameIdentifier)??"",ct);
                        var archivo=extension=="pdf"?generador.Pdf(consulta):generador.Excel(consulta);
                        return Results.File(archivo.Contenido,archivo.MimeType,archivo.NombreOriginal);
                    }
                    catch(UnauthorizedAccessException){return Results.NotFound();}
                    catch(InvalidOperationException){return Results.NotFound();}
                }).RequireAuthorization(p=>p.RequireRole("Administrador","Recursos Humanos"));
        }
        return app;
    }
}
