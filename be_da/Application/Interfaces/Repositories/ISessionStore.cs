using be_da.Application.DTOs;
using be_da.Domain.Entities;

namespace be_da.Application.Interfaces.Repositories;
public interface ISessionStore
{
    Task<RegisterSessionResult> AcceptInvitationAsync(long userId, long invitationId, CancellationToken ct) => throw new NotSupportedException();
    Task<IReadOnlyList<TimeSlotSummary>> GetTimeSlotsAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<VenueSummary>> GetVenuesAsync(CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<VenueSummary>>([]);
    Task<IReadOnlyList<SessionSummary>> GetSessionsAsync(SessionFilter filter, long? currentUserId, CancellationToken cancellationToken);
    Task<SessionDetails?> GetSessionByIdAsync(long id, long? currentUserId, CancellationToken cancellationToken);
    Task<RegisterSessionResult> RegisterSessionAsync(long userId, long sessionId, CancellationToken cancellationToken);
    Task<RegisterSessionResult> RegisterLateEntryAsync(long userId, long sessionId, long expectedRevision, CancellationToken cancellationToken) => throw new NotSupportedException("Late-entry registration is not supported by this store.");
}
