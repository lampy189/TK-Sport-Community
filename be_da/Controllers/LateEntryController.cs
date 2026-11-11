using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;
using be_da.Application.Sessions;
using be_da.Application.Abstractions;
using TKSC.Application.DTOs;

namespace be_da.Controllers;
[ApiController,Route("api/late-entry"),ResponseCache(NoStore=true,Location=ResponseCacheLocation.None)]
public sealed class LateEntryController(ILateEntryStore store,ISessionStore sessions,IAntiforgery antiforgery,
    IConfiguration configuration, IWebHostEnvironment environment):ControllerBase
{
    [AllowAnonymous,HttpGet]
    public async Task<IActionResult> List([FromQuery]long? venueId,[FromQuery]long? sessionId,CancellationToken ct)
    {
        var selected = be_da.Authorization.CenterSelection.RestrictedVenue;
        if (selected.HasValue && venueId.HasValue && selected != venueId) return Forbid();
        return Ok(await store.List(selected ?? venueId,sessionId,false,ct));
    }
    [Authorize(Roles="Staff,Manager"),HttpGet("manage")]
    public async Task<IActionResult> Manage(CancellationToken ct)=>Ok(await store.List(Venue(),null,true,ct));
    [Authorize(Roles="Player"),HttpPost("{id:long}/holds")]
    public async Task<IActionResult> Hold(long id,HoldLateEntryRequest input,CancellationToken ct)
    {
        await ValidateCsrf();
        if (be_da.Authorization.CenterSelection.RestrictedVenue is long center)
        {
            var target = await sessions.GetSessionByIdAsync(id, Actor(), ct);
            if (target is null || target.Summary.VenueId != center) return NotFound(new { code = "SESSION_NOT_FOUND" });
        }
        var result=await sessions.RegisterLateEntryAsync(Actor(),id,input.ExpectedRevision,ct);
        return result.Success?Ok(result):Conflict(new{code=result.ErrorCode,message=result.Message});
    }
    private long Actor()=>long.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier),out var id)?id:throw new PaymentException(401,"Vui lòng đăng nhập.");
    private async Task ValidateCsrf()
    {
        try{await antiforgery.ValidateRequestAsync(HttpContext);}
        catch(AntiforgeryValidationException){throw new PaymentException(400,"Phiên bảo mật đã thay đổi. Vui lòng tải lại.");}
    }
    private long? Venue()
    {
        // Explicit local legacy mode only. The SQL store also verifies that no
        // multi-center schema/policy exists before permitting an unscoped read.
        if (!be_da.Authorization.CenterSelection.Scoped && environment.IsDevelopment() && configuration.GetValue("LateEntry:Standalone", false)) return null;
        return HttpContext.Items["SportCenterId"] is long id?id:throw new PaymentException(403,"Tài khoản chưa được phân công cơ sở.");
    }
}
