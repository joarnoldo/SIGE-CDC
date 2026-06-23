using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using SIGECDC.Persistence.Identity;

namespace SIGECDC.Web.Components.Account
{
    // Cuando se configure un servicio real de correo, se puede retirar el bloque de confirmación manual usado para desarrollo.
    internal sealed class IdentityNoOpEmailSender : IEmailSender<ApplicationUser>
    {
        private readonly IEmailSender emailSender = new NoOpEmailSender();

        public Task SendConfirmationLinkAsync(ApplicationUser user, string email, string confirmationLink) =>
            emailSender.SendEmailAsync(email, "Confirmar su correo electrónico", $"Confirme su cuenta desde <a href='{confirmationLink}'>este enlace</a>.");

        public Task SendPasswordResetLinkAsync(ApplicationUser user, string email, string resetLink) =>
            emailSender.SendEmailAsync(email, "Restablecer su contraseña", $"Restablezca su contraseña desde <a href='{resetLink}'>este enlace</a>.");

        public Task SendPasswordResetCodeAsync(ApplicationUser user, string email, string resetCode) =>
            emailSender.SendEmailAsync(email, "Restablecer su contraseña", $"Restablezca su contraseña usando el siguiente código: {resetCode}");
    }
}
