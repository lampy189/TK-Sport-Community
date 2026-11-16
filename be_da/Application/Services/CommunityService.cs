using be_da.Application.DTOs;
using be_da.Application.Interfaces.Services;
using be_da.Application.Interfaces.Repositories;
using be_da.Application.Interfaces.External;

namespace be_da.Application.Services;
public sealed class CommunityService(IFriendshipStore friendshipStore, IFirebaseNotificationService notificationService) : ICommunityService
{
    public async Task<OperationResult> SearchMembers(long userId, string? actorName, string? query, int limit = 20, CancellationToken cancellationToken = default)
    {
        var members = await friendshipStore.SearchMembersAsync(userId, query, limit, cancellationToken);
        return OperationResult.Success(members);
    }

    public async Task<OperationResult> GetFriendSuggestions(long userId, string? actorName, CancellationToken cancellationToken)
    {
        var suggestions = await friendshipStore.GetFriendSuggestionsAsync(userId, cancellationToken);
        return OperationResult.Success(suggestions);
    }

    public async Task<OperationResult> GetFriends(long userId, string? actorName, CancellationToken cancellationToken)
    {
        var friends = await friendshipStore.GetFriendsAsync(userId, cancellationToken);
        return OperationResult.Success(friends);
    }

    public async Task<OperationResult> GetPendingRequests(long userId, string? actorName, CancellationToken cancellationToken)
    {
        var requests = await friendshipStore.GetPendingRequestsAsync(userId, cancellationToken);
        return OperationResult.Success(requests);
    }

    public async Task<OperationResult> SendFriendRequest(long userId, string? actorName, SendFriendRequest request, CancellationToken cancellationToken)
    {
        if (!request.TargetPlayerId.HasValue || request.TargetPlayerId.Value <= 0)
            return OperationResult.Invalid(new { code = "invalid_target_player_id", message = "Target player ID is required and must be positive." });
        var result = await friendshipStore.SendFriendRequestAsync(userId, request.TargetPlayerId.Value, cancellationToken);
        if (!result.Success)
        {
            return result.ErrorCode switch
            {
                "target_player_not_found" => OperationResult.Missing(new { code = result.ErrorCode, message = result.Message }),
                "already_friends" or "friend_request_pending" => OperationResult.Conflict(new { code = result.ErrorCode, message = result.Message }),
                _ => OperationResult.Invalid(new { code = result.ErrorCode, message = result.Message })};
        }

        var targetUserId = await friendshipStore.GetUserIdByPlayerIdAsync(request.TargetPlayerId.Value, cancellationToken);
        if (targetUserId.HasValue)
        {
            var senderName = actorName ?? "Một người chơi";
            await notificationService.SendPushNotificationAsync(targetUserId.Value, "Lời mời kết bạn mới", $"{senderName} đã gửi cho bạn một lời mời kết bạn trên TKSC.", new Dictionary<string, string> { ["type"] = "friend_request", ["friendshipId"] = result.FriendshipId.ToString() ?? "" }, cancellationToken);
        }

        return OperationResult.Success(result);
    }

    public async Task<OperationResult> RespondFriendRequest(long userId, string? actorName, long id, RespondFriendRequest request, CancellationToken cancellationToken)
    {
        if (!request.Accept.HasValue)
            return OperationResult.Invalid(new { code = "invalid_decision", message = "Accept field is required (true or false)." });
        var result = await friendshipStore.RespondFriendRequestAsync(userId, id, request.Accept.Value, cancellationToken);
        if (!result.Success)
        {
            return result.ErrorCode switch
            {
                "request_not_found" => OperationResult.Missing(new { code = result.ErrorCode, message = result.Message }),
                "unauthorized_to_respond" => OperationResult.Denied(),
                _ => OperationResult.Invalid(new { code = result.ErrorCode, message = result.Message })};
        }

        if (request.Accept.Value)
        {
            var myPlayerId = await friendshipStore.GetPlayerIdByUserIdAsync(userId, cancellationToken);
            if (myPlayerId.HasValue)
            {
                var responderFullName = actorName ?? "Bạn của bạn";
                var friends = await friendshipStore.GetFriendsAsync(userId, cancellationToken);
                var connectedFriend = friends.FirstOrDefault(f => f.FriendshipId == id);
                if (connectedFriend is not null)
                {
                    await notificationService.SendPushNotificationAsync(connectedFriend.FriendUserId, "Lời mời kết bạn đã được chấp nhận", $"{responderFullName} đã đồng ý lời mời kết bạn của bạn.", new Dictionary<string, string> { ["type"] = "friend_accepted", ["friendshipId"] = id.ToString() }, cancellationToken);
                }
            }
        }

        return OperationResult.Success(result);
    }

    public async Task<OperationResult> RemoveFriend(long userId, string? actorName, long friendPlayerId, CancellationToken cancellationToken)
    {
        var result = await friendshipStore.RemoveFriendAsync(userId, friendPlayerId, cancellationToken);
        if (!result.Success)
        {
            return OperationResult.Missing(new { code = result.ErrorCode, message = result.Message });
        }

        return OperationResult.Completed();
    }

    public async Task<OperationResult> NotifyChatMessage(long userId, string? actorName, NotifyChatMessageRequest request, CancellationToken cancellationToken)
    {
        if (!request.RecipientPlayerId.HasValue || request.RecipientPlayerId.Value <= 0)
            return OperationResult.Invalid(new { code = "invalid_recipient_id", message = "Recipient player ID is required." });
        if (string.IsNullOrWhiteSpace(request.MessagePreview))
            return OperationResult.Invalid(new { code = "invalid_message_preview", message = "Message preview cannot be blank." });
        var areFriends = await friendshipStore.AreFriendsAsync(userId, request.RecipientPlayerId.Value, cancellationToken);
        if (!areFriends)
        {
            return OperationResult.Denied(new { code = "not_friends", message = "You can only exchange messages and notifications with accepted friends." });
        }

        var recipientUserId = await friendshipStore.GetUserIdByPlayerIdAsync(request.RecipientPlayerId.Value, cancellationToken);
        if (!recipientUserId.HasValue)
        {
            return OperationResult.Missing(new { code = "recipient_not_found", message = "Recipient player not found." });
        }

        var senderName = actorName ?? "Người chơi";
        var senderPlayerId = await friendshipStore.GetPlayerIdByUserIdAsync(userId, cancellationToken);
        await notificationService.SendPushNotificationAsync(recipientUserId.Value, $"Tin nhắn mới từ {senderName}", request.MessagePreview.Trim(), new Dictionary<string, string> { ["type"] = "chat_message", ["senderPlayerId"] = senderPlayerId?.ToString() ?? "", ["senderName"] = senderName }, cancellationToken);
        return OperationResult.Success(new { success = true, message = "Notification queued for delivery." });
    }

    public async Task<OperationResult> GetConversation(long userId, string? actorName, long friendPlayerId, int limit = 50, CancellationToken cancellationToken = default)
    {
        var areFriends = await friendshipStore.AreFriendsAsync(userId, friendPlayerId, cancellationToken);
        if (!areFriends)
        {
            return OperationResult.Denied(new { code = "not_friends", message = "You can only view messages with accepted friends." });
        }

        var messages = await friendshipStore.GetConversationAsync(userId, friendPlayerId, limit, cancellationToken);
        return OperationResult.Success(messages);
    }

    public async Task<OperationResult> SendMessage(long userId, string? actorName, SendMessageRequest request, CancellationToken cancellationToken)
    {
        if (!request.RecipientPlayerId.HasValue || request.RecipientPlayerId.Value <= 0)
            return OperationResult.Invalid(new { code = "invalid_recipient_id", message = "Recipient player ID is required." });
        if (string.IsNullOrWhiteSpace(request.Content))
            return OperationResult.Invalid(new { code = "invalid_message_content", message = "Nội dung tin nhắn không được để trống." });
        var trimmedContent = request.Content.Trim();
        if (trimmedContent.Length > 2000)
            return OperationResult.Invalid(new { code = "message_too_long", message = "Tin nhắn không được vượt quá 2000 ký tự." });
        var areFriends = await friendshipStore.AreFriendsAsync(userId, request.RecipientPlayerId.Value, cancellationToken);
        if (!areFriends)
        {
            return OperationResult.Denied(new { code = "not_friends", message = "You can only send messages to accepted friends." });
        }

        var result = await friendshipStore.SendMessageAsync(userId, request.RecipientPlayerId.Value, trimmedContent, cancellationToken);
        if (result is null)
        {
            return OperationResult.Missing(new { code = "recipient_not_found", message = "Recipient player was not found." });
        }

        var senderName = actorName ?? "Người chơi";
        var preview = trimmedContent.StartsWith("[[TKSC_SESSION_INVITE:", StringComparison.Ordinal) ? "Đã gửi lời mời cùng đặt sân chơi thể thao." : (trimmedContent.Length > 100 ? trimmedContent[..100] + "..." : trimmedContent);
        await notificationService.SendPushNotificationAsync(result.RecipientUserId, $"Tin nhắn mới từ {senderName}", preview, new Dictionary<string, string> { ["type"] = "chat_message", ["senderPlayerId"] = result.SenderPlayerId.ToString(), ["senderName"] = senderName, ["messageId"] = result.Id.ToString() }, cancellationToken);
        return OperationResult.Created(result);
    }

    public async Task<OperationResult> MarkMessagesAsRead(long userId, string? actorName, long friendPlayerId, CancellationToken cancellationToken)
    {
        await friendshipStore.MarkMessagesAsReadAsync(userId, friendPlayerId, cancellationToken);
        return OperationResult.Completed();
    }

    public async Task<OperationResult> RegisterFcmToken(long userId, string? actorName, RegisterFcmTokenRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.FcmToken) || request.FcmToken.Length < 10)
            return OperationResult.Invalid(new { code = "invalid_fcm_token", message = "Valid FCM token is required." });
        await friendshipStore.SaveFcmTokenAsync(userId, request.FcmToken.Trim(), request.DeviceType?.Trim(), cancellationToken);
        return OperationResult.Completed();
    }
}
