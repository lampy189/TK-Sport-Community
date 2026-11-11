using TKSC.Application.DTOs;

namespace TKSC.Application.Services;

public interface IRefereeSessionsService
{
    Task<IReadOnlyList<AssignedSessionSummary>> GetAssignedSessionsAsync(long actorId);
}
