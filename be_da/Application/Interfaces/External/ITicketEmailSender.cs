using TKSC.Application.DTOs;

namespace TKSC.Application.Interfaces.External;
public interface ITicketEmailSender
{
    Task SendAsync(OutgoingEmail email, CancellationToken ct);
}
