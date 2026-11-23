using System.Security.Claims;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using SIGECDC.Domain.Planillas;
using SIGECDC.Web.Configuracion;

namespace SIGECDC.Web.Components.Pages.Planillas;

public abstract class ComponenteRecorridoPlanilla : ComponentBase
{
    [Inject] protected AuthenticationStateProvider AutenticacionRecorrido { get; set; } = default!;
    [Inject] protected DisponibilidadFuncionesFinancieras DisponibilidadRecorrido { get; set; } = default!;
    [Inject] private ILoggerFactory LogFactory { get; set; } = default!;
    protected string? Mensaje { get; set; }
    protected bool EsError { get; set; }
    protected bool Ocupado { get; set; }

    protected async Task<string> ActorAsync()
    {
        if (!DisponibilidadRecorrido.PlanillaDemostrativaHabilitada) throw new UnauthorizedAccessException();
        var usuario = (await AutenticacionRecorrido.GetAuthenticationStateAsync()).User;
        if (usuario.Identity?.IsAuthenticated != true || !usuario.IsInRole("Recursos Humanos"))
            throw new UnauthorizedAccessException();
        return usuario.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new UnauthorizedAccessException();
    }

    protected async Task AccionAsync(Func<Task> accion, string? exito = null)
    {
        if (Ocupado) return;
        Ocupado = true; Mensaje = null; EsError = false;
        try
        {
            _ = await ActorAsync();
            await accion();
            Mensaje = exito;
        }
        catch (Exception ex)
        {
            EsError = true; Mensaje = MensajesRecorridoPlanilla.Para(ex);
            LogFactory.CreateLogger(GetType()).LogWarning("Operación de Planilla rechazada: {Tipo} {Codigo}",
                ex.GetType().Name, ex is ConfiguracionConceptoException c ? c.Codigo : "PRE04_OPERACION_RECHAZADA");
        }
        finally { Ocupado = false; }
    }
}
