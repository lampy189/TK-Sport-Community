namespace be_da.Application.DTOs;
public sealed record FriendSuggestionItem(long PlayerId, long UserId, string FullName, string? AvatarUrl, string? Gender, string FriendshipStatus, long? FriendshipId, int SharedSessionsCount, string? LatestSharedSessionLabel, IReadOnlyList<SportProfileItem> SportProfiles, string? Bio = null);
