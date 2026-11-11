using be_da.Application.DTOs;
using be_da.Domain.Entities;
using be_da.Application.Interfaces.Repositories;
using be_da.Application.Interfaces.Services;

namespace be_da.Application.Services;
public sealed class ProfileService(IProfileStore repository, be_da.Application.Interfaces.External.IAvatarStorage files) : IProfileService
{
    public Task<UserProfileDetails?> GetProfileAsync(long userId, CancellationToken cancellationToken) => repository.GetProfileAsync(userId, cancellationToken);
    public Task<PublicPlayerProfileDetails?> GetPublicPlayerProfileAsync(long playerId, long? viewerUserId, CancellationToken cancellationToken) => repository.GetPublicPlayerProfileAsync(playerId, viewerUserId, cancellationToken);
    public Task<IReadOnlyList<SportProfileItem>?> GetSportProfilesAsync(long userId, CancellationToken cancellationToken) => repository.GetSportProfilesAsync(userId, cancellationToken);
    public AvatarFile? FindAvatar(string fileName) => files.Find(fileName);
    public async Task<OperationResult> UpdateProfileAsync(long userId, UpdateProfileRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.FullName))
            return OperationResult.Invalid(new { code = "invalid_full_name", message = "Họ và tên không được để trống." });
        var trimmedName = request.FullName.Trim();
        if (trimmedName.Length is < 2 or > 100)
            return OperationResult.Invalid(new { code = "invalid_full_name", message = "Họ và tên phải có độ dài từ 2 đến 100 ký tự." });
        var todayVn = DateOnly.FromDateTime(DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(7)).DateTime);
        if (request.DateOfBirth.HasValue && (request.DateOfBirth.Value > todayVn || request.DateOfBirth.Value < new DateOnly(1920, 1, 1)))
            return OperationResult.Invalid(new { code = "invalid_date_of_birth", message = "Ngày sinh không hợp lệ (không được ở tương lai hoặc trước năm 1920)." });
        if (!string.IsNullOrWhiteSpace(request.Gender))
        {
            var g = request.Gender.Trim();
            if (!string.Equals(g, "Male", StringComparison.OrdinalIgnoreCase) && !string.Equals(g, "Female", StringComparison.OrdinalIgnoreCase) && !string.Equals(g, "Other", StringComparison.OrdinalIgnoreCase) && !string.Equals(g, "Nam", StringComparison.OrdinalIgnoreCase) && !string.Equals(g, "Nữ", StringComparison.OrdinalIgnoreCase) && !string.Equals(g, "Nu", StringComparison.OrdinalIgnoreCase) && !string.Equals(g, "Khác", StringComparison.OrdinalIgnoreCase))
            {
                return OperationResult.Invalid(new { code = "invalid_gender", message = "Giới tính không hợp lệ (Male, Female, Other)." });
            }
        }

        if (request.Bio is not null && request.Bio.Trim().Length > 500)
            return OperationResult.Invalid(new { code = "invalid_bio", message = "Giới thiệu bản thân không được vượt quá 500 ký tự." });
        var normalizedRequest = request;
        if (!string.IsNullOrWhiteSpace(request.AvatarUrl))
        {
            var rawAvatar = request.AvatarUrl.Trim();
            if (rawAvatar.StartsWith("data:image/", StringComparison.OrdinalIgnoreCase))
            {
                var savedUrl = await files.SaveDataUrlAsync(userId, rawAvatar, cancellationToken);
                if (savedUrl is null)
                {
                    return OperationResult.Invalid(new { code = "invalid_avatar_url", message = "Ảnh đại diện tải lên không hợp lệ." });
                }

                normalizedRequest = request with
                {
                    AvatarUrl = savedUrl
                };
            }
            else if (rawAvatar.StartsWith("/api/profile/avatars/", StringComparison.OrdinalIgnoreCase))
            {
                normalizedRequest = request with
                {
                    AvatarUrl = rawAvatar
                };
            }
            else if (!Uri.TryCreate(rawAvatar, UriKind.Absolute, out var uri) || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            {
                return OperationResult.Invalid(new { code = "invalid_avatar_url", message = "Avatar URL must be a valid HTTP or HTTPS URL." });
            }
        }

        var updated = await repository.UpdateProfileAsync(userId, normalizedRequest, cancellationToken);
        return updated is null ? OperationResult.Missing(new { code = "profile_not_found" }) : OperationResult.Success(updated);
    }

    public Task<SportProfileResult> UpdateSportProfileAsync(long userId, string sportCode, UpdateSportProfileRequest request, CancellationToken cancellationToken) => !request.Score.HasValue || request.Score.Value < 0 || request.Score.Value > 100 ? Task.FromResult(new SportProfileResult(false, "invalid_score", "Score must be between 0 and 100.", null)) : repository.UpdateSportProfileAsync(userId, sportCode, request, cancellationToken);
}
