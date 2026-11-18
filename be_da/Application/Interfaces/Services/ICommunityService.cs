using be_da.Application.DTOs;

namespace be_da.Application.Interfaces.Services;
public interface ICommunityService
{
    Task<OperationResult> SearchMembers(long userId, string? actorName, string? query, int limit = 20, CancellationToken cancellationToken = default);
    Task<OperationResult> GetFriendSuggestions(long userId, string? actorName, CancellationToken cancellationToken);
    Task<OperationResult> GetFriends(long userId, string? actorName, CancellationToken cancellationToken);
    Task<OperationResult> GetPendingRequests(long userId, string? actorName, CancellationToken cancellationToken);
    Task<OperationResult> SendFriendRequest(long userId, string? actorName, SendFriendRequest request, CancellationToken cancellationToken);
    Task<OperationResult> RespondFriendRequest(long userId, string? actorName, long id, RespondFriendRequest request, CancellationToken cancellationToken);
    Task<OperationResult> RemoveFriend(long userId, string? actorName, long friendPlayerId, CancellationToken cancellationToken);
    Task<OperationResult> NotifyChatMessage(long userId, string? actorName, NotifyChatMessageRequest request, CancellationToken cancellationToken);
    Task<OperationResult> GetConversation(long userId, string? actorName, long friendPlayerId, int limit = 50, CancellationToken cancellationToken = default);
    Task<OperationResult> SendMessage(long userId, string? actorName, SendMessageRequest request, CancellationToken cancellationToken);
    Task<OperationResult> MarkMessagesAsRead(long userId, string? actorName, long friendPlayerId, CancellationToken cancellationToken);
    Task<OperationResult> RegisterFcmToken(long userId, string? actorName, RegisterFcmTokenRequest request, CancellationToken cancellationToken);
}
