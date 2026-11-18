namespace be_da.Application.DTOs;
public sealed record FriendItem(long FriendshipId, long FriendPlayerId, long FriendUserId, string FullName, string? AvatarUrl, DateTimeOffset ConnectedSince, IReadOnlyList<SportProfileItem> SportProfiles, string? Gender = null, string? Bio = null, int UnreadCount = 0, string? LastMessagePreview = null, DateTimeOffset? LastMessageAt = null);
