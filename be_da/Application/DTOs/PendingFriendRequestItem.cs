namespace be_da.Application.DTOs;
public sealed record PendingFriendRequestItem(long FriendshipId, long OtherPlayerId, long OtherUserId, string OtherFullName, string? OtherAvatarUrl, DateTimeOffset CreatedAt, bool IsIncoming, string? Gender = null, string? Bio = null, IReadOnlyList<SportProfileItem>? SportProfiles = null);
