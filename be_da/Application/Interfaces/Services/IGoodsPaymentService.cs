using TKSC.Domain.Exceptions;
using TKSC.Application.Interfaces.Repositories;
using TKSC.Application.DTOs.Persistence;
using System.Data;
using System.Text.Json;
using TKSC.Application.DTOs;
using TKSC.Application.Interfaces.Services;
using TKSC.Application.Interfaces.External;
using TKSC.Domain.Entities;
using TKSC.Application.Services;

namespace be_da.Application.Interfaces.Services;
public interface IGoodsPaymentService
{
    Task<object> ListAsync(CancellationToken ct);
    Task<GoodsOrderView> ReadAsync(long id, CancellationToken ct);
    Task<GoodsOrderView> CashAsync(long id, long actorId, int receivedAmount, CancellationToken ct);
    Task<GoodsOrderView> InstructionsAsync(long id, long actorId, CancellationToken ct);
    Task<GoodsOrderView> ConfirmTransferAsync(long id, CancellationToken ct);
    Task<bool> TryWebhookAsync(string payload, CancellationToken ct);
    Task<GoodsOrderView> InvoiceAsync(long id, long actorId, CancellationToken ct);
    Task<GoodsOrderView> DeliverAsync(long id, long actorId, CancellationToken ct);
    Task<GoodsOrderView> ReceiveAsync(long id, long actorId, CancellationToken ct);
    Task<BuyerGoodsOrder> ReceiveAsBuyerAsync(long id, long buyerId, CancellationToken ct);
    Task<IReadOnlyList<BuyerGoodsOrder>> ListForBuyerAsync(long buyerId, CancellationToken ct);
}
