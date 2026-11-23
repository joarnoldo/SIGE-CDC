namespace SIGECDC.Web.Components.Account;

// Protege también rutas directas y POST scaffolded, no solo el menú.
public sealed class ContencionIdentityNoSoportadaMiddleware(RequestDelegate siguiente)
{
    private static readonly HashSet<string> Rutas = new(StringComparer.OrdinalIgnoreCase)
    {
        "/Account/Register", "/Account/RegisterConfirmation", "/Account/ResendEmailConfirmation",
        "/Account/ConfirmEmail", "/Account/ConfirmEmailChange", "/Account/ExternalLogin",
        "/Account/PerformExternalLogin", "/Account/ResetPassword", "/Account/ResetPasswordConfirmation",
        "/Account/ForgotPasswordConfirmation", "/Account/Manage/Email", "/Account/Manage/ExternalLogins",
        "/Account/Manage/LinkExternalLogin", "/Account/Manage/PersonalData", "/Account/Manage/DownloadPersonalData",
        "/Account/Manage/SetPassword"
    };

    public async Task InvokeAsync(HttpContext contexto)
    {
        if (Rutas.Contains(contexto.Request.Path.Value?.TrimEnd('/') ?? ""))
        {
            contexto.Response.StatusCode = StatusCodes.Status404NotFound;
            contexto.Response.Headers.CacheControl = "no-store";
            contexto.Response.ContentType = "text/plain; charset=utf-8";
            await contexto.Response.WriteAsync("Esta función no está disponible. Para recuperar acceso, contacte al Administrador.", contexto.RequestAborted);
            return;
        }
        await siguiente(contexto);
    }
}
