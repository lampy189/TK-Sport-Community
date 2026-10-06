using System.Security.Claims;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using be_da.Authorization;

namespace be_da.Controllers;

[ApiController]
[Route("api/auth")]
[EnableRateLimiting("auth")]
public sealed class AuthController(IConfiguration configuration, IAntiforgery antiforgery) : ControllerBase
{
    [AllowAnonymous]
    [HttpGet("csrf")]
    public IActionResult Csrf() => Ok(new { token = antiforgery.GetAndStoreTokens(HttpContext).RequestToken });

    [AllowAnonymous]
    [HttpPost("google")]
    public async Task<IActionResult> Google()
    {
        if (!await HasValidCsrf()) return BadRequest(new { code = "invalid_csrf_token" });
        if (User.Identity?.IsAuthenticated == true)
            return Conflict(new { code = "sign_out_before_switching_account" });
        if (string.IsNullOrWhiteSpace(configuration["Authentication:Google:ClientId"])
            || string.IsNullOrWhiteSpace(configuration["Authentication:Google:ClientSecret"]))
            return StatusCode(503, new { code = "google_login_not_configured" });
        // Fixed local destination: no open returnUrl redirect and no tokens in the URL.
        return Challenge(new AuthenticationProperties { RedirectUri = "/api/auth/me" }, GoogleDefaults.AuthenticationScheme);
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
    public async Task<IActionResult> Logout()
    {
        if (!await HasValidCsrf()) return BadRequest(new { code = "invalid_csrf_token" });
        await HttpContext.SignOutAsync(AccountPrincipal.Scheme);
        return NoContent();
    }

    private async Task<bool> HasValidCsrf()
    {
        try { await antiforgery.ValidateRequestAsync(HttpContext); return true; }
        catch (AntiforgeryValidationException) { return false; }
    }
}
