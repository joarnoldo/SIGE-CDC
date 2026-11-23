#pragma warning disable BL0006 // Inspección y despacho del árbol Blazor exclusivamente desde el arnés QA.
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace SIGECDC.Tests.EntregableImplantacion.Soporte;

public sealed class ComponenteAcademico(IServiceProvider servicios) : Renderer(servicios, servicios.GetRequiredService<ILoggerFactory>())
{
    private int raiz;
    public override Dispatcher Dispatcher { get; } = Dispatcher.CreateDefault();
    protected override Task UpdateDisplayAsync(in RenderBatch renderBatch) => Task.CompletedTask;
    protected override void HandleException(Exception exception) => System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(exception).Throw();
    protected override IComponent ResolveComponentForRenderMode(Type componentType, int? parentComponentId,
        IComponentActivator componentActivator, IComponentRenderMode renderMode) => componentActivator.CreateInstance(componentType);

    public Task MostrarAsync<T>() where T : IComponent => Dispatcher.InvokeAsync(async () =>
    {
        raiz = AssignRootComponentId(InstantiateComponent(typeof(T)));
        await RenderRootComponentAsync(raiz, ParameterView.Empty);
    });

    private IEnumerable<RenderTreeFrame> Marcos(int id)
    {
        var rango = GetCurrentRenderTreeFrames(id);
        var copia = rango.Array.Take(rango.Count).ToArray();
        foreach (var marco in copia)
        {
            yield return marco;
            if (marco.FrameType == RenderTreeFrameType.Component)
                foreach (var hijo in Marcos(marco.ComponentId)) yield return hijo;
        }
    }
    public Task<string> TextoAsync() => Dispatcher.InvokeAsync(() => string.Join(" ", Marcos(raiz)
        .Where(f => f.FrameType is RenderTreeFrameType.Text or RenderTreeFrameType.Markup)
        .Select(f => f.FrameType == RenderTreeFrameType.Text ? f.TextContent : f.MarkupContent)));

    public Task EventoAsync(string elemento, string evento, EventArgs argumentos, string? id = null, int indice = 0) => Dispatcher.InvokeAsync(async () =>
    {
        var marcos = Marcos(raiz).ToArray(); var eventos = new List<ulong>();
        for (var i = 0; i < marcos.Length; i++)
        {
            if (marcos[i].FrameType != RenderTreeFrameType.Element || marcos[i].ElementName != elemento) continue;
            var atributos = marcos.Skip(i + 1).TakeWhile(f => f.FrameType == RenderTreeFrameType.Attribute).ToArray();
            if (id is not null && !atributos.Any(f => f.AttributeName == "id" && Equals(f.AttributeValue, id))) continue;
            eventos.AddRange(atributos.Where(f => f.AttributeName == evento && f.AttributeEventHandlerId != 0).Select(f => f.AttributeEventHandlerId));
        }
        if (eventos.Count <= indice) throw new InvalidOperationException($"No se encontró evento {elemento}/{evento}/{id}.");
        await DispatchEventAsync(eventos[indice], null, argumentos);
    });
}
