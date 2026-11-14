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

namespace TKSC.Infrastructure.Persistence.Repositories;
public sealed class GoodsPaymentRepository(Sep490Context db, TimeProvider clock) : IGoodsPaymentRepository
{
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
            await LockAsync("TKSC:goods-stock", ct);
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
    public Task<List<GoodsOrderListItem>> ListOrdersListAsync(CancellationToken ct) => db.Orders.AsNoTracking().OrderByDescending(o => o.Id).Take(100).Select(o => new GoodsOrderListItem { Id = o.Id, OrderCode = o.OrderCode, CustomerName = o.CustomerName, TotalAmount = o.TotalAmount, Status = o.Status, PaymentState = o.PaymentState }).ToListAsync(ct);
    public Task<Order?> LoadOrdersAsync(long id, CancellationToken ct) => db.Orders.Include(o => o.OrderItems).ThenInclude(i => i.Product).Include(o => o.Payments).SingleOrDefaultAsync(o => o.Id == id, ct);
    public Task<int> SaveAsync(CancellationToken ct) => db.SaveChangesAsync(ct);
    public void ClearTracking() => db.ChangeTracker.Clear();
    public Task<bool> TryWebhookPaymentsExistsAsync(CancellationToken ct, VerifiedBankEvent data) => db.Payments.AsNoTracking().AnyAsync(p => p.Id == data.OrderCode && p.GoodsOrderId != null && p.OrderId == null && p.Method == "payos", ct);
    public Task<bool> ApplyBankTransactionsExistsAsync(CancellationToken ct, string? externalId) => db.BankTransactions.AnyAsync(b => b.ExternalId == externalId, ct);
    public Task<Payment> ApplyPaymentsAsync(VerifiedBankEvent data, CancellationToken ct) => db.Payments.SingleAsync(p => p.Id == data.OrderCode && p.OrderId == null && p.GoodsOrderId != null, ct);
    public void AddBankTransactions(BankTransaction value) => db.BankTransactions.Add(value);
    public Task<string?> AuditValueAuditLogsAsync(long id, string action, CancellationToken ct) => db.AuditLogs.Where(a => a.EntityType == "GoodsOrder" && a.EntityId == id && a.Action == action).OrderByDescending(a => a.Id).Select(a => a.NewValue).FirstOrDefaultAsync(ct);
    public void AddAuditLogs(AuditLog value) => db.AuditLogs.Add(value);
    public Task<List<Order>> ListForBuyerOrdersListAsync(long buyerId, CancellationToken ct) => db.Orders.AsNoTracking().Include(o => o.OrderItems).ThenInclude(i => i.Product).Where(o => o.BuyerId == buyerId).OrderByDescending(o => o.Id).Take(50).ToListAsync(ct);
    public Task<bool> ConfirmReceiptStockMovementsExistsAsync(CancellationToken ct, long id) => db.StockMovements.AnyAsync(m => m.ReferenceType == "GoodsOrder" && m.ReferenceId == id && m.MovementType == "SaleOut", ct);
    public Task<long?> ConfirmReceiptStockItemsAsync(CancellationToken ct, GoodsStockLine line) => db.StockItems.Where(s => s.ProductId == line.ProductId).Select(s => (long? )s.Id).SingleOrDefaultAsync(ct);
        public Task<int> ConfirmReceiptDatabaseUpdateAsync(CancellationToken ct, GoodsStockLine line, long stockItemId, long orderId) => db.Database.ExecuteSqlInterpolatedAsync($"""
                                        UPDATE si SET quantity_on_hand = quantity_on_hand - {line.Quantity}
                                        FROM dbo.stock_items si
                                        WHERE si.id = {stockItemId}
                                            AND si.quantity_on_hand - {line.Quantity} >= (
                                                SELECT ISNULL(SUM(lines.quantity),0)
                                                FROM dbo.audit_logs a
                                                JOIN dbo.orders reserved_order ON reserved_order.id=a.entity_id
                                                CROSS APPLY OPENJSON(a.new_value,'$.lines') WITH (productId bigint '$.productId',quantity int '$.quantity') lines
                                                WHERE a.entity_type='GoodsOrder' AND a.action='goods_stock_reserved' AND lines.productId=si.product_id
                                                    AND reserved_order.id<>{orderId}
                                                    AND (LOWER(ISNULL(reserved_order.payment_state,''))='paid'
                                                             OR TRY_CONVERT(datetimeoffset,JSON_VALUE(a.new_value,'$.expiresAt'))>SYSUTCDATETIME())
                                                    AND NOT EXISTS(SELECT 1 FROM dbo.stock_movements m WHERE m.reference_type='GoodsOrder' AND m.reference_id=reserved_order.id AND m.movement_type='SaleOut')
                                            );
                                        """, ct);
    public void AddStockMovements(StockMovement value) => db.StockMovements.Add(value);
}
