using be_da.Application.Accounts;
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
<<<<<<< HEAD
public sealed class AuthController(IConfiguration configuration, IAntiforgery antiforgery, IWebHostEnvironment environment) : ControllerBase
=======
public sealed class AuthController(IConfiguration configuration, IAntiforgery antiforgery, IWebHostEnvironment environment,
    CenterSelection centers, IAccountStore accounts) : ControllerBase
>>>>>>> 0d40ca35efaf07c5d4887d6f8febc66c57f5578c
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
<<<<<<< HEAD
    public async Task<IActionResult> Google([FromQuery] string? portal = null)
=======
    public async Task<IActionResult> Google([FromQuery] string? portal = null, [FromQuery] long? venueId = null)
>>>>>>> 0d40ca35efaf07c5d4887d6f8febc66c57f5578c
    {
        if (!await HasValidCsrf()) return BadRequest(new { code = "invalid_csrf_token" });
        if (User.Identity?.IsAuthenticated == true)
            return Conflict(new { code = "sign_out_before_switching_account" });
        if (string.IsNullOrWhiteSpace(configuration["Authentication:Google:ClientId"])
            || string.IsNullOrWhiteSpace(configuration["Authentication:Google:ClientSecret"]))
            return StatusCode(503, new { code = "google_login_not_configured" });
        if (!LoginPortal.IsValid(portal)) return BadRequest(new { code = "invalid_login_portal" });
        // Fixed local destination: no open returnUrl redirect and no tokens in the URL.
        var destination = environment.IsDevelopment()
            ? (Request.Headers["X-TKSC-Swagger"] == "1" ? "/swagger/index.html?googleLogin=complete" : "http://localhost:5173")
            : "/api/auth/me";
        if (portal is not null)
            destination = (environment.IsDevelopment() ? "http://localhost:5173" : "") + LoginPortal.Path(portal);
        var properties = new AuthenticationProperties { RedirectUri = destination };
        if (portal is not null) properties.Items["login_portal"] = portal;
<<<<<<< HEAD
=======
        if (venueId.HasValue)
        {
            if (venueId <= 0) return BadRequest(new { code = "center_unavailable" });
            properties.Items["selected_center"] = venueId.Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }
>>>>>>> 0d40ca35efaf07c5d4887d6f8febc66c57f5578c
        return Challenge(properties, GoogleDefaults.AuthenticationScheme);
    }

    [Authorize]
    [HttpGet("me")]
    public IActionResult Me() => Ok(new
    {
        id = User.FindFirstValue(ClaimTypes.NameIdentifier),
        email = User.FindFirstValue(ClaimTypes.Email),
        fullName = User.FindFirstValue(ClaimTypes.Name),
<<<<<<< HEAD
        role = User.FindFirstValue(ClaimTypes.Role)
    });

=======
        venueId = User.FindFirstValue(SportCenterConnection.VenueClaim),
        selectedVenueId = User.FindFirstValue(CenterSelection.SelectedClaim),
        centerSelectionEnabled = centers.Enabled,
        centerRequired = centers.Enabled && !User.IsInRole("Owner") && !User.IsInRole("SystemAdmin")
            && string.IsNullOrWhiteSpace(User.FindFirstValue(CenterSelection.SelectedClaim)),
        role = User.FindFirstValue(ClaimTypes.Role)
    });

    [AllowAnonymous, HttpGet("centers")]
    public async Task<IActionResult> Centers(CancellationToken ct)
    {
        var account = long.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id)
            ? await accounts.FindByIdAsync(id, ct) : null;
        return Ok(new { enabled = centers.Enabled, centers = await centers.Choices(account, ct) });
    }

    public sealed record SelectCenterRequest(long? VenueId);
    [Authorize, HttpPost("center")]
    public async Task<IActionResult> SelectCenter(SelectCenterRequest request, CancellationToken ct)
    {
        if (!await HasValidCsrf()) return BadRequest(new { code = "invalid_csrf_token" });
        if (!centers.Enabled) return Conflict(new { code = "centers_not_enabled", message = "Chức năng nhiều cơ sở chưa được kích hoạt." });
        var account = await accounts.FindByIdAsync(long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!), ct);
        if (account is null || !account.CanSignIn) return Unauthorized();
        if (request.VenueId is null && account.Role != "Owner")
            return BadRequest(new { code = "center_required", message = "Vui lòng chọn cơ sở." });
        try
        {
            var selected = await centers.Resolve(account, request.VenueId, ct);
            var dev = User.FindFirstValue(AccountPrincipal.LoginMethod) == "development";
            await HttpContext.SignInAsync(AccountPrincipal.Scheme, AccountPrincipal.Create(account, dev, selected));
            return Ok(new { selectedVenueId = selected });
        }
        catch (SignInRejectedException ex)
        { return StatusCode(403, new { code = ex.Code, message = "Cơ sở không hoạt động hoặc không thuộc quyền truy cập của tài khoản." }); }
    }

>>>>>>> 0d40ca35efaf07c5d4887d6f8febc66c57f5578c
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

<<<<<<< HEAD
    public sealed record DevLoginRequest(string Email, string? Portal = null);
=======
    public sealed record DevLoginRequest(string Email, string? Portal = null, long? VenueId = null);
>>>>>>> 0d40ca35efaf07c5d4887d6f8febc66c57f5578c

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
        if (!LoginPortal.IsValid(request.Portal)) return BadRequest(new { code = "invalid_login_portal" });
        if (!LoginPortal.Allows(request.Portal, account.Role))
            return StatusCode(403, new { code = "portal_access_denied", message = "Tài khoản không thuộc cổng đăng nhập này." });

<<<<<<< HEAD
        var principal = AccountPrincipal.Create(account, developmentLogin: true);
=======
        long? selected;
        try { selected = await centers.Resolve(account, request.VenueId, cancellationToken); }
        catch (SignInRejectedException ex)
        { return StatusCode(403, new { code = ex.Code, message = "Cơ sở không hoạt động hoặc tài khoản chưa được phân công đúng cơ sở." }); }
        var principal = AccountPrincipal.Create(account, developmentLogin: true, selectedVenueId: selected);
>>>>>>> 0d40ca35efaf07c5d4887d6f8febc66c57f5578c
        await HttpContext.SignInAsync(AccountPrincipal.Scheme, principal);
        return Ok(new
        {
            id = account.Id,
            email = account.Email,
            fullName = account.FullName,
            role = account.Role,
<<<<<<< HEAD
=======
            venueId = account.VenueId,
            selectedVenueId = selected,
            centerSelectionEnabled = centers.Enabled,
            centerRequired = centers.Enabled && account.Role is not ("Owner" or "SystemAdmin") && selected is null,
>>>>>>> 0d40ca35efaf07c5d4887d6f8febc66c57f5578c
            status = account.Status
        });
    }

    private async Task<bool> HasValidCsrf()
    {
        try { await antiforgery.ValidateRequestAsync(HttpContext); return true; }
        catch (AntiforgeryValidationException) { return false; }
    }
}
