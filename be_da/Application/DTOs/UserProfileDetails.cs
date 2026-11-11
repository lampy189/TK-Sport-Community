namespace be_da.Application.DTOs;
public sealed record UserProfileDetails(long Id, string Email, string FullName, string Role, string Status, DateOnly? DateOfBirth, string? Gender, string? AvatarUrl, IReadOnlyList<SportProfileItem> SportProfiles, long? PlayerId = null, string? Bio = null);
