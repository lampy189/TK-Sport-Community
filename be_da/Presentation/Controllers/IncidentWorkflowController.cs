using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TKSC.Application.DTOs;
using TKSC.Application.Services;

namespace TKSC.Presentation.Controllers;
[ApiController, Route("api/issue-workflow"), Authorize(Roles = "Staff,staff,Manager,manager,Owner,owner")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class IncidentWorkflowController(TKSC.Application.Interfaces.Services.IIncidentWorkflowService service, IAntiforgery csrf) : ControllerBase
{
    private long Actor => long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private async Task<IActionResult> Write(Func<Task<object>> action)
    {
        try
        {
            await csrf.ValidateRequestAsync(HttpContext);
        }
        catch (AntiforgeryValidationException)
        {
            return BadRequest(new { message = "Phiên bảo mật thay đổi; tải lại." });
        }

        return Ok(await action());
    }

    [HttpGet]
    public async Task<IActionResult> States() => Ok(await service.States(Actor));
    [HttpGet("cases")]
    public async Task<IActionResult> Cases() => Ok(await service.Cases(Actor));
    [HttpPost("cases")]
    public Task<IActionResult> Submit(OperationalRequest input) => Write(() => service.Request(Actor, input));
    [HttpPost("cases/{id:long}/decision")]
    public Task<IActionResult> Decide(long id, OperationalDecision input) => Write(() => service.Decide(Actor, id, input));
    [HttpPost("cases/{id:long}/payout")]
    public Task<IActionResult> Payout(long id, RefundPayoutInput input) => Write(() => service.RecordReturn(Actor, id, input));
    [HttpPost("cases/{id:long}/verify-return")]
    public Task<IActionResult> VerifyReturn(long id, OperationalDecision input) => Write(() => service.VerifyReturn(Actor, id, input.Reason));
    [HttpPost("cases/{id:long}/execute")]
    public Task<IActionResult> Execute(long id, OperationalDecision input) => Write(() => service.Execute(Actor, id, input.Reason));
    [HttpPost("cases/{id:long}/close")]
    public Task<IActionResult> Close(long id, OperationalDecision input) => Write(() => service.Close(Actor, id, input.Reason ?? ""));
    [HttpGet("{kind}/{id:long}")]
    public async Task<IActionResult> Detail(string kind, long id) => Ok(await service.Detail(Actor, kind, id));
    [HttpPost("{kind}/{id:long}")]
    public Task<IActionResult> Act(string kind, long id, IncidentAction input) => Write(() => service.Act(Actor, kind, id, input));
}
