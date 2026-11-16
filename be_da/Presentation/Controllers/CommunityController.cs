using be_da.Presentation.Filters;
using be_da.Application.DTOs;
using be_da.Application.Interfaces.External;
using be_da.Application.Interfaces.Services;
using System.Security.Claims;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using be_da.Domain.Entities;

namespace be_da.Presentation.Controllers;
[ApiController]
[Route("api/community")]
[Authorize]
public sealed class CommunityController(ICommunityService service, IAntiforgery antiforgery) : ControllerBase
{
    // UC-25c / SCR-13: Search members and view public sport ratings
    [HttpGet("members")]
    [ProducesResponseType(typeof(IReadOnlyList<MemberSearchResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> SearchMembers([FromQuery] string? query, [FromQuery] int limit = 20, CancellationToken cancellationToken = default)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized();
        return (await service.SearchMembers(userId, User.FindFirstValue(ClaimTypes.Name), query, limit, cancellationToken)).ToResponse();
    }

    // UC-25c: Friend suggestions based on players who played in the same session/slot
    [HttpGet("friends/suggestions")]
    [ProducesResponseType(typeof(IReadOnlyList<FriendSuggestionItem>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetFriendSuggestions(CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized();
        return (await service.GetFriendSuggestions(userId, User.FindFirstValue(ClaimTypes.Name), cancellationToken)).ToResponse();
    }

    [HttpGet("friends")]
    [ProducesResponseType(typeof(IReadOnlyList<FriendItem>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetFriends(CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized();
        return (await service.GetFriends(userId, User.FindFirstValue(ClaimTypes.Name), cancellationToken)).ToResponse();
    }

    [HttpGet("friends/requests")]
    [ProducesResponseType(typeof(IReadOnlyList<PendingFriendRequestItem>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetPendingRequests(CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized();
        return (await service.GetPendingRequests(userId, User.FindFirstValue(ClaimTypes.Name), cancellationToken)).ToResponse();
    }

    [HttpPost("friends/request")]
    [ProducesResponseType(typeof(FriendshipActionResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> SendFriendRequest([FromBody] SendFriendRequest request, CancellationToken cancellationToken)
    {
        if (!await HasValidCsrf())
            return BadRequest(new { code = "invalid_csrf_token" });
        if (!TryGetUserId(out var userId))
            return Unauthorized();
        return (await service.SendFriendRequest(userId, User.FindFirstValue(ClaimTypes.Name), request, cancellationToken)).ToResponse();
    }

    [HttpPost("friends/{id:long}/respond")]
    [ProducesResponseType(typeof(FriendshipActionResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RespondFriendRequest([FromRoute] long id, [FromBody] RespondFriendRequest request, CancellationToken cancellationToken)
    {
        if (!await HasValidCsrf())
            return BadRequest(new { code = "invalid_csrf_token" });
        if (!TryGetUserId(out var userId))
            return Unauthorized();
        return (await service.RespondFriendRequest(userId, User.FindFirstValue(ClaimTypes.Name), id, request, cancellationToken)).ToResponse();
    }

    [HttpDelete("friends/{friendPlayerId:long}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveFriend([FromRoute] long friendPlayerId, CancellationToken cancellationToken)
    {
        if (!await HasValidCsrf())
            return BadRequest(new { code = "invalid_csrf_token" });
        if (!TryGetUserId(out var userId))
            return Unauthorized();
        return (await service.RemoveFriend(userId, User.FindFirstValue(ClaimTypes.Name), friendPlayerId, cancellationToken)).ToResponse();
    }

    [HttpPost("chat/notify-message")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> NotifyChatMessage([FromBody] NotifyChatMessageRequest request, CancellationToken cancellationToken)
    {
        if (!await HasValidCsrf())
            return BadRequest(new { code = "invalid_csrf_token" });
        if (!TryGetUserId(out var userId))
            return Unauthorized();
        return (await service.NotifyChatMessage(userId, User.FindFirstValue(ClaimTypes.Name), request, cancellationToken)).ToResponse();
    }

    // UC-25c / UC-24 / SCR-14: Get conversation history between accepted friends
    [HttpGet("messages/{friendPlayerId:long}")]
    [ProducesResponseType(typeof(IReadOnlyList<ChatMessageItem>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetConversation([FromRoute] long friendPlayerId, [FromQuery] int limit = 50, CancellationToken cancellationToken = default)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized();
        return (await service.GetConversation(userId, User.FindFirstValue(ClaimTypes.Name), friendPlayerId, limit, cancellationToken)).ToResponse();
    }

    // UC-25c / UC-24 / SCR-14: Send direct chat message to an accepted friend
    [HttpPost("messages")]
    [ProducesResponseType(typeof(ChatMessageItem), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SendMessage([FromBody] SendMessageRequest request, CancellationToken cancellationToken)
    {
        if (!await HasValidCsrf())
            return BadRequest(new { code = "invalid_csrf_token" });
        if (!TryGetUserId(out var userId))
            return Unauthorized();
        return (await service.SendMessage(userId, User.FindFirstValue(ClaimTypes.Name), request, cancellationToken)).ToResponse();
    }

    // UC-25c / UC-24: Mark incoming messages from a friend as read
    [HttpPost("messages/{friendPlayerId:long}/read")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> MarkMessagesAsRead([FromRoute] long friendPlayerId, CancellationToken cancellationToken)
    {
        if (!await HasValidCsrf())
            return BadRequest(new { code = "invalid_csrf_token" });
        if (!TryGetUserId(out var userId))
            return Unauthorized();
        return (await service.MarkMessagesAsRead(userId, User.FindFirstValue(ClaimTypes.Name), friendPlayerId, cancellationToken)).ToResponse();
    }

    [HttpPost("fcm-token")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RegisterFcmToken([FromBody] RegisterFcmTokenRequest request, CancellationToken cancellationToken)
    {
        if (!await HasValidCsrf())
            return BadRequest(new { code = "invalid_csrf_token" });
        if (!TryGetUserId(out var userId))
            return Unauthorized();
        return (await service.RegisterFcmToken(userId, User.FindFirstValue(ClaimTypes.Name), request, cancellationToken)).ToResponse();
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
