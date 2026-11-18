using Microsoft.EntityFrameworkCore;
using TKSC.Application.DTOs;
using TKSC.Application.Interfaces.Repositories;
using TKSC.Domain.Entities;
using TKSC.Infrastructure.Persistence.Contexts;
namespace TKSC.Infrastructure.Persistence.Repositories;
public sealed class NotificationDeliveryRepository(Sep490Context db) : INotificationDeliveryRepository
{
    public Task<List<EmailDeliveryItem>> EmailsAsync(long userId, CancellationToken ct) => db.Notifications.AsNoTracking()
        .Where(n => n.UserId == userId && n.Channel == "email").OrderByDescending(n => n.Id).Take(50)
        .Select(n => new EmailDeliveryItem(n.Id, n.Title, n.Status, n.ReferenceId,
            n.Status == "failed" && !db.AuditLogs.Any(a => a.Action == "email_retry" && a.EntityId == n.Id))).ToListAsync(ct);
    public async Task<int> RetryAsync(long userId, long id, CancellationToken ct)
    {
        await using var tx = db.Database.CurrentTransaction is null ? await db.Database.BeginTransactionAsync(ct) : null;
        var item = await db.Notifications.FromSqlInterpolated($"SELECT * FROM notifications WITH (UPDLOCK, ROWLOCK) WHERE id = {id}").SingleOrDefaultAsync(ct);
        if (item is null || item.UserId != userId || item.Channel != "email") return 404;
        if (item.Status != "failed" || await db.AuditLogs.AnyAsync(a => a.Action == "email_retry" && a.EntityId == id, ct)) return 409;
        item.Status = "pending";
        db.AuditLogs.Add(new AuditLog { ActorId = userId, Action = "email_retry", EntityType = "notification", EntityId = id,
            OldValue = "failed", NewValue = $"pending; {DateTimeOffset.UtcNow:O}" });
        await db.SaveChangesAsync(ct);
        if (tx is not null) await tx.CommitAsync(ct);
        return 202;
    }
    public async Task RemoveDeviceAsync(long userId, string token, CancellationToken ct) =>
        await db.UserFcmTokens.Where(t => t.UserId == userId && t.FcmToken == token).ExecuteDeleteAsync(ct);
}
