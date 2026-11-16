using TKSC.Application.DTOs;
namespace TKSC.Application.Interfaces.Services;
public interface IPaymentRecoveryService
{
    Task<IReadOnlyList<PaymentRecoveryItem>> ListAsync(CancellationToken ct);
    Task<object> ActAsync(long id,long actor,string action,PaymentRecoveryInput input,CancellationToken ct);
}
