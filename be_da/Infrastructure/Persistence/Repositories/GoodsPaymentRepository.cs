using TKSC.Domain.Exceptions;
using System.Data;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using TKSC.Application.DTOs;
using TKSC.Application.Interfaces.Services;
using TKSC.Application.Interfaces.Repositories;
using TKSC.Application.Interfaces.External;
using TKSC.Domain.Entities;
using TKSC.Infrastructure.Persistence.Contexts;
using TKSC.Application.Services;
using TKSC.Application.DTOs.Persistence;
using TKSC.Application.Validators;
using Microsoft.EntityFrameworkCore.Storage;

namespace TKSC.Infrastructure.Persistence.Repositories;
public sealed class GoodsPaymentRepository(Sep490Context db, TimeProvider clock, be_da.Application.Interfaces.External.IDataAccessContext? scope = null) : IGoodsPaymentRepository
{
    // Staff and Managers work on their own center's orders; buyers and background jobs are not limited by center.
    // Row-Level Security may be disabled on a database, so the filter is applied here as well.
    private long? StaffVenue => scope?.Role?.ToLowerInvariant() is "staff" or "manager" or "referee" ? scope.VenueId : null;
    private DateTimeOffset Now => clock.GetUtcNow();

    private static TKSC.Domain.Exceptions.PaymentException Error(int status, string message) => new(status, message);
    public async Task<T> TransactionAsync<T>(Func<Task<T>> action, string? eventKey, CancellationToken ct)
    {
        db.ChangeTracker.Clear();
        try
        {
            await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
            // Same identity/lock namespace as social payments: a transfer cannot fund both purposes.
            if (eventKey != null)
                await LockAsync(eventKey, ct);
            await LockAsync("TKSC:goods-payments", ct);
            var result = await action();
            await transaction.CommitAsync(ct);
            return result;
        }
        catch (SqlException e)when (e.Number is 1205 or 51000)
        {
            throw Error(409, "Đơn đang được cập nhật. Tải lại và thử lại.");
        }
    }

    public Task<int> LockAsync(string resource, CancellationToken ct) => db.Database.ExecuteSqlInterpolatedAsync($"""
        DECLARE @result int;
        EXEC @result = sys.sp_getapplock @Resource={resource}, @LockMode='Exclusive', @LockOwner='Transaction', @LockTimeout=10000;
        IF @result < 0 THROW 51000, 'Goods payment is busy.', 1;
        """, ct);
    public Task<List<GoodsOrderListItem>> ListOrdersListAsync(CancellationToken ct) => db.Orders.AsNoTracking().Where(o => StaffVenue == null || o.VenueId == StaffVenue).OrderByDescending(o => o.Id).Take(100).Select(o => new GoodsOrderListItem { Id = o.Id, OrderCode = o.OrderCode, CustomerName = o.CustomerName, TotalAmount = o.TotalAmount, Status = o.Status, PaymentState = o.PaymentState, HandoverState = o.HandoverState, ReservationState = o.ReservationState, ExpiresAt = o.ExpiresAt }).ToListAsync(ct);
    public Task<Order?> LoadOrdersAsync(long id, CancellationToken ct)
    {
        var venue = StaffVenue;
        return db.Orders.Include(o => o.OrderItems).ThenInclude(i => i.Product).Include(o => o.Payments).SingleOrDefaultAsync(o => o.Id == id && (venue == null || o.VenueId == venue), ct);
    }
    public Task<int> SaveAsync(CancellationToken ct) => db.SaveChangesAsync(ct);
    public void ClearTracking() => db.ChangeTracker.Clear();
    public Task<bool> TryWebhookPaymentsExistsAsync(CancellationToken ct, VerifiedBankEvent data) => db.Payments.AsNoTracking().AnyAsync(p => p.Id == data.OrderCode && p.GoodsOrderId != null && p.OrderId == null && p.Method == "payos", ct);
    public Task<bool> ApplyBankTransactionsExistsAsync(CancellationToken ct, string? externalId) => db.BankTransactions.AnyAsync(b => b.ExternalId == externalId, ct);
    public Task<Payment> ApplyPaymentsAsync(VerifiedBankEvent data, CancellationToken ct) => db.Payments.SingleAsync(p => p.Id == data.OrderCode && p.OrderId == null && p.GoodsOrderId != null, ct);
    public void AddBankTransactions(BankTransaction value) => db.BankTransactions.Add(value);
    public Task<string?> AuditValueAuditLogsAsync(long id, string action, CancellationToken ct) => db.AuditLogs.Where(a => a.EntityType == "GoodsOrder" && a.EntityId == id && a.Action == action).OrderByDescending(a => a.Id).Select(a => a.NewValue).FirstOrDefaultAsync(ct);
    public void AddAuditLogs(AuditLog value) => db.AuditLogs.Add(value);
    public Task<List<Order>> ListForBuyerOrdersListAsync(long buyerId, CancellationToken ct) => db.Orders.AsNoTracking().Include(o => o.OrderItems).ThenInclude(i => i.Product).Where(o => o.BuyerId == buyerId).OrderByDescending(o => o.Id).Take(50).ToListAsync(ct);
    public Task<long?> ReserveStockAsync(long orderId, CancellationToken ct) => StockReservationSql.ReserveAsync(Connection, Transaction, orderId, ct);
    public Task ReleaseStockAsync(long orderId, CancellationToken ct) => StockReservationSql.ReleaseAsync(Connection, Transaction, orderId, ct);
    public Task<long> ConsumeStockAsync(long orderId, long actorId, bool held, CancellationToken ct) => StockReservationSql.ConsumeAsync(Connection, Transaction, orderId, actorId, held, ct);
    public Task<string?> ProductNameAsync(long productId, CancellationToken ct) => db.Products.AsNoTracking().Where(p => p.Id == productId).Select(p => p.Name).SingleOrDefaultAsync(ct);
    public Task<List<long>> DueForExpiryAsync(DateTimeOffset now, int take, CancellationToken ct) => db.Orders.AsNoTracking()
        .Where(o => o.ReservationState == GoodsReservationRules.Held && o.ExpiresAt != null && o.ExpiresAt <= now
            && (o.Status == "Cart" || o.Status == "PendingPayment") && o.PaymentState != "Paid" && o.PaymentState != "NeedsReview")
        .OrderBy(o => o.ExpiresAt).Select(o => o.Id).Take(take).ToListAsync(ct);
    private System.Data.Common.DbConnection Connection => db.Database.GetDbConnection();
    private System.Data.Common.DbTransaction? Transaction => db.Database.CurrentTransaction?.GetDbTransaction();
}
