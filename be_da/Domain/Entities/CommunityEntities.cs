namespace be_da.Domain.Entities;

public sealed record FriendshipRecord(
    long Id,
    long RequesterPlayerId,
    long AddresseePlayerId,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? RespondedAt
);

public sealed record MemberSearchResult(
    long PlayerId,
    long UserId,
    string FullName,
    string? AvatarUrl,
    string? Gender,
    string FriendshipStatus, // "None", "PendingSent", "PendingReceived", "Accepted"
    long? FriendshipId,
    IReadOnlyList<SportProfileItem> SportProfiles,
    string? Bio = null
);

public sealed record FriendItem(
    long FriendshipId,
    long FriendPlayerId,
    long FriendUserId,
    string FullName,
    string? AvatarUrl,
    DateTimeOffset ConnectedSince,
    IReadOnlyList<SportProfileItem> SportProfiles,
    string? Gender = null,
    string? Bio = null,
    int UnreadCount = 0,
    string? LastMessagePreview = null,
    DateTimeOffset? LastMessageAt = null
);

public sealed record PendingFriendRequestItem(
    long FriendshipId,
    long OtherPlayerId,
    long OtherUserId,
    string OtherFullName,
    string? OtherAvatarUrl,
    DateTimeOffset CreatedAt,
    bool IsIncoming,
    string? Gender = null,
    string? Bio = null,
    IReadOnlyList<SportProfileItem>? SportProfiles = null
);

public sealed record FriendSuggestionItem(
    long PlayerId,
    long UserId,
    string FullName,
    string? AvatarUrl,
    string? Gender,
    string FriendshipStatus,
    long? FriendshipId,
    int SharedSessionsCount,
    string? LatestSharedSessionLabel,
    IReadOnlyList<SportProfileItem> SportProfiles,
    string? Bio = null
);

