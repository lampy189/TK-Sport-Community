using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Antiforgery;
using System.Security.Claims;
using TKSC.Application.Services;
using TKSC.Application.DTOs;

namespace TKSC.Presentation.Controllers;
[ApiController, Route("api/ticket-recovery"), Authorize(Roles = "Staff,staff,Manager,manager,Owner,owner")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class TicketRecoveryController(TKSC.Application.Interfaces.Services.ITicketRecoveryService service, IAntiforgery csrf) : ControllerBase
{
    private long Actor => long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    public async Task<IActionResult> List() => Ok(await service.List(Actor));
    [HttpPost("{id:long}/{actionName}")]
    public async Task<IActionResult> Act(long id, string actionName, PaymentRecoveryInput input)
    {
        try
        {
            await csrf.ValidateRequestAsync(HttpContext);
        }
        catch (AntiforgeryValidationException)
        {
            return BadRequest(new { message = "Tải lại phiên bảo mật." });
        }

        return Ok(await service.Act(Actor, id, actionName, input));
    }
}
