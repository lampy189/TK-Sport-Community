using be_da.Application.Sessions;
using be_da.Domain.Entities;

namespace be_da.Application.Abstractions;

public interface ISessionStore
{
    Task<IReadOnlyList<TimeSlotSummary>> GetTimeSlotsAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<VenueSummary>> GetVenuesAsync(CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<VenueSummary>>([]);
    Task<IReadOnlyList<SessionSummary>> GetSessionsAsync(SessionFilter filter, long? currentUserId, CancellationToken cancellationToken);
    Task<SessionDetails?> GetSessionByIdAsync(long id, long? currentUserId, CancellationToken cancellationToken);
    Task<RegisterSessionResult> RegisterSessionAsync(long userId, long sessionId, CancellationToken cancellationToken);
    Task<RegisterSessionResult> RegisterLateEntryAsync(long userId, long sessionId, long expectedRevision, CancellationToken cancellationToken)
        => throw new NotSupportedException("Late-entry registration is not supported by this store.");
}
