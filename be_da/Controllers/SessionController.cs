using System.Security.Claims;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using be_da.Application.Abstractions;
using be_da.Application.Sessions;
using be_da.Domain.Entities;

namespace be_da.Controllers;

[ApiController]
[Route("api/sessions")]
public sealed class SessionController(ISessionStore sessionStore, IAntiforgery antiforgery) : ControllerBase
{
    // UC-06 / SCR-04: Get configured venue time slots for schedule/timetable view
    [AllowAnonymous]
    [HttpGet("time-slots")]
    [ProducesResponseType(typeof(IReadOnlyList<TimeSlotSummary>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTimeSlots(CancellationToken cancellationToken)
    {
        var timeSlots = await sessionStore.GetTimeSlotsAsync(cancellationToken);
        return Ok(timeSlots);
    }

    // UC-06: Browse court sessions
    [AllowAnonymous]
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<SessionSummary>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSessions(
        [FromQuery] long? sportId,
        [FromQuery] string? sport,
        [FromQuery] DateOnly? date,
        [FromQuery] long? timeSlotId,
        [FromQuery] string? status,
        [FromQuery] DateOnly? fromDate,
        [FromQuery] DateOnly? toDate,
        CancellationToken cancellationToken)
    {
        var currentUserId = TryGetUserId(out var uid) ? uid : (long?)null;
        var filter = new SessionFilter(sportId, sport, date, timeSlotId, status, fromDate, toDate);
        var sessions = await sessionStore.GetSessionsAsync(filter, currentUserId, cancellationToken);
        return Ok(sessions);
    }

    // UC-06 / SCR-05: View session details and participants
    [AllowAnonymous]
    [HttpGet("{id:long}")]
    [ProducesResponseType(typeof(SessionDetails), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSessionById(long id, CancellationToken cancellationToken)
    {
        var currentUserId = TryGetUserId(out var uid) ? uid : (long?)null;
        var session = await sessionStore.GetSessionByIdAsync(id, currentUserId, cancellationToken);
        if (session == null) return NotFound(new { code = "SESSION_NOT_FOUND", message = "Session not found." });
        return Ok(session);
    }

    // UC-07: Submit Registration Request & Check Conditions (Player only)
    [Authorize(Roles = "Player")]
    [HttpPost("{id:long}/register")]
    [ProducesResponseType(typeof(RegisterSessionResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> RegisterSession(long id, CancellationToken cancellationToken)
    {
        if (!await HasValidCsrf()) return BadRequest(new { code = "invalid_csrf_token" });
        if (!TryGetUserId(out var playerUserId)) return Unauthorized();

        var result = await sessionStore.RegisterSessionAsync(playerUserId, id, cancellationToken);
        if (!result.Success)
        {
            return result.ErrorCode switch
            {
                "SKILL_INELIGIBLE" => BadRequest(new
                {
                    code = "SKILL_INELIGIBLE",
                    reason = result.Reason,
                    message = result.Message ?? $"Không đủ điều kiện kỹ năng: {result.Reason}"
                }),
                "SESSION_FULL" => BadRequest(new
                {
                    code = "SESSION_FULL",
                    message = result.Message ?? "Buổi chơi đã đủ người."
                }),
                "SESSION_NOT_FOUND" => NotFound(new
                {
                    code = "SESSION_NOT_FOUND",
                    message = result.Message
                }),
                "ALREADY_REGISTERED" => Conflict(new
                {
                    code = "ALREADY_REGISTERED",
                    message = result.Message
                }),
                _ => BadRequest(new
                {
                    code = result.ErrorCode,
                    message = result.Message
                })
            };
        }

        return Ok(new
        {
            success = true,
            orderId = result.OrderId,
            orderCode = result.OrderCode,
            ticketId = result.TicketId,
            ticketCode = result.TicketCode,
            sessionId = id,
            amountVnd = result.AmountVnd,
            status = "Pending",
            expiresAt = result.ExpiresAt,
            message = result.Message ?? "Giữ chỗ thành công. Vui lòng thanh toán trước khi hết hạn."
        });
    }

    private bool TryGetUserId(out long userId)
    {
        var rawId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return long.TryParse(rawId, out userId);
    }

    private async Task<bool> HasValidCsrf()
    {
        try { await antiforgery.ValidateRequestAsync(HttpContext); return true; }
        catch (AntiforgeryValidationException) { return false; }
    }
}
