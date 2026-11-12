using TKSC.Domain.Exceptions;
using TKSC.Application.DTOs;

namespace TKSC.Application.Interfaces.Repositories;
public interface INotificationRepository
{
    Task<IReadOnlyList<NotificationItem>> ListAsync(long userId, CancellationToken ct);
    Task<int> UnreadAsync(long userId, CancellationToken ct);
    Task<int> MarkReadAsync(long userId, long id, CancellationToken ct);
    Task MarkAllReadAsync(long userId, CancellationToken ct);
    Task<bool> IsActiveAsync(long userId, CancellationToken ct);
}
