using be_da.Application.DTOs;

namespace be_da.Application.Interfaces.Services;

public interface ISessionManagementService
{
    Task<SessionManagementOptions> GetOptionsAsync(long venueId, CancellationToken ct);
    Task<IReadOnlyList<ManagedSession>> GetSessionsAsync(long venueId, CancellationToken ct);
    Task<OperationResult> SaveAsync(long venueId, long actorId, long? sessionId, SaveManagedSessionRequest request, CancellationToken ct);
    Task<OperationResult> SetAssignmentsAsync(long venueId, long actorId, long sessionId, SessionAssignmentsRequest request, CancellationToken ct);
    Task<OperationResult> SetStatusAsync(long venueId, long actorId, long sessionId, bool open, CancellationToken ct);
}