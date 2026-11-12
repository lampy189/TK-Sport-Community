using Microsoft.EntityFrameworkCore;
using TKSC.Application.DTOs;
using TKSC.Application.Interfaces.Repositories;
using TKSC.Domain.Entities;
using TKSC.Infrastructure.Persistence.Contexts;

namespace TKSC.Infrastructure.Persistence.Repositories;
public sealed class TicketEmailRepository(Sep490Context db) : ITicketEmailRepository
{
    public async Task<List<long>> PendingAsync(CancellationToken stoppingToken) => await db.Notifications.AsNoTracking().Where(n => n.Channel == "email" && n.ReferenceType != null && n.ReferenceType.StartsWith("ticket_") && (n.Status == "pending" || n.Status == "retry1" || n.Status == "retry2" || n.Status == "retry3" || n.Status == "retry4")).OrderBy(n => n.Id).Take(20).Select(n => n.Id).ToListAsync(stoppingToken);
    public async Task WithLockedAsync(long id, Func<Notification?, Task<bool>> action, CancellationToken ct, CancellationToken saveToken)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var notification = await db.Notifications.FromSqlInterpolated($"SELECT * FROM notifications WITH (UPDLOCK, ROWLOCK) WHERE id = {id}").SingleOrDefaultAsync(ct);
        if (!await action(notification))
            return;
        await db.SaveChangesAsync(saveToken);
        await tx.CommitAsync(saveToken);
    }

    public Task<User> UserAsync(long id, CancellationToken ct) => db.Users.AsNoTracking().SingleAsync(u => u.Id == id, ct);
    public async Task<TicketEmailDetails?> TicketAsync(long? id, CancellationToken ct) => await db.Tickets.AsNoTracking().Where(t => t.Id == id).Select(t => new TicketEmailDetails { TicketCode = t.TicketCode, Status = t.Status, AmountVnd = t.AmountVnd, SessionDate = t.Session.SessionDate, StartTime = t.Session.TimeSlot.StartTime, EndTime = t.Session.TimeSlot.EndTime, Court = t.Session.Court.Name, Venue = t.Session.Court.Venue.Name, Sport = t.Session.Sport.Name, Holder = t.Player.User.FullName }).SingleOrDefaultAsync(ct);
}
