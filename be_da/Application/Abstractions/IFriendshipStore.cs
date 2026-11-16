using be_da.Application.Community;
using be_da.Domain.Entities;

namespace be_da.Application.Abstractions;

public interface IFriendshipStore
{
    Task<long?> GetPlayerIdByUserIdAsync(long userId, CancellationToken cancellationToken);
    Task<long?> GetUserIdByPlayerIdAsync(long playerId, CancellationToken cancellationToken);
    Task<IReadOnlyList<MemberSearchResult>> SearchMembersAsync(long currentUserId, string? query, int limit, CancellationToken cancellationToken);
    Task<IReadOnlyList<FriendSuggestionItem>> GetFriendSuggestionsAsync(long currentUserId, CancellationToken cancellationToken);
    Task<IReadOnlyList<FriendItem>> GetFriendsAsync(long currentUserId, CancellationToken cancellationToken);
    Task<IReadOnlyList<PendingFriendRequestItem>> GetPendingRequestsAsync(long currentUserId, CancellationToken cancellationToken);
    Task<FriendshipActionResult> SendFriendRequestAsync(long currentUserId, long targetPlayerId, CancellationToken cancellationToken);
    Task<FriendshipActionResult> RespondFriendRequestAsync(long currentUserId, long friendshipId, bool accept, CancellationToken cancellationToken);
    Task<FriendshipActionResult> RemoveFriendAsync(long currentUserId, long friendPlayerId, CancellationToken cancellationToken);
    Task<bool> AreFriendsAsync(long currentUserId, long otherPlayerId, CancellationToken cancellationToken);
    Task SaveFcmTokenAsync(long userId, string fcmToken, string? deviceType, CancellationToken cancellationToken);
    Task<IReadOnlyList<string>> GetFcmTokensByUserIdAsync(long userId, CancellationToken cancellationToken);

    // Direct Chat (UC-24, SCR-14, FR-14)
    Task<ChatMessageItem?> SendMessageAsync(long currentUserId, long recipientPlayerId, string content, CancellationToken cancellationToken);
    Task<IReadOnlyList<ChatMessageItem>> GetConversationAsync(long currentUserId, long otherPlayerId, int limit, CancellationToken cancellationToken);
    Task<bool> MarkMessagesAsReadAsync(long currentUserId, long senderPlayerId, CancellationToken cancellationToken);
}

