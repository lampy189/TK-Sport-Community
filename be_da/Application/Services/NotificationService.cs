using TKSC.Domain.Exceptions;
using System.Text.Json;
using TKSC.Application.DTOs;
using TKSC.Application.Interfaces.Repositories;
using be_da.Application.Interfaces.Services;

namespace TKSC.Application.Services;
public sealed class NotificationService(INotificationRepository repository) : INotificationService
{
    public async Task<NotificationPage> ListAsync(long userId, CancellationToken ct)
    {
        var items = await repository.ListAsync(userId, ct);
        return new(items, await repository.UnreadAsync(userId, ct), items.FirstOrDefault()?.Id ?? 0);
    }

    public async Task<bool> MarkReadAsync(long userId, long id, CancellationToken ct) => await repository.MarkReadAsync(userId, id, ct) != 0;
    public Task MarkAllReadAsync(long userId, CancellationToken ct) => repository.MarkAllReadAsync(userId, ct);
    public async Task<string?> StreamSnapshotAsync(long userId, CancellationToken ct)
    {
        if (!await repository.IsActiveAsync(userId, ct))
            return null;
        var page = await ListAsync(userId, ct);
        return JsonSerializer.Serialize(new { items = page.Items.Select(n => new { id = n.Id, title = n.Title, referenceId = n.ReferenceId, referenceType = n.ReferenceType, status = n.Status }), unread = page.Unread });
    }
}
