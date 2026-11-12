using TKSC.Domain.Exceptions;
using TKSC.Application.DTOs;

namespace TKSC.Application.Interfaces.Services;
public interface IPaymentService
{
    Task<OrderSummary> DisplayAsync(long id, long userId);
    Task<IReadOnlyList<TicketSummary>> TicketsAsync(long userId);
    Task<IReadOnlyList<ReviewSummary>> ReviewsAsync();
    Task<CheckoutResult> ProcessAsync(long id, long userId);
    Task<OrderSummary> RefreshAsync(long id, long userId);
    Task<bool> FlagAsync(long id, long userId, string? reason);
    Task HandleWebhookAsync(string payload);
    Task<TicketCancelResult> CancelTicketAsync(long ticketId, long userId);
}
