using System.Security.Claims;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using be_da.Application.Abstractions;
using be_da.Application.Community;
using be_da.Domain.Entities;

namespace be_da.Controllers;

[ApiController]
[Route("api/community")]
[Authorize]
public sealed class CommunityController(
    IFriendshipStore friendshipStore,
    IFirebaseNotificationService notificationService,
    IAntiforgery antiforgery) : ControllerBase
{
    // UC-25c / SCR-13: Search members and view public sport ratings
    [HttpGet("members")]
    [ProducesResponseType(typeof(IReadOnlyList<MemberSearchResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> SearchMembers([FromQuery] string? query, [FromQuery] int limit = 20, CancellationToken cancellationToken = default)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var members = await friendshipStore.SearchMembersAsync(userId, query, limit, cancellationToken);
        return Ok(members);
    }

    // UC-25c: Friend suggestions based on players who played in the same session/slot
    [HttpGet("friends/suggestions")]
    [ProducesResponseType(typeof(IReadOnlyList<FriendSuggestionItem>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetFriendSuggestions(CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var suggestions = await friendshipStore.GetFriendSuggestionsAsync(userId, cancellationToken);
        return Ok(suggestions);
    }

    [HttpGet("friends")]
    [ProducesResponseType(typeof(IReadOnlyList<FriendItem>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetFriends(CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var friends = await friendshipStore.GetFriendsAsync(userId, cancellationToken);
        return Ok(friends);
    }

    [HttpGet("friends/requests")]
    [ProducesResponseType(typeof(IReadOnlyList<PendingFriendRequestItem>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetPendingRequests(CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var requests = await friendshipStore.GetPendingRequestsAsync(userId, cancellationToken);
        return Ok(requests);
    }

    [HttpPost("friends/request")]
    [ProducesResponseType(typeof(FriendshipActionResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> SendFriendRequest([FromBody] SendFriendRequest request, CancellationToken cancellationToken)
    {
        if (!await HasValidCsrf()) return BadRequest(new { code = "invalid_csrf_token" });
        if (!TryGetUserId(out var userId)) return Unauthorized();

        if (!request.TargetPlayerId.HasValue || request.TargetPlayerId.Value <= 0)
            return BadRequest(new { code = "invalid_target_player_id", message = "Target player ID is required and must be positive." });

        var result = await friendshipStore.SendFriendRequestAsync(userId, request.TargetPlayerId.Value, cancellationToken);
        if (!result.Success)
        {
            return result.ErrorCode switch
            {
                "target_player_not_found" => NotFound(new { code = result.ErrorCode, message = result.Message }),
                "already_friends" or "friend_request_pending" => Conflict(new { code = result.ErrorCode, message = result.Message }),
                _ => BadRequest(new { code = result.ErrorCode, message = result.Message })
            };
        }

        var targetUserId = await friendshipStore.GetUserIdByPlayerIdAsync(request.TargetPlayerId.Value, cancellationToken);
        if (targetUserId.HasValue)
        {
            var senderName = User.FindFirstValue(ClaimTypes.Name) ?? "Một người chơi";
            await notificationService.SendPushNotificationAsync(
                targetUserId.Value,
                "Lời mời kết bạn mới",
                $"{senderName} đã gửi cho bạn một lời mời kết bạn trên TKSC.",
                new Dictionary<string, string>
                {
                    ["type"] = "friend_request",
                    ["friendshipId"] = result.FriendshipId.ToString() ?? ""
                },
                cancellationToken
            );
        }

        return Ok(result);
    }

    [HttpPost("friends/{id:long}/respond")]
    [ProducesResponseType(typeof(FriendshipActionResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RespondFriendRequest(
        [FromRoute] long id,
        [FromBody] RespondFriendRequest request,
        CancellationToken cancellationToken)
    {
        if (!await HasValidCsrf()) return BadRequest(new { code = "invalid_csrf_token" });
        if (!TryGetUserId(out var userId)) return Unauthorized();

        if (!request.Accept.HasValue)
            return BadRequest(new { code = "invalid_decision", message = "Accept field is required (true or false)." });

        var result = await friendshipStore.RespondFriendRequestAsync(userId, id, request.Accept.Value, cancellationToken);
        if (!result.Success)
        {
            return result.ErrorCode switch
            {
                "request_not_found" => NotFound(new { code = result.ErrorCode, message = result.Message }),
                "unauthorized_to_respond" => Forbid(),
                _ => BadRequest(new { code = result.ErrorCode, message = result.Message })
            };
        }

        if (request.Accept.Value)
        {
            var myPlayerId = await friendshipStore.GetPlayerIdByUserIdAsync(userId, cancellationToken);
            if (myPlayerId.HasValue)
            {
                var responderFullName = User.FindFirstValue(ClaimTypes.Name) ?? "Bạn của bạn";
                var friends = await friendshipStore.GetFriendsAsync(userId, cancellationToken);
                var connectedFriend = friends.FirstOrDefault(f => f.FriendshipId == id);
                if (connectedFriend is not null)
                {
                    await notificationService.SendPushNotificationAsync(
                        connectedFriend.FriendUserId,
                        "Lời mời kết bạn đã được chấp nhận",
                        $"{responderFullName} đã đồng ý lời mời kết bạn của bạn.",
                        new Dictionary<string, string>
                        {
                            ["type"] = "friend_accepted",
                            ["friendshipId"] = id.ToString()
                        },
                        cancellationToken
                    );
                }
            }
        }

        return Ok(result);
    }

    [HttpDelete("friends/{friendPlayerId:long}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveFriend([FromRoute] long friendPlayerId, CancellationToken cancellationToken)
    {
        if (!await HasValidCsrf()) return BadRequest(new { code = "invalid_csrf_token" });
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var result = await friendshipStore.RemoveFriendAsync(userId, friendPlayerId, cancellationToken);
        if (!result.Success)
        {
            return NotFound(new { code = result.ErrorCode, message = result.Message });
        }

        return NoContent();
    }

    [HttpPost("chat/notify-message")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> NotifyChatMessage([FromBody] NotifyChatMessageRequest request, CancellationToken cancellationToken)
    {
        if (!await HasValidCsrf()) return BadRequest(new { code = "invalid_csrf_token" });
        if (!TryGetUserId(out var userId)) return Unauthorized();

        if (!request.RecipientPlayerId.HasValue || request.RecipientPlayerId.Value <= 0)
            return BadRequest(new { code = "invalid_recipient_id", message = "Recipient player ID is required." });

        if (string.IsNullOrWhiteSpace(request.MessagePreview))
            return BadRequest(new { code = "invalid_message_preview", message = "Message preview cannot be blank." });

        var areFriends = await friendshipStore.AreFriendsAsync(userId, request.RecipientPlayerId.Value, cancellationToken);
        if (!areFriends)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new
            {
                code = "not_friends",
                message = "You can only exchange messages and notifications with accepted friends."
            });
        }

        var recipientUserId = await friendshipStore.GetUserIdByPlayerIdAsync(request.RecipientPlayerId.Value, cancellationToken);
        if (!recipientUserId.HasValue)
        {
            return NotFound(new { code = "recipient_not_found", message = "Recipient player not found." });
        }

        var senderName = User.FindFirstValue(ClaimTypes.Name) ?? "Người chơi";
        var senderPlayerId = await friendshipStore.GetPlayerIdByUserIdAsync(userId, cancellationToken);

        await notificationService.SendPushNotificationAsync(
            recipientUserId.Value,
            $"Tin nhắn mới từ {senderName}",
            request.MessagePreview.Trim(),
            new Dictionary<string, string>
            {
                ["type"] = "chat_message",
                ["senderPlayerId"] = senderPlayerId?.ToString() ?? "",
                ["senderName"] = senderName
            },
            cancellationToken
        );

        return Ok(new { success = true, message = "Notification queued for delivery." });
    }

    // UC-25c / UC-24 / SCR-14: Get conversation history between accepted friends
    [HttpGet("messages/{friendPlayerId:long}")]
    [ProducesResponseType(typeof(IReadOnlyList<ChatMessageItem>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetConversation(
        [FromRoute] long friendPlayerId,
        [FromQuery] int limit = 50,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var areFriends = await friendshipStore.AreFriendsAsync(userId, friendPlayerId, cancellationToken);
        if (!areFriends)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new
            {
                code = "not_friends",
                message = "You can only view messages with accepted friends."
            });
        }

        var messages = await friendshipStore.GetConversationAsync(userId, friendPlayerId, limit, cancellationToken);
        return Ok(messages);
    }

    // UC-25c / UC-24 / SCR-14: Send direct chat message to an accepted friend
    [HttpPost("messages")]
    [ProducesResponseType(typeof(ChatMessageItem), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SendMessage(
        [FromBody] SendMessageRequest request,
        CancellationToken cancellationToken)
    {
        if (!await HasValidCsrf()) return BadRequest(new { code = "invalid_csrf_token" });
        if (!TryGetUserId(out var userId)) return Unauthorized();

        if (!request.RecipientPlayerId.HasValue || request.RecipientPlayerId.Value <= 0)
            return BadRequest(new { code = "invalid_recipient_id", message = "Recipient player ID is required." });

        if (string.IsNullOrWhiteSpace(request.Content))
            return BadRequest(new { code = "invalid_message_content", message = "Nội dung tin nhắn không được để trống." });

        var trimmedContent = request.Content.Trim();
        if (trimmedContent.Length > 2000)
            return BadRequest(new { code = "message_too_long", message = "Tin nhắn không được vượt quá 2000 ký tự." });


        var areFriends = await friendshipStore.AreFriendsAsync(userId, request.RecipientPlayerId.Value, cancellationToken);
        if (!areFriends)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new
            {
                code = "not_friends",
                message = "You can only send messages to accepted friends."
            });
        }

        var result = await friendshipStore.SendMessageAsync(userId, request.RecipientPlayerId.Value, trimmedContent, cancellationToken);
        if (result is null)
        {
            return NotFound(new { code = "recipient_not_found", message = "Recipient player was not found." });
        }

        var senderName = User.FindFirstValue(ClaimTypes.Name) ?? "Người chơi";

        var preview = trimmedContent.StartsWith("[[TKSC_SESSION_INVITE:", StringComparison.Ordinal)
            ? "Đã gửi lời mời cùng đặt sân chơi thể thao."
            : (trimmedContent.Length > 100 ? trimmedContent[..100] + "..." : trimmedContent);

        await notificationService.SendPushNotificationAsync(
            result.RecipientUserId,
            $"Tin nhắn mới từ {senderName}",
            preview,
            new Dictionary<string, string>
            {
                ["type"] = "chat_message",
                ["senderPlayerId"] = result.SenderPlayerId.ToString(),
                ["senderName"] = senderName,
                ["messageId"] = result.Id.ToString()
            },
            cancellationToken
        );

        return StatusCode(StatusCodes.Status201Created, result);
    }

    // UC-25c / UC-24: Mark incoming messages from a friend as read
    [HttpPost("messages/{friendPlayerId:long}/read")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> MarkMessagesAsRead(
        [FromRoute] long friendPlayerId,
        CancellationToken cancellationToken)
    {
        if (!await HasValidCsrf()) return BadRequest(new { code = "invalid_csrf_token" });
        if (!TryGetUserId(out var userId)) return Unauthorized();

        await friendshipStore.MarkMessagesAsReadAsync(userId, friendPlayerId, cancellationToken);
        return NoContent();
    }

    [HttpPost("fcm-token")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RegisterFcmToken([FromBody] RegisterFcmTokenRequest request, CancellationToken cancellationToken)
    {
        if (!await HasValidCsrf()) return BadRequest(new { code = "invalid_csrf_token" });
        if (!TryGetUserId(out var userId)) return Unauthorized();

        if (string.IsNullOrWhiteSpace(request.FcmToken) || request.FcmToken.Length < 10)
            return BadRequest(new { code = "invalid_fcm_token", message = "Valid FCM token is required." });

        await friendshipStore.SaveFcmTokenAsync(userId, request.FcmToken.Trim(), request.DeviceType?.Trim(), cancellationToken);
        return NoContent();
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

