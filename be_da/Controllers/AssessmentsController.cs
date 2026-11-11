using System.Security.Claims;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TKSC.Application.Assessments;
using TKSC.Application.Services;

namespace TKSC.Controllers;

[ApiController]
[Authorize(Roles = "referee,Referee")]
[Route("api/v1/skill")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class AssessmentsController(SkillAssessmentService service, IAntiforgery antiforgery) : ControllerBase
{
    [HttpGet("sessions")]
    public Task<IActionResult> Sessions(CancellationToken ct) => Execute(() => service.SessionsAsync(ActorId, ct));

    [HttpGet("sports/{sportId:long}/rubric")]
    public Task<IActionResult> Rubric(long sportId, CancellationToken ct) =>
        Execute(() => service.GetRubricAsync(sportId, ct));

    [HttpGet("sessions/{sessionId:long}/attendees")]
    public Task<IActionResult> Attendees(long sessionId, CancellationToken ct) =>
        Execute(() => service.GetAttendeesAsync(sessionId, ActorId, ct));

    [HttpPost("sessions/{sessionId:long}/players/{playerId:long}/assessment")]
    public Task<IActionResult> Open(long sessionId, long playerId, CancellationToken ct) =>
        Execute(() => service.OpenAsync(sessionId, playerId, ActorId, ct), true);

<<<<<<< HEAD
=======
    [HttpGet("sessions/{sessionId:long}/players/{playerId:long}/context")]
    public Task<IActionResult> PlayerContext(long sessionId, long playerId, CancellationToken ct) =>
        Execute(() => service.PlayerContextAsync(sessionId, playerId, ActorId, ct));

>>>>>>> 0d40ca35efaf07c5d4887d6f8febc66c57f5578c
    [HttpGet("assessments/{id:long}")]
    public Task<IActionResult> Read(long id, CancellationToken ct) =>
        Execute(() => service.ReadAsync(id, ActorId, ct));

    [HttpPut("assessments/{id:long}")]
    public Task<IActionResult> Save(long id, SaveAssessmentRequest request, CancellationToken ct) =>
        Execute(() => service.SaveAsync(id, ActorId, request, ct), true);

    [HttpPost("assessments/{id:long}/publish")]
    public Task<IActionResult> Publish(long id, PublishAssessmentRequest request, CancellationToken ct) =>
        Execute(() => service.PublishAsync(id, ActorId, request, ct), true);

    private long ActorId => long.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;

    private async Task<IActionResult> Execute<T>(Func<Task<T>> action, bool mutation = false)
    {
        if (ActorId <= 0) return Unauthorized();
        try
        {
            if (mutation)
            {
                if (Request.Headers["X-TKSC-Request"] != "1") return BadRequest();
                await antiforgery.ValidateRequestAsync(HttpContext);
            }
            return Ok(await action());
        }
        catch (AntiforgeryValidationException)
        { return BadRequest(new { message = "Phiên bảo mật đã thay đổi. Vui lòng thử lại." }); }
        catch (AssessmentException e) { return StatusCode(e.StatusCode, new { message = e.Message }); }
    }
}
