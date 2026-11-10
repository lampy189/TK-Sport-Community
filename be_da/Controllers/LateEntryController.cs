using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;
using be_da.Application.Sessions;
using be_da.Application.Abstractions;
using TKSC.Application.DTOs;

namespace be_da.Controllers;
[ApiController,Route("api/late-entry"),ResponseCache(NoStore=true,Location=ResponseCacheLocation.None)]
public sealed class LateEntryController(ILateEntryStore store,ISessionStore sessions,IAntiforgery antiforgery):ControllerBase
{
    [AllowAnonymous,HttpGet]
    public async Task<IActionResult> List([FromQuery]long? venueId,[FromQuery]long? sessionId,CancellationToken ct)=>Ok(await store.List(venueId,sessionId,false,ct));
    [Authorize(Roles="Staff,Manager"),HttpGet("manage")]
    public async Task<IActionResult> Manage(CancellationToken ct)=>Ok(await store.List(Venue(),null,true,ct));
    [Authorize(Roles="Player"),HttpPost("{id:long}/holds")]
    public async Task<IActionResult> Hold(long id,HoldLateEntryRequest input,CancellationToken ct)
    {
        await ValidateCsrf();
        var result=await sessions.RegisterLateEntryAsync(Actor(),id,input.ExpectedRevision,ct);
        return result.Success?Ok(result):Conflict(new{code=result.ErrorCode,message=result.Message});
    }
    private long Actor()=>long.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier),out var id)?id:throw new PaymentException(401,"Vui lòng đăng nhập.");
    private async Task ValidateCsrf()
    {
        try{await antiforgery.ValidateRequestAsync(HttpContext);}
        catch(AntiforgeryValidationException){throw new PaymentException(400,"Phiên bảo mật đã thay đổi. Vui lòng tải lại.");}
    }
    private long Venue()=>HttpContext.Items["SportCenterId"] is long id?id:throw new PaymentException(403,"Tài khoản chưa được phân công cơ sở.");
}
