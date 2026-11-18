using TKSC.Application.Interfaces.Services;
using System.Security.Claims;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TKSC.Application.DTOs;
using TKSC.Application.Services;
namespace TKSC.Presentation.Controllers;
[ApiController,Authorize(Roles="Staff,staff,Manager,manager")]
[Route("api/v1/commerce/payment-recovery")]
[ResponseCache(NoStore=true,Location=ResponseCacheLocation.None)]
public sealed class PaymentRecoveryController(IPaymentRecoveryService service,IAntiforgery csrf) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct) => Ok(await service.ListAsync(ct));
    [HttpPost("{id:long:min(1)}/{actionName}")]
    public async Task<IActionResult> Act(long id,string actionName,PaymentRecoveryInput input,CancellationToken ct)
    {
        if(!long.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier),out var actor) || actor<=0) return Unauthorized();
        if(Request.Headers["X-TKSC-Request"]!="1") return BadRequest(new {message="Thiếu mã yêu cầu ứng dụng."});
        try { await csrf.ValidateRequestAsync(HttpContext); }
        catch(AntiforgeryValidationException) { return BadRequest(new {code="invalid_csrf_token"}); }
        return Ok(await service.ActAsync(id,actor,actionName,input,ct));
    }
}
