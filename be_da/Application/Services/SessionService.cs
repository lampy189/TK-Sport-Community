using be_da.Application.DTOs;
using be_da.Domain.Entities;
using be_da.Application.Interfaces.Repositories;
using be_da.Application.Interfaces.Services;

namespace be_da.Application.Services;
public sealed class SessionService(ISessionStore repository) : ISessionService
{
    public bool IsDateRangeValid(DateOnly? fromDate, DateOnly? toDate) => !fromDate.HasValue || !toDate.HasValue || fromDate.Value <= toDate.Value;
    public Task<IReadOnlyList<TimeSlotSummary>> GetTimeSlotsAsync(CancellationToken cancellationToken) => repository.GetTimeSlotsAsync(cancellationToken);
    public Task<IReadOnlyList<VenueSummary>> GetVenuesAsync(CancellationToken cancellationToken) => repository.GetVenuesAsync(cancellationToken);
    public Task<IReadOnlyList<SessionSummary>> GetSessionsAsync(SessionFilter filter, long? currentUserId, CancellationToken cancellationToken) => repository.GetSessionsAsync(filter, currentUserId, cancellationToken);
    public Task<SessionDetails?> GetSessionByIdAsync(long id, long? currentUserId, CancellationToken cancellationToken) => repository.GetSessionByIdAsync(id, currentUserId, cancellationToken);
    public Task<RegisterSessionResult> RegisterSessionAsync(long userId, long sessionId, CancellationToken cancellationToken) => repository.RegisterSessionAsync(userId, sessionId, cancellationToken);
    public Task<RegisterSessionResult> RegisterLateEntryAsync(long userId, long sessionId, long expectedRevision, CancellationToken cancellationToken) => repository.RegisterLateEntryAsync(userId, sessionId, expectedRevision, cancellationToken);
}
