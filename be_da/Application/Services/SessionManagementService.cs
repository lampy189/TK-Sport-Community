using be_da.Application.DTOs;
using be_da.Application.Interfaces.Repositories;
using be_da.Application.Interfaces.Services;

namespace be_da.Application.Services;

public sealed class SessionManagementService(ISessionManagementRepository repository) : ISessionManagementService
{
    public Task<SessionManagementOptions> GetOptionsAsync(long venueId, CancellationToken ct) => repository.GetOptionsAsync(venueId, ct);
    public Task<IReadOnlyList<ManagedSession>> GetSessionsAsync(long venueId, CancellationToken ct) => repository.GetSessionsAsync(venueId, ct);

    public Task<OperationResult> SaveAsync(long venueId, long actorId, long? sessionId, SaveManagedSessionRequest request, CancellationToken ct)
    {
        if (venueId <= 0 || actorId <= 0 || request.SportId <= 0 || request.CourtId <= 0 || request.TimeSlotId <= 0)
            return Task.FromResult(OperationResult.Invalid(new { code = "INVALID_SESSION", message = "Chọn đủ cơ sở, môn, sân và khung giờ." }));
        if (request.SessionDate < DateOnly.FromDateTime(DateTime.UtcNow.AddHours(7)))
            return Task.FromResult(OperationResult.Invalid(new { code = "SESSION_IN_PAST", message = "Không thể tạo hoặc sửa buổi đã qua." }));
        if (request.Capacity is < 1 or > 8 || request.PriceVnd < 0 || request.Band?.Length > 100)
            return Task.FromResult(OperationResult.Invalid(new { code = "INVALID_SESSION", message = "Sức chứa, giá hoặc điều kiện trình độ không hợp lệ." }));

        var normalized = request with { Band = string.IsNullOrWhiteSpace(request.Band) ? null : request.Band.Trim() };
        return repository.SaveAsync(venueId, actorId, sessionId, normalized, ct);
    }

    public Task<OperationResult> SetAssignmentsAsync(long venueId, long actorId, long sessionId, SessionAssignmentsRequest request, CancellationToken ct)
    {
        var refereeIds = request.RefereeIds ?? [];
        var staffIds = request.StaffIds ?? [];
        if (venueId <= 0 || actorId <= 0 || sessionId <= 0 || refereeIds.Any(id => id <= 0) || staffIds.Any(id => id <= 0)
            || refereeIds.Distinct().Count() != refereeIds.Length || staffIds.Distinct().Count() != staffIds.Length)
            return Task.FromResult(OperationResult.Invalid(new { code = "INVALID_ASSIGNMENTS", message = "Danh sách phân công không hợp lệ hoặc có nhân sự trùng." }));
        return repository.SetAssignmentsAsync(venueId, actorId, sessionId, new(refereeIds, staffIds), ct);
    }

    public Task<OperationResult> SetStatusAsync(long venueId, long actorId, long sessionId, bool open, CancellationToken ct)
    {
        if (venueId <= 0 || actorId <= 0 || sessionId <= 0)
            return Task.FromResult(OperationResult.Invalid(new { code = "INVALID_SESSION", message = "Không tìm thấy cơ sở hoặc buổi chơi." }));
        return repository.SetStatusAsync(venueId, actorId, sessionId, open, ct);
    }
}