using TKSC.Application.DTOs;
namespace TKSC.Application.Interfaces;

public interface IPaymentGateway
{
    void ValidateConfiguration();
    Task<GatewayCheckout> CreateAsync(long paymentId, long orderId, int amount, DateTimeOffset expiresAt);
    Task<GatewayStatus> GetAsync(long paymentId);
    Task<VerifiedBankEvent> VerifyAsync(string payload);
}
