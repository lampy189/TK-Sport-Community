using TKSC.Application.DTOs;
using TKSC.Domain.Entities;

namespace TKSC.Application.Interfaces.Repositories;
public interface ITicketEmailRepository
{
    Task<List<long>> PendingAsync(CancellationToken ct);
    Task WithLockedAsync(long id, Func<Notification?, Task<bool>> action, CancellationToken ct, CancellationToken saveToken);
    Task<User> UserAsync(long id, CancellationToken ct);
    Task<TicketEmailDetails?> TicketAsync(long? id, CancellationToken ct);
}
