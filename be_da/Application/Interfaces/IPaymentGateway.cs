using TKSC.Application.DTOs;
namespace TKSC.Application.Interfaces;

public interface IPaymentGateway
{
    void ValidateConfiguration();
    Task<GatewayCheckout> CreateAsync(long paymentId, long orderId, int amount, DateTimeOffset expiresAt);
    Task<GatewayCheckout> CreateGoodsAsync(long paymentId, long goodsOrderId, int amount, DateTimeOffset expiresAt)
        => throw new GatewayException("Cổng thanh toán chưa hỗ trợ đơn hàng hóa.");
    Task<GatewayStatus> GetAsync(long paymentId);
    Task<VerifiedBankEvent> VerifyAsync(string payload);
}
