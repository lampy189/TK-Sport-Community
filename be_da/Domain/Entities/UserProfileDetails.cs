namespace be_da.Domain.Entities;

public sealed record UserProfileDetails(
    long Id,
    string Email,
    string FullName,
    string Role,
    string Status,
    DateOnly? DateOfBirth,
    string? Gender,
    string? AvatarUrl,
    IReadOnlyList<SportProfileItem> SportProfiles,
    long? PlayerId = null,
    string? Bio = null
);

public sealed record SportProfileItem(
    string SportCode,
    string SportName,
    short? CurrentScore,
    string? CurrentBand,
    string? RatingStatus,
    long SportId = 0
);

public sealed record PublicRatingHistoryItem(
    long Id,
    string SportCode,
    string SportName,
    short? OldScore,
    short? NewScore,
    string? OldBand,
    string? NewBand,
    string? AssessmentType,
    DateOnly? SessionDate,
    string? CourtName
);

public sealed record PublicPlayerProfileDetails(
    long PlayerId,
    long UserId,
    string FullName,
    string? AvatarUrl,
    string? Gender,
    DateOnly? DateOfBirth,
    string? Bio,
    bool IsMe,
    string FriendshipStatus, // "Self", "None", "PendingSent", "PendingReceived", "Accepted"
    long? FriendshipId,
    IReadOnlyList<SportProfileItem> SportProfiles,
    IReadOnlyList<PublicRatingHistoryItem> RecentRatingHistory
);

