using TKSC.Application.DTOs;

namespace TKSC.Application.Services;

public interface ITicketCheckInService
{
    Task<CheckInResult> ExecuteAsync(string? ticketCode, long actorId, bool manager, bool confirm);
}
