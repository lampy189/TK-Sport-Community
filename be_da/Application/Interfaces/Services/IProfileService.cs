using be_da.Application.DTOs;
using be_da.Domain.Entities;

namespace be_da.Application.Interfaces.Services;
public interface IProfileService
{
    Task<UserProfileDetails?> GetProfileAsync(long userId, CancellationToken cancellationToken);
    Task<PublicPlayerProfileDetails?> GetPublicPlayerProfileAsync(long playerId, long? viewerUserId, CancellationToken cancellationToken);
    Task<IReadOnlyList<SportProfileItem>?> GetSportProfilesAsync(long userId, CancellationToken cancellationToken);
    AvatarFile? FindAvatar(string fileName);
    Task<OperationResult> UpdateProfileAsync(long userId, UpdateProfileRequest request, CancellationToken cancellationToken);
    Task<SportProfileResult> UpdateSportProfileAsync(long userId, string sportCode, UpdateSportProfileRequest request, CancellationToken cancellationToken);
}
