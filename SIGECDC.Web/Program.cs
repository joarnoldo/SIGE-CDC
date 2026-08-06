using SIGECDC.Application.Archivos;
using SIGECDC.Application.Activos;
using SIGECDC.Application.Forecast;
using SIGECDC.Application.Planillas;
using SIGECDC.Application.RecursosHumanos;
using SIGECDC.Application.SitioPublico;
using SIGECDC.Infrastructure.Archivos;
using SIGECDC.Infrastructure.Reportes;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
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
builder.Services.Configure<SecurityStampValidatorOptions>(options =>
    options.ValidationInterval = TimeSpan.FromMinutes(4));

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
builder.Services.AddSingleton<IGeneradorColillaPdf, GeneradorColillaPdf>();
builder.Services.AddSingleton<IGeneradorReportePlanillaPdf, GeneradorReportePlanillaPdf>();
builder.Services.AddSingleton<IGeneradorReportePlanillaExcel, GeneradorReportePlanillaExcel>();
builder.Services.AddSingleton<IGeneradorForecastPdf, GeneradorForecastPdf>();
builder.Services.AddSingleton<IGeneradorForecastExcel, GeneradorForecastExcel>();
builder.Services.AgregarPersistencia(connectionString);

builder.Services.AddIdentityCore<ApplicationUser>(options =>
    {
        options.SignIn.RequireConfirmedAccount = true;
        options.Lockout.AllowedForNewUsers = true;
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
        options.Stores.SchemaVersion = IdentitySchemaVersions.Version2;
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddSignInManager<UsuarioActivoSignInManager>()
    .AddDefaultTokenProviders();

builder.Services.AddSingleton<IEmailSender<ApplicationUser>, IdentityNoOpEmailSender>();

var app = builder.Build();

await app.ValidarConexionMySqlDesarrolloAsync();
await app.SembrarUsuariosDesarrolloAsync();

if (DatosDemoSprint3Seeder.FueSolicitada(args))
{
    await app.SembrarDatosDemoSprint3Async();
    return;
}

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

app.MapGet(
        "/empleado/colillas/{idColillaPago:long}/pdf",
        async (
            long idColillaPago,
            ClaimsPrincipal usuario,
            IColillaPagoService colillaPagoService,
            IGeneradorColillaPdf generadorPdf,
            CancellationToken cancellationToken) =>
        {
            var idUsuario = usuario.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(idUsuario))
            {
                return Results.NotFound();
            }

            var colilla = await colillaPagoService.ObtenerDetallePropioAsync(
                idColillaPago,
                idUsuario,
                cancellationToken);

            if (colilla is null)
            {
                return Results.NotFound();
            }

            var archivo = generadorPdf.Generar(colilla);
            return Results.File(archivo.Contenido, archivo.MimeType, archivo.NombreOriginal);
        })
    .RequireAuthorization(policy => policy.RequireRole("Empleado"));

app.MapGet(
        "/rrhh/reportes-planilla/{idPeriodoPlanilla:long}/pdf",
        async (
            long idPeriodoPlanilla,
            IReportePlanillaService reporteService,
            IGeneradorReportePlanillaPdf generadorPdf,
            CancellationToken cancellationToken) =>
        {
            var reporte = await reporteService.ObtenerReporteAsync(
                idPeriodoPlanilla,
                cancellationToken);

            if (reporte is null || reporte.Detalles.Count == 0)
            {
                return Results.NotFound();
            }

            var archivo = generadorPdf.Generar(reporte);
            return Results.File(archivo.Contenido, archivo.MimeType, archivo.NombreOriginal);
        })
    .RequireAuthorization(policy => policy.RequireRole("Recursos Humanos"));

app.MapGet(
        "/rrhh/reportes-planilla/{idPeriodoPlanilla:long}/excel",
        async (
            long idPeriodoPlanilla,
            IReportePlanillaService reporteService,
            IGeneradorReportePlanillaExcel generadorExcel,
            CancellationToken cancellationToken) =>
        {
            var reporte = await reporteService.ObtenerReporteAsync(
                idPeriodoPlanilla,
                cancellationToken);

            if (reporte is null || reporte.Detalles.Count == 0)
            {
                return Results.NotFound();
            }

            var archivo = generadorExcel.Generar(reporte);
            return Results.File(archivo.Contenido, archivo.MimeType, archivo.NombreOriginal);
        })
    .RequireAuthorization(policy => policy.RequireRole("Recursos Humanos"));

app.MapGet(
        "/rrhh/forecast/{idForecastEscenario:long}/resultados/pdf",
        async Task<IResult> (
            long idForecastEscenario,
            string dimension,
            long? idForecastPeriodo,
            ClaimsPrincipal usuario,
            IForecastResultadoService resultadoService,
            IGeneradorForecastPdf generadorPdf,
            CancellationToken cancellationToken) =>
        {
            var idUsuario = usuario.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(idUsuario))
            {
                return Results.NotFound();
            }

            try
            {
                var consulta = await resultadoService.ObtenerResultadosAsync(
                    idForecastEscenario,
                    new FiltroResultadosForecast
                    {
                        Dimension = dimension,
                        IdForecastPeriodo = idForecastPeriodo
                    },
                    idUsuario,
                    cancellationToken);

                if (consulta is null || consulta.Filas.Count == 0)
                {
                    return Results.NotFound();
                }

                var archivo = generadorPdf.Generar(consulta);
                return Results.File(archivo.Contenido, archivo.MimeType, archivo.NombreOriginal);
            }
            catch (UnauthorizedAccessException)
            {
                return Results.Forbid();
            }
            catch (Exception ex) when (ex is ValidationException or ArgumentException)
            {
                return Results.NotFound();
            }
        })
    .RequireAuthorization(policy => policy.RequireRole("Recursos Humanos"));

app.MapGet(
        "/rrhh/forecast/{idForecastEscenario:long}/resultados/excel",
        async Task<IResult> (
            long idForecastEscenario,
            string dimension,
            long? idForecastPeriodo,
            ClaimsPrincipal usuario,
            IForecastResultadoService resultadoService,
            IGeneradorForecastExcel generadorExcel,
            CancellationToken cancellationToken) =>
        {
            var idUsuario = usuario.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(idUsuario))
            {
                return Results.NotFound();
            }

            try
            {
                var consulta = await resultadoService.ObtenerResultadosAsync(
                    idForecastEscenario,
                    new FiltroResultadosForecast
                    {
                        Dimension = dimension,
                        IdForecastPeriodo = idForecastPeriodo
                    },
                    idUsuario,
                    cancellationToken);

                if (consulta is null || consulta.Filas.Count == 0)
                {
                    return Results.NotFound();
                }

                var archivo = generadorExcel.Generar(consulta);
                return Results.File(archivo.Contenido, archivo.MimeType, archivo.NombreOriginal);
            }
            catch (UnauthorizedAccessException)
            {
                return Results.Forbid();
            }
            catch (Exception ex) when (ex is ValidationException or ArgumentException)
            {
                return Results.NotFound();
            }
        })
    .RequireAuthorization(policy => policy.RequireRole("Recursos Humanos"));

app.MapGet(
        "/operaciones/mantenimientos/evidencias/{idDocumentoArchivo:long}/descargar",
        async (
            long idDocumentoArchivo,
            IMantenimientoService mantenimientoService,
            CancellationToken cancellationToken) =>
        {
            var archivo = await mantenimientoService.AbrirEvidenciaAsync(
                idDocumentoArchivo,
                cancellationToken);

            return archivo is null
                ? Results.NotFound()
                : Results.File(archivo.Contenido, archivo.MimeType, archivo.NombreOriginal);
        })
    .RequireAuthorization(policy => policy.RequireRole("Administrador", "Operaciones"));

app.MapGet(
    "/galeria/imagenes/{idImagenGaleria:long}",
    async (long idImagenGaleria, IGaleriaService galeriaService, CancellationToken cancellationToken) =>
    {
        var archivo = await galeriaService.AbrirImagenPublicaAsync(idImagenGaleria, cancellationToken);

        return archivo is null
            ? Results.NotFound()
            : Results.File(archivo.Contenido, archivo.MimeType ?? "application/octet-stream");
});

app.MapGet(
        "/admin/galeria/imagenes/{idImagenGaleria:long}",
        async (long idImagenGaleria, IGaleriaService galeriaService, CancellationToken cancellationToken) =>
        {
            var archivo = await galeriaService.AbrirImagenAdministrativaAsync(idImagenGaleria, cancellationToken);

            return archivo is null
                ? Results.NotFound()
                : Results.File(archivo.Contenido, archivo.MimeType ?? "application/octet-stream");
        })
    .RequireAuthorization(policy => policy.RequireRole("Administrador"));

// Add additional endpoints required by the Identity /Account Razor components.
app.MapAdditionalIdentityEndpoints();

app.Run();
