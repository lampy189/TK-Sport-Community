using System.Data;
using Microsoft.EntityFrameworkCore;
using TKSC.Application.DTOs;
using TKSC.Application.Interfaces;
using TKSC.Domain.Entities;
using TKSC.Infrastructure.DbContext;
using TKSC.Application.Mappings;

namespace TKSC.Infrastructure.Repositories;

public sealed class PaymentRepository(Sep490Context db) : IPaymentRepository
{
    public async Task<OrderSummary?> GetOrderAsync(long orderId, long payerId)
    {
        var order = await db.SocialBookingOrders.AsNoTracking().Include(o => o.Payments).Include(o => o.Tickets)
            .SingleOrDefaultAsync(o => o.Id == orderId && o.PayerId == payerId);
        return order is null ? null : PaymentMappings.ToSummary(order);
    }
    public async Task<IReadOnlyList<TicketSummary>> GetTicketsAsync(long userId) =>
        await db.Tickets.AsNoTracking().Where(t => t.Player.UserId == userId || t.Order.PayerId == userId)
            .Select(PaymentMappings.TicketProjection).ToListAsync();

    public async Task<IReadOnlyList<ReviewSummary>> GetReviewsAsync()
    {
        var payments = await db.Payments.AsNoTracking().Where(p => p.Status == "review_required")
            .OrderByDescending(p => p.Id).ToListAsync();
        var ids = payments.Select(p => p.Id).ToList();
        var audits = await db.AuditLogs.AsNoTracking().Where(a => a.EntityType == "Payment" && a.EntityId != null
            && ids.Contains(a.EntityId.Value)).ToListAsync();
        var result = payments.Select(p => new ReviewSummary(p.Id, p.OrderId, p.ReferenceCode, p.AmountVnd, p.Status,
            audits.Where(a => a.EntityId == p.Id).Select(a => new ReviewReason(a.Action, a.NewValue)).ToList())).ToList();
        var unmatched = await db.BankTransactions.AsNoTracking().Where(e => e.MatchedPaymentId == null
            && e.MatchStatus == "review_required").OrderByDescending(e => e.Id).ToListAsync();
        result.AddRange(unmatched.Select(e => new ReviewSummary(-e.Id, null, e.ReferenceCode ?? e.ExternalId,
            e.AmountVnd, "review_required", [new("unmatched_transfer", "Giao dịch đã xác thực nhưng không khớp đơn social.")])));
        return result;
    }
    public Task<long?> FindOrderForPaymentAsync(long paymentId) => db.Payments.AsNoTracking()
        .Where(p => p.Id == paymentId && p.Method == "payos").Select(p => p.OrderId).SingleOrDefaultAsync();

    public async Task<T> InTransactionAsync<T>(long? orderId, string? eventKey, Func<SocialBookingOrder?, Task<T>> action)
    {
        // Avoid stale tracked entities between the two phases of a gateway request.
        db.ChangeTracker.Clear();
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        if (eventKey is not null)
        {
            // Same bank reference must be processed once even if delivered against different orders.
            await db.Database.ExecuteSqlInterpolatedAsync($@"
                DECLARE @result int;
                EXEC @result = sp_getapplock @Resource={eventKey}, @LockMode='Exclusive',
                    @LockOwner='Transaction', @LockTimeout=10000;
                IF @result < 0 THROW 51000, 'Cannot acquire payment event lock', 1;");
        }
        SocialBookingOrder? order = null;
        if (orderId is not null)
        {
            order = await db.SocialBookingOrders.FromSqlInterpolated(
                $"SELECT * FROM social_booking_orders WITH (UPDLOCK, HOLDLOCK) WHERE id = {orderId.Value}")
                .SingleOrDefaultAsync();
            if (order is not null)
            {
                await db.Entry(order).Collection(o => o.Payments).LoadAsync();
                await db.Entry(order).Collection(o => o.Tickets).LoadAsync();
            }
        }
        var result = await action(order);
        await transaction.CommitAsync();
        return result;
    }
    public Task<bool> EventExistsAsync(string externalId) => db.BankTransactions.AnyAsync(e => e.ExternalId == externalId);
    public void AddPayment(Payment payment) => db.Payments.Add(payment);
    public void AddEvent(BankTransaction bankEvent) => db.BankTransactions.Add(bankEvent);
    public void AddAudit(AuditLog audit) => db.AuditLogs.Add(audit);
    public Task SaveAsync() => db.SaveChangesAsync();
}
