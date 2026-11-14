using TKSC.Domain.Exceptions;
using System.Data;
using System.Text.Json;
using TKSC.Application.DTOs;
using TKSC.Application.Interfaces.Services;
using TKSC.Application.Interfaces.Repositories;
using TKSC.Application.Interfaces.External;
using TKSC.Domain.Entities;
using TKSC.Application.DTOs.Persistence;

namespace TKSC.Application.Interfaces.Repositories;
public interface IGoodsPaymentRepository
{
    Task<T> TransactionAsync<T>(Func<Task<T>> action, string? eventKey, CancellationToken ct);
    Task<List<GoodsOrderListItem>> ListOrdersListAsync(CancellationToken ct);
    Task<Order?> LoadOrdersAsync(long id, CancellationToken ct);
    Task<int> SaveAsync(CancellationToken ct);
    void ClearTracking();
    Task<bool> TryWebhookPaymentsExistsAsync(CancellationToken ct, VerifiedBankEvent data);
    Task<bool> ApplyBankTransactionsExistsAsync(CancellationToken ct, string? externalId);
    Task<Payment> ApplyPaymentsAsync(VerifiedBankEvent data, CancellationToken ct);
    void AddBankTransactions(BankTransaction value);
    Task<string?> AuditValueAuditLogsAsync(long id, string action, CancellationToken ct);
    void AddAuditLogs(AuditLog value);
    Task<List<Order>> ListForBuyerOrdersListAsync(long buyerId, CancellationToken ct);
    Task<bool> ConfirmReceiptStockMovementsExistsAsync(CancellationToken ct, long id);
    Task<long?> ConfirmReceiptStockItemsAsync(CancellationToken ct, GoodsStockLine line);
    Task<int> ConfirmReceiptDatabaseUpdateAsync(CancellationToken ct, GoodsStockLine line, long stockItemId, long orderId);
    void AddStockMovements(StockMovement value);
}
