using System.Net;
using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SIGECDC.Web.Components;
using SIGECDC.Web.Configuracion;

namespace SIGECDC.Tests.EntregableImplantacion.Soporte;

// Host de integración local: componentes y endpoints reales; no ejecuta Program,
// seeders empresariales, bootstrap, settings ni servicios externos del producto.
public sealed class WebAcademica : IAsyncDisposable
{
    private readonly WebApplication app;
    public Uri Direccion { get; }
    private WebAcademica(WebApplication aplicacion, Uri direccion) { app = aplicacion; Direccion = direccion; }
    public static async Task<WebAcademica> IniciarAsync(EntornoAcademico entorno)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        { ApplicationName = typeof(App).Assembly.FullName, EnvironmentName = "Development", Args = [] });
        builder.Configuration.Sources.Clear();
        builder.Configuration.AddInMemoryCollection();
        builder.Logging.ClearProviders();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        entorno.Configurar(builder.Services);
        builder.Services.AddRazorComponents().AddInteractiveServerComponents();
        builder.Services.AddCascadingAuthenticationState();
        var assembly = typeof(App).Assembly;
        var redireccion = assembly.GetType("SIGECDC.Web.Components.Account.IdentityRedirectManager", true)!;
        builder.Services.AddScoped(redireccion);
        var autenticacion = assembly.GetType("SIGECDC.Web.Components.Account.IdentityRevalidatingAuthenticationStateProvider", true)!;
        builder.Services.AddScoped(typeof(AuthenticationStateProvider), autenticacion);
        var correo = assembly.GetType("SIGECDC.Web.Components.Account.IdentityNoOpEmailSender", true)!;
        builder.Services.AddSingleton(typeof(Microsoft.AspNetCore.Identity.IEmailSender<SIGECDC.Persistence.Identity.ApplicationUser>), correo);
        var aplicacion = builder.Build();
        aplicacion.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
        aplicacion.UseAuthentication(); aplicacion.UseAuthorization(); aplicacion.UseAntiforgery();
        aplicacion.MapRazorComponents<App>().AddInteractiveServerRenderMode();
        aplicacion.MapPublicacionPlanilla();
        // Se invoca el registro interno original, incluido el endpoint Logout.
        var endpoints = assembly.GetType("Microsoft.AspNetCore.Routing.IdentityComponentsEndpointRouteBuilderExtensions", true)!;
        endpoints.GetMethod("MapAdditionalIdentityEndpoints", BindingFlags.Public | BindingFlags.Static)!.Invoke(null, [aplicacion]);
        await aplicacion.StartAsync();
        var direcciones = aplicacion.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!;
        return new(aplicacion, new Uri(direcciones.Addresses.Single()));
    }

    public HttpClient Cliente() => new(new HttpClientHandler { AllowAutoRedirect = false, CookieContainer = new CookieContainer() })
    { BaseAddress = Direccion };

    public static async Task<HttpResponseMessage> IngresarAsync(HttpClient cliente, string usuario, string? clave = null)
    {
        using var pagina = await cliente.GetAsync("/Account/Login");
        pagina.EnsureSuccessStatusCode();
        var campos = Ocultos(await pagina.Content.ReadAsStringAsync());
        campos["Input.Email"] = usuario + "@example.invalid";
        campos["Input.Password"] = clave ?? EntornoAcademico.ClaveSintetica;
        campos["Input.RememberMe"] = "false";
        return await cliente.PostAsync("/Account/Login", new FormUrlEncodedContent(campos));
    }

    public static Dictionary<string, string> Ocultos(string html)
    {
        var resultado = new Dictionary<string, string>();
        foreach (Match input in Regex.Matches(html, "<input\\b[^>]*>", RegexOptions.IgnoreCase))
        {
            var atributos = Regex.Matches(input.Value, "([\\w_-]+)=\"([^\"]*)\"")
                .ToDictionary(m => m.Groups[1].Value, m => WebUtility.HtmlDecode(m.Groups[2].Value), StringComparer.OrdinalIgnoreCase);
            if (atributos.GetValueOrDefault("type") == "hidden" && atributos.TryGetValue("name", out var nombre))
                resultado[nombre] = atributos.GetValueOrDefault("value", "");
        }
        return resultado;
    }

    public async ValueTask DisposeAsync() { await app.StopAsync(); await app.DisposeAsync(); }
}
