using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using be_da.Application.Abstractions;

namespace be_da.Authorization;

public sealed class AccountCookieEvents(IAccountStore accounts, IWebHostEnvironment environment) : CookieAuthenticationEvents
{
    public override async Task ValidatePrincipal(CookieValidatePrincipalContext context)
    {
        var idClaim = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
        var subject = context.Principal?.FindFirstValue(AccountPrincipal.GoogleSubject);
        var account = long.TryParse(idClaim, out var id)
            ? await accounts.FindByIdAsync(id, context.HttpContext.RequestAborted) : null;
        var developmentLogin = context.Principal?.FindFirstValue(AccountPrincipal.LoginMethod) == "development";
        var validIdentity = developmentLogin
            ? environment.IsDevelopment() || environment.IsEnvironment("Testing")
            : !string.IsNullOrEmpty(subject) && string.Equals(subject, account?.GoogleSubject, StringComparison.Ordinal);
        if (account is null || !account.CanSignIn || !validIdentity)
        {
            context.RejectPrincipal();
            await context.HttpContext.SignOutAsync(AccountPrincipal.Scheme);
            return;
        }
        // Refresh authorization from DB on every request: never retain a stale privileged role.
        context.ReplacePrincipal(AccountPrincipal.Create(account, developmentLogin));
    }

    public override Task RedirectToLogin(RedirectContext<CookieAuthenticationOptions> context)
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        return Task.CompletedTask;
    }

    public override Task RedirectToAccessDenied(RedirectContext<CookieAuthenticationOptions> context)
    {
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        return Task.CompletedTask;
    }
}
