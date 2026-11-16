using be_da.Application.DTOs;
using be_da.Domain.Entities;

namespace be_da.Application.Interfaces.Services;
public interface ISessionService
{
    bool IsDateRangeValid(DateOnly? fromDate, DateOnly? toDate);
    Task<IReadOnlyList<TimeSlotSummary>> GetTimeSlotsAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<VenueSummary>> GetVenuesAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<SessionSummary>> GetSessionsAsync(SessionFilter filter, long? currentUserId, CancellationToken cancellationToken);
    Task<SessionDetails?> GetSessionByIdAsync(long id, long? currentUserId, CancellationToken cancellationToken);
    Task<RegisterSessionResult> RegisterSessionAsync(long userId, long sessionId, CancellationToken cancellationToken);
    Task<RegisterSessionResult> RegisterLateEntryAsync(long userId, long sessionId, long expectedRevision, CancellationToken cancellationToken);
}
