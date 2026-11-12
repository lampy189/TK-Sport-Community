using TKSC.Domain.Exceptions;
using TKSC.Application.DTOs;

namespace be_da.Application.Interfaces.Services;
public interface INotificationService
{
    Task<NotificationPage> ListAsync(long userId, CancellationToken ct);
    Task<bool> MarkReadAsync(long userId, long id, CancellationToken ct);
    Task MarkAllReadAsync(long userId, CancellationToken ct);
    Task<string?> StreamSnapshotAsync(long userId, CancellationToken ct);
}
