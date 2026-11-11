namespace TKSC.Application.Services;

public interface IGoodsPaymentsService
{
    Task<object> ListAsync(CancellationToken ct);
    Task<GoodsOrderView> ReadAsync(long id, CancellationToken ct);
    Task<GoodsOrderView> InstructionsAsync(long id, long actorId, CancellationToken ct);
    Task<GoodsOrderView> ConfirmTransferAsync(long id, CancellationToken ct);
    Task<GoodsOrderView> CashAsync(long id, long actorId, int receivedAmount, CancellationToken ct);
    Task<GoodsOrderView> InvoiceAsync(long id, long actorId, CancellationToken ct);
    Task<bool> TryWebhookAsync(string payload, CancellationToken ct);
    Task<IReadOnlyList<BuyerGoodsOrder>> ListForBuyerAsync(long buyerId, CancellationToken ct);
    Task<BuyerGoodsOrder> ReceiveAsBuyerAsync(long id, long buyerId, CancellationToken ct);
}
