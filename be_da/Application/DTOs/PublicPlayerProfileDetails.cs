namespace be_da.Application.DTOs;
public sealed record PublicPlayerProfileDetails(long PlayerId, long UserId, string FullName, string? AvatarUrl, string? Gender, DateOnly? DateOfBirth, string? Bio, bool IsMe, string FriendshipStatus, // "Self", "None", "PendingSent", "PendingReceived", "Accepted"
 long? FriendshipId, IReadOnlyList<SportProfileItem> SportProfiles, IReadOnlyList<PublicRatingHistoryItem> RecentRatingHistory);
