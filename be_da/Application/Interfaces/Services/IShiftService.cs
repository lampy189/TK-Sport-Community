using be_da.Application.DTOs;

namespace be_da.Application.Interfaces.Services;

public interface IShiftService
{
    Task<IReadOnlyList<ManagedSession>> MyShiftsAsync(long venueId, long actorId, string role, CancellationToken ct);
    Task<OperationResult> AcceptShiftAsync(long venueId, long actorId, string role, long sessionId, CancellationToken ct);
}
