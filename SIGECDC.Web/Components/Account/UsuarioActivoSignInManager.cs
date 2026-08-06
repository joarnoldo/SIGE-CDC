using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using SIGECDC.Persistence.Identity;

namespace SIGECDC.Web.Components.Account;

internal sealed class UsuarioActivoSignInManager(
    UserManager<ApplicationUser> userManager,
    IHttpContextAccessor contextAccessor,
    IUserClaimsPrincipalFactory<ApplicationUser> claimsFactory,
    IOptions<IdentityOptions> optionsAccessor,
    ILogger<SignInManager<ApplicationUser>> logger,
    IAuthenticationSchemeProvider schemes,
    IUserConfirmation<ApplicationUser> confirmation)
    : SignInManager<ApplicationUser>(
        userManager,
        contextAccessor,
        claimsFactory,
        optionsAccessor,
        logger,
        schemes,
        confirmation)
{
    public override async Task<bool> CanSignInAsync(ApplicationUser user)
    {
        return user.EstaActivo()
            && await base.CanSignInAsync(user);
    }

    public override async Task<bool> ValidateSecurityStampAsync(
        ApplicationUser? user,
        string? securityStamp)
    {
        return user?.EstaActivo() == true
            && await base.ValidateSecurityStampAsync(user, securityStamp);
    }

    public override async Task<SignInResult> TwoFactorRecoveryCodeSignInAsync(
        string recoveryCode)
    {
        var user = await GetTwoFactorAuthenticationUserAsync();
        if (user?.EstaActivo() != true)
        {
            return SignInResult.NotAllowed;
        }

        return await base.TwoFactorRecoveryCodeSignInAsync(recoveryCode);
    }
}
