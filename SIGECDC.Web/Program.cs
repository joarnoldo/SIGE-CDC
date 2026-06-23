using SIGECDC.Application.Archivos;
using SIGECDC.Application.RecursosHumanos;
using SIGECDC.Infrastructure.Archivos;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using SIGECDC.Persistence;
using SIGECDC.Persistence.Identity;
using SIGECDC.Web.Configuracion;
using SIGECDC.Web.Components;
using SIGECDC.Web.Components.Account;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<IdentityRedirectManager>();
builder.Services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();

builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = IdentityConstants.ApplicationScheme;
        options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
    })
    .AddIdentityCookies();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddSingleton<IAlmacenamientoArchivosService>(_ =>
    new AlmacenamientoArchivosLocal(builder.Configuration["AlmacenamientoArchivos:RutaBase"] ?? string.Empty));
builder.Services.AgregarPersistencia(connectionString);

builder.Services.AddIdentityCore<ApplicationUser>(options =>
    {
        options.SignIn.RequireConfirmedAccount = true;
        options.Stores.SchemaVersion = IdentitySchemaVersions.Version2;
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();

builder.Services.AddSingleton<IEmailSender<ApplicationUser>, IdentityNoOpEmailSender>();

var app = builder.Build();

await app.ValidarConexionMySqlDesarrolloAsync();
await app.SembrarUsuariosDesarrolloAsync();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapGet(
        "/rrhh/documentos/{idDocumentoArchivo:long}/descargar",
        async (long idDocumentoArchivo, IContratoDocumentoService contratoDocumentoService, CancellationToken cancellationToken) =>
        {
            var archivo = await contratoDocumentoService.AbrirDocumentoAsync(idDocumentoArchivo, cancellationToken);

            return archivo is null
                ? Results.NotFound()
                : Results.File(archivo.Contenido, archivo.MimeType, archivo.NombreOriginal);
        })
    .RequireAuthorization(policy => policy.RequireRole("Administrador", "Recursos Humanos"));

// Add additional endpoints required by the Identity /Account Razor components.
app.MapAdditionalIdentityEndpoints();

app.Run();
