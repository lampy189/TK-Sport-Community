using TKSC.Domain.Exceptions;
using TKSC.Application.DTOs;
using TKSC.Application.Services.Notifications;
using TKSC.Domain.Entities;
using TKSC.Application.DTOs.Persistence;

namespace TKSC.Application.Interfaces.Repositories;
public interface ITicketSupportRepository
{
    Task<Ticket?> AccessTicketsAsync(long id, long actor, bool staff);
    Task<List<TicketSupportInboxItem>> InboxTicketsListAsync();
    Task<List<TicketSupportMessageItem>> ListTicketSupportMessagesListAsync(long id);
    void AddTicketSupportMessages(TicketSupportMessage value);
    void AddNotifications(IEnumerable<Notification> value);
    Task<long> SendSessionsAsync(Ticket t);
    Task<List<long>> SendUsersListAsync(long venueId);
    void AddNotifications(Notification value);
    Task<int> SaveAsync();
    Task<TicketSupportMessage?> ImageTicketSupportMessagesAsync(long id, long messageId);
}
