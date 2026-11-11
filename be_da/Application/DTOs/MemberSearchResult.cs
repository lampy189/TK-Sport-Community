namespace be_da.Application.DTOs;
public sealed record MemberSearchResult(long PlayerId, long UserId, string FullName, string? AvatarUrl, string? Gender, string FriendshipStatus, // "None", "PendingSent", "PendingReceived", "Accepted"
 long? FriendshipId, IReadOnlyList<SportProfileItem> SportProfiles, string? Bio = null);
