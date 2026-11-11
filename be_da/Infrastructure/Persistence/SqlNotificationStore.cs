using Microsoft.EntityFrameworkCore;
using be_da.Application.Abstractions;
using TKSC.Infrastructure.DbContext;

namespace be_da.Infrastructure.Persistence;

public sealed class SqlNotificationStore(Sep490Context db) : INotificationStore
{
    public async Task<NotificationFeed> GetFeedAsync(long userId, CancellationToken cancellationToken)
    {
        var query = db.Notifications.AsNoTracking().Where(n => n.UserId == userId && n.Channel == "web");
        var items = await query.OrderByDescending(n => n.Id).Take(50)
            .Select(n => new NotificationFeedItem(n.Id, n.Title, n.ReferenceId, n.ReferenceType, n.Status))
            .ToListAsync(cancellationToken);
        var unread = await query.CountAsync(n => n.Status == "unread", cancellationToken);
        return new NotificationFeed(items, unread, items.FirstOrDefault()?.Id ?? 0);
    }

    public async Task<bool> MarkReadAsync(long userId, long notificationId, CancellationToken cancellationToken) =>
        await db.Notifications.Where(n => n.Id == notificationId && n.UserId == userId && n.Channel == "web")
            .ExecuteUpdateAsync(update => update.SetProperty(n => n.Status, "read"), cancellationToken) != 0;

    public async Task MarkAllReadAsync(long userId, CancellationToken cancellationToken)
    {
        await db.Notifications.Where(n => n.UserId == userId && n.Channel == "web" && n.Status == "unread")
            .ExecuteUpdateAsync(update => update.SetProperty(n => n.Status, "read"), cancellationToken);
    }

    public Task<bool> IsActiveUserAsync(long userId, CancellationToken cancellationToken) =>
        db.Users.AsNoTracking().AnyAsync(
            user => user.Id == userId && user.DeletedAt == null && user.Status.ToLower() == "active",
            cancellationToken);
}
