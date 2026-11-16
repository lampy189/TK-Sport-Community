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
    // Stock holds (upgrade 017), run inside the current transaction.
    // Reserve: null when every line is held, otherwise the product that is short (nothing held).
    Task<long?> ReserveStockAsync(long orderId, CancellationToken ct);
    Task ReleaseStockAsync(long orderId, CancellationToken ct);
    // Posts the sale once: 0 posted now, -1 already posted, otherwise the product without enough stock.
    Task<long> ConsumeStockAsync(long orderId, long actorId, bool held, CancellationToken ct);
    Task<string?> ProductNameAsync(long productId, CancellationToken ct);
    Task<List<long>> DueForExpiryAsync(DateTimeOffset now, int take, CancellationToken ct);
}
