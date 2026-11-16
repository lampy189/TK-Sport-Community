using TKSC.Domain.Exceptions;
using TKSC.Application.DTOs;
using TKSC.Application.Interfaces.Services;
using TKSC.Application.Interfaces.Repositories;
using TKSC.Application.Interfaces.External;
using TKSC.Domain.Entities;
using TKSC.Application.Services;

namespace be_da.Application.Interfaces.Services;
public interface ITicketCheckInService
{
    Task<IReadOnlyList<AssignedSessionSummary>> GetAssignedSessionsAsync(long actorId);
    Task<CheckInResult> ExecuteAsync(string? ticketCode, long actorId, bool manager, bool confirm);
}
