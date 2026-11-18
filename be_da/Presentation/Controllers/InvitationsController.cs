using System.Security.Claims;
using be_da.Application.DTOs;
using be_da.Application.Interfaces.Services;
using be_da.Presentation.Filters;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace be_da.Presentation.Controllers;
[ApiController, Authorize(Roles = "Player"), Route("api/invitations")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class InvitationsController(IInvitationService service, IAntiforgery antiforgery) : ControllerBase
{
    private long UserId => long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct) => (await service.ListAsync(UserId, ct)).ToResponse();
    [HttpGet("sessions/{sessionId:long}/candidates")]
    public async Task<IActionResult> Candidates(long sessionId, CancellationToken ct) => (await service.CandidatesAsync(UserId, sessionId, ct)).ToResponse();
    [HttpPost("sessions/{sessionId:long}")]
    public async Task<IActionResult> Send(long sessionId, InviteSessionRequest request, CancellationToken ct)
    {
        if (!await Csrf()) return BadRequest(new { code = "invalid_csrf_token" });
        return (await service.SendAsync(UserId, sessionId, request, ct)).ToResponse();
    }
    [HttpPost("{id:long}/reply")]
    public async Task<IActionResult> Reply(long id, InvitationReply request, CancellationToken ct)
    {
        if (!await Csrf()) return BadRequest(new { code = "invalid_csrf_token" });
        return (await service.ReplyAsync(UserId, id, request.Accept, ct)).ToResponse();
    }
    private async Task<bool> Csrf()
    {
        try { await antiforgery.ValidateRequestAsync(HttpContext); return true; }
        catch (AntiforgeryValidationException) { return false; }
    }
}
