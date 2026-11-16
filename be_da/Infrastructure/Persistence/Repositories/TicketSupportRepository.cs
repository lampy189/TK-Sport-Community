using TKSC.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;
using TKSC.Application.DTOs;
using TKSC.Application.Services.Notifications;
using TKSC.Domain.Entities;
using TKSC.Infrastructure.Persistence.Contexts;
using TKSC.Application.Services;
using TKSC.Application.DTOs.Persistence;
using TKSC.Application.Interfaces.Repositories;

namespace TKSC.Infrastructure.Persistence.Repositories;
public sealed class TicketSupportRepository(Sep490Context db, TimeProvider clock) : ITicketSupportRepository
{
    private DateTimeOffset Now => clock.GetUtcNow();

    private static TKSC.Domain.Exceptions.PaymentException Error(int status, string message) => new(status, message);
    public Task<Ticket?> AccessTicketsAsync(long id, long actor, bool staff) => db.Tickets.AsNoTracking().Include(t => t.Order).Include(t => t.Player).Include(t => t.Session).SingleOrDefaultAsync(t => t.Id == id && (staff || t.Order.PayerId == actor || t.Player.UserId == actor));
    public Task<List<TicketSupportInboxItem>> InboxTicketsListAsync() => db.Tickets.AsNoTracking().Where(t => db.TicketSupportMessages.Any(m => m.TicketId == t.Id)).OrderByDescending(t => db.TicketSupportMessages.Where(m => m.TicketId == t.Id).Max(m => m.Id)).Select(t => new TicketSupportInboxItem { Id = t.Id, TicketId = t.Id, Holder = t.Player.User.FullName, Status = t.Status, LastMessage = db.TicketSupportMessages.Where(m => m.TicketId == t.Id).OrderByDescending(m => m.Id).Select(m => m.Content).FirstOrDefault(), LastIsStaff = db.TicketSupportMessages.Where(m => m.TicketId == t.Id).OrderByDescending(m => m.Id).Select(m => m.IsStaff).FirstOrDefault(), LastSender = (
        from m in db.TicketSupportMessages
        join u in db.Users on m.SenderId equals u.Id
        where m.TicketId == t.Id && !m.IsStaff
        orderby m.Id descending
        select u.FullName).FirstOrDefault(), RefundStatus = db.Refunds.Where(r => r.TicketId == t.Id).OrderByDescending(r => r.Id).Select(r => r.Status).FirstOrDefault() }).ToListAsync();
    public Task<List<TicketSupportMessageItem>> ListTicketSupportMessagesListAsync(long id) => (
        from m in db.TicketSupportMessages.AsNoTracking()join u in db.Users on m.SenderId equals u.Id
        where m.TicketId == id
        orderby m.Id
        select new TicketSupportMessageItem
        {
            Id = m.Id,
            Content = m.Content,
            CreatedAt = m.CreatedAt,
            IsStaff = m.IsStaff,
            Sender = u.FullName,
            HasImage = m.ImageData != null
        }

    ).ToListAsync();
    public void AddTicketSupportMessages(TicketSupportMessage value) => db.TicketSupportMessages.Add(value);
    public void AddNotifications(IEnumerable<Notification> value) => db.Notifications.AddRange(value);
    public Task<long> SendSessionsAsync(Ticket t) => db.Sessions.Where(s => s.Id == t.SessionId).Select(s => s.Court.VenueId).SingleAsync();
    public Task<List<long>> SendUsersListAsync(long venueId) => db.Users.Where(u => u.VenueId == venueId && u.DeletedAt == null && u.Status.ToLower() == "active" && (u.Role.Code.ToLower() == "staff" || u.Role.Code.ToLower() == "manager")).Select(u => u.Id).ToListAsync();
    public void AddNotifications(Notification value) => db.Notifications.Add(value);
    public Task<int> SaveAsync() => db.SaveChangesAsync();
    public Task<TicketSupportMessage?> ImageTicketSupportMessagesAsync(long id, long messageId) => db.TicketSupportMessages.AsNoTracking().SingleOrDefaultAsync(m => m.TicketId == id && m.Id == messageId);
}
