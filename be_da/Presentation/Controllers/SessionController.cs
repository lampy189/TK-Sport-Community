using be_da.Application.DTOs;
using be_da.Application.Interfaces.Services;
using System.Security.Claims;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using be_da.Domain.Entities;

namespace be_da.Presentation.Controllers;
[ApiController]
[Route("api/sessions")]
public sealed class SessionController(ISessionService sessionStore, IAntiforgery antiforgery) : ControllerBase
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

    // UC-06 / SCR-04: Get active Sport Center venues for filtering
    [AllowAnonymous]
    [HttpGet("venues")]
    [ProducesResponseType(typeof(IReadOnlyList<VenueSummary>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetVenues(CancellationToken cancellationToken)
    {
        var venues = await sessionStore.GetVenuesAsync(cancellationToken);
        if (be_da.Presentation.Authorization.CenterSelection.CurrentVenue is long selected)
            venues = venues.Where(v => v.Id == selected).ToList();
        return Ok(venues);
    }

    // UC-06: Browse court sessions
    [AllowAnonymous]
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<SessionSummary>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSessions([FromQuery] long? sportId, [FromQuery] string? sport, [FromQuery] DateOnly? date, [FromQuery] long? timeSlotId, [FromQuery] string? status, [FromQuery] DateOnly? fromDate, [FromQuery] DateOnly? toDate, [FromQuery] long? venueId, [FromQuery] string? venue, [FromQuery] string? sessionType, CancellationToken cancellationToken)
    {
        if (!sessionStore.IsDateRangeValid(fromDate, toDate))
        {
            return BadRequest(new { code = "INVALID_DATE_RANGE", message = "Ngày bắt đầu (fromDate) không được lớn hơn ngày kết thúc (toDate)." });
        }

        var currentUserId = TryGetUserId(out var uid) ? uid : (long? )null;
        var restricted = be_da.Presentation.Authorization.CenterSelection.CurrentVenue;
        if (restricted.HasValue && venueId.HasValue && restricted != venueId)
            return Forbid();
        if (restricted.HasValue)
        {
            venueId = restricted;
            venue = null;
        }

        var filter = new SessionFilter(sportId, sport, date, timeSlotId, status, fromDate, toDate, venueId, venue, sessionType);
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
        var currentUserId = TryGetUserId(out var uid) ? uid : (long? )null;
        var session = await sessionStore.GetSessionByIdAsync(id, currentUserId, cancellationToken);
        if (session == null)
            return NotFound(new { code = "SESSION_NOT_FOUND", message = "Session not found." });
        if (be_da.Presentation.Authorization.CenterSelection.RestrictedVenue is long center && session.Summary.VenueId != center)
            return NotFound(new { code = "SESSION_NOT_FOUND", message = "Không tìm thấy buổi tại cơ sở đang chọn." });
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
        if (!await HasValidCsrf())
            return BadRequest(new { code = "invalid_csrf_token" });
        if (!TryGetUserId(out var playerUserId))
            return Unauthorized();
        if (be_da.Presentation.Authorization.CenterSelection.RestrictedVenue is long center)
        {
            var target = await sessionStore.GetSessionByIdAsync(id, playerUserId, cancellationToken);
            if (target is null || target.Summary.VenueId != center)
                return NotFound(new { code = "SESSION_NOT_FOUND" });
        }

        var result = await sessionStore.RegisterSessionAsync(playerUserId, id, cancellationToken);
        if (!result.Success)
        {
            return result.ErrorCode switch
            {
                "SKILL_INELIGIBLE" => BadRequest(new { code = "SKILL_INELIGIBLE", reason = result.Reason, message = result.Message ?? $"Không đủ điều kiện kỹ năng: {result.Reason}" }),
                "SESSION_FULL" => BadRequest(new { code = "SESSION_FULL", message = result.Message ?? "Buổi chơi đã đủ người." }),
                "SESSION_NOT_FOUND" => NotFound(new { code = "SESSION_NOT_FOUND", message = result.Message }),
                "ALREADY_REGISTERED" => Conflict(new { code = "ALREADY_REGISTERED", message = result.Message }),
                _ => BadRequest(new { code = result.ErrorCode, message = result.Message })};
        }

        return Ok(new { success = true, orderId = result.OrderId, orderCode = result.OrderCode, ticketId = result.TicketId, ticketCode = result.TicketCode, sessionId = id, amountVnd = result.AmountVnd, status = "Pending", expiresAt = result.ExpiresAt, message = result.Message ?? "Giữ chỗ thành công. Vui lòng thanh toán trước khi hết hạn." });
    }

    private bool TryGetUserId(out long userId)
    {
        var rawId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return long.TryParse(rawId, out userId);
    }

    private async Task<bool> HasValidCsrf()
    {
        try
        {
            await antiforgery.ValidateRequestAsync(HttpContext);
            return true;
        }
        catch (AntiforgeryValidationException)
        {
            return false;
        }
    }
}
