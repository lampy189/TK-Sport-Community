using TKSC.Application.DTOs;
using TKSC.Domain.Exceptions;
using be_da.Application.Interfaces.Services;
using System.Security.Claims;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TKSC.Application.Services;

namespace TKSC.Presentation.Controllers;
[ApiController]
[Authorize(Roles = "player,Player,referee,Referee,manager,Manager")]
[Route("api/v1/skill-reviews")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class SkillReviewsController(ISkillReviewService service, IAntiforgery antiforgery) : ControllerBase
{
    private long Actor => long.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;
    private string Role => User.FindFirstValue(ClaimTypes.Role)?.ToLowerInvariant() ?? "";

    [HttpGet("exceptions")]
    public Task<IActionResult> Exceptions([FromQuery] long? sessionId, CancellationToken ct) => Execute(() => service.Exceptions(Actor, Role, sessionId, ct));
    [HttpPost("sessions/{id:long}/exceptions"), Authorize(Roles = "player,Player")]
    public Task<IActionResult> RequestException(long id, ReviewInput input, CancellationToken ct) => Execute(() => service.RequestException(id, Actor, input, ct), true);
    [HttpPost("exceptions/{id:long}/decision"), Authorize(Roles = "referee,Referee,manager,Manager")]
    public Task<IActionResult> DecideException(long id, ExceptionDecision input, CancellationToken ct) => Execute(() => service.DecideException(id, Actor, Role, input, ct), true);
    [HttpGet("appeals")]
    public Task<IActionResult> Appeals(CancellationToken ct) => Execute(() => service.Appeals(Actor, Role, ct));
    [HttpPost("assessments/{id:long}/appeals"), Authorize(Roles = "player,Player")]
    public Task<IActionResult> RequestAppeal(long id, ReviewInput input, CancellationToken ct) => Execute(() => service.RequestAppeal(id, Actor, input, ct), true);
    [HttpPost("appeals/{id:long}/review"), Authorize(Roles = "referee,Referee")]
    public Task<IActionResult> Review(long id, ReviewInput input, CancellationToken ct) => Execute(() => service.ReviewAppeal(id, Actor, input, ct), true);
    [HttpPost("appeals/{id:long}/decision"), Authorize(Roles = "manager,Manager")]
    public Task<IActionResult> DecideAppeal(long id, AppealDecision input, CancellationToken ct) => Execute(() => service.DecideAppeal(id, Actor, input, ct), true);
    private async Task<IActionResult> Execute(Func<Task<object>> action, bool mutation = false)
    {
        if (Actor <= 0)
            return Unauthorized();
        try
        {
            if (mutation)
            {
                if (Request.Headers["X-TKSC-Request"] != "1")
                    return BadRequest();
                await antiforgery.ValidateRequestAsync(HttpContext);
            }

            return Ok(await action());
        }
        catch (AntiforgeryValidationException)
        {
            return BadRequest(new { message = "Phiên bảo mật đã thay đổi. Vui lòng thử lại." });
        }
        catch (AssessmentException e)
        {
            return StatusCode(e.StatusCode, new { message = e.Message });
        }
    }
}
