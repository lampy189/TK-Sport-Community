using System.Security.Claims;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using be_da.Application.Abstractions;
using be_da.Authorization;

namespace be_da.Controllers;

[ApiController]
[Route("api/auth")]
[EnableRateLimiting("auth")]
public sealed class AuthController(IConfiguration configuration, IAntiforgery antiforgery, IWebHostEnvironment environment) : ControllerBase
{
    [AllowAnonymous]
    [HttpGet("csrf")]
    public IActionResult Csrf() => Ok(new { token = antiforgery.GetAndStoreTokens(HttpContext).RequestToken });

    [AllowAnonymous]
    [HttpPost("google")]

    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status302Found)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> Google()
    {
        if (!await HasValidCsrf()) return BadRequest(new { code = "invalid_csrf_token" });
        if (User.Identity?.IsAuthenticated == true)
            return Conflict(new { code = "sign_out_before_switching_account" });
        if (string.IsNullOrWhiteSpace(configuration["Authentication:Google:ClientId"])
            || string.IsNullOrWhiteSpace(configuration["Authentication:Google:ClientSecret"]))
            return StatusCode(503, new { code = "google_login_not_configured" });
        // Fixed local destination: no open returnUrl redirect and no tokens in the URL.
        var destination = environment.IsDevelopment() && Request.Headers["X-TKSC-Swagger"] == "1"
            ? "/swagger/index.html?googleLogin=complete" : "/api/auth/me";
        return Challenge(new AuthenticationProperties { RedirectUri = destination }, GoogleDefaults.AuthenticationScheme);
    }

    [Authorize]
    [HttpGet("me")]
    public IActionResult Me() => Ok(new
    {
        id = User.FindFirstValue(ClaimTypes.NameIdentifier),
        email = User.FindFirstValue(ClaimTypes.Email),
        fullName = User.FindFirstValue(ClaimTypes.Name),
        role = User.FindFirstValue(ClaimTypes.Role)
    });

    [Authorize]
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Logout()
    {
        if (!await HasValidCsrf()) return BadRequest(new { code = "invalid_csrf_token" });
        await HttpContext.SignOutAsync(AccountPrincipal.Scheme);
        return NoContent();
    }

    public sealed record DevLoginRequest(string Email);

    [AllowAnonymous]
    [HttpPost("dev-login")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DevLogin([FromBody] DevLoginRequest request, [FromServices] IAccountStore accountStore, CancellationToken cancellationToken)
    {
        if (!environment.IsDevelopment() && !environment.IsEnvironment("Testing")) return NotFound();
        if (!await HasValidCsrf()) return BadRequest(new { code = "invalid_csrf_token" });
        if (string.IsNullOrWhiteSpace(request.Email)) return BadRequest(new { code = "email_required" });

        var account = await accountStore.FindByEmailAsync(request.Email.Trim(), cancellationToken);
        if (account == null) return NotFound(new { code = "user_not_found", message = $"No user found with email {request.Email}" });
        if (!account.CanSignIn) return Forbid();

        var principal = AccountPrincipal.Create(account);
        await HttpContext.SignInAsync(AccountPrincipal.Scheme, principal);
        return Ok(new
        {
            id = account.Id,
            email = account.Email,
            fullName = account.FullName,
            role = account.Role,
            status = account.Status
        });
    }

    private async Task<bool> HasValidCsrf()
    {
        try { await antiforgery.ValidateRequestAsync(HttpContext); return true; }
        catch (AntiforgeryValidationException) { return false; }
    }
}
