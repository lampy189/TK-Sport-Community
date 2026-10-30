using TKSC.Application.DTOs;
using TKSC.Domain.Entities;

namespace TKSC.Application.Interfaces;

public interface IPaymentRepository
{
    Task<OrderSummary?> GetOrderAsync(long orderId, long payerId);
    Task<IReadOnlyList<TicketSummary>> GetTicketsAsync(long userId);
    Task<IReadOnlyList<ReviewSummary>> GetReviewsAsync();
    Task<long?> FindOrderForPaymentAsync(long paymentId);
    // Serializes order changes and deduplicates bank references inside the same transaction.
    Task<T> InTransactionAsync<T>(long? orderId, string? eventKey, Func<SocialBookingOrder?, Task<T>> action);
    Task<bool> EventExistsAsync(string externalId);
    void AddPayment(Payment payment);
    void AddEvent(BankTransaction bankEvent);
    void AddAudit(AuditLog audit);
    Task<TicketCancelContext?> GetTicketCancelContextAsync(long ticketId);
    void AddRefund(Refund refund);
    Task<long?> FindOrderForTicketCodeAsync(string code);
    Task<CheckInContext?> GetCheckInContextAsync(string code, long actorId);
    Task SaveAsync();
}
