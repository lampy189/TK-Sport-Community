using TKSC.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;
using TKSC.Application.DTOs;
using TKSC.Application.Interfaces.Repositories;
using TKSC.Infrastructure.Persistence.Contexts;

namespace TKSC.Infrastructure.Persistence.Repositories;
public sealed class NotificationRepository(Sep490Context db) : INotificationRepository
{
    public async Task<IReadOnlyList<NotificationItem>> ListAsync(long userId, CancellationToken ct) => await db.Notifications.AsNoTracking().Where(n => n.UserId == userId && n.Channel == "web").OrderByDescending(n => n.Id).Take(50).Select(n => new NotificationItem(n.Id, n.Title, n.ReferenceId, n.ReferenceType, n.Status)).ToListAsync(ct);
    public Task<int> UnreadAsync(long userId, CancellationToken ct) => db.Notifications.AsNoTracking().CountAsync(n => n.UserId == userId && n.Channel == "web" && n.Status == "unread", ct);
    public Task<int> MarkReadAsync(long userId, long id, CancellationToken ct) => db.Notifications.Where(n => n.Id == id && n.UserId == userId && n.Channel == "web").ExecuteUpdateAsync(s => s.SetProperty(n => n.Status, "read"), ct);
    public async Task MarkAllReadAsync(long userId, CancellationToken ct) => await db.Notifications.Where(n => n.UserId == userId && n.Channel == "web" && n.Status == "unread").ExecuteUpdateAsync(s => s.SetProperty(n => n.Status, "read"), ct);
    public Task<bool> IsActiveAsync(long userId, CancellationToken ct) => db.Users.AsNoTracking().AnyAsync(u => u.Id == userId && u.DeletedAt == null && u.Status.ToLower() == "active", ct);
}
