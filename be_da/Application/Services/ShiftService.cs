using be_da.Application.DTOs;
using be_da.Application.Interfaces.Repositories;
using be_da.Application.Interfaces.Services;

namespace be_da.Application.Services;

public sealed class ShiftService(IShiftRepository repository) : IShiftService
{
    public Task<IReadOnlyList<ManagedSession>> MyShiftsAsync(long venueId, long actorId, string role, CancellationToken ct) =>
        repository.MyShiftsAsync(venueId, actorId, role, ct);

    public Task<OperationResult> AcceptShiftAsync(long venueId, long actorId, string role, long sessionId, CancellationToken ct) =>
        repository.AcceptShiftAsync(venueId, actorId, role, sessionId, ct);
}
