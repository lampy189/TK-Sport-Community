using TKSC.Domain.Entities;
using TKSC.Application.DTOs;
namespace TKSC.Application.Interfaces.Repositories;
public interface IPaymentRecoveryRepository
{
    Task<IReadOnlyList<PaymentRecoveryItem>> ListAsync(CancellationToken ct);
    Task<Payment?> FindAsync(long id, CancellationToken ct);
    Task RecordAsync(long id, long actor, string action, string evidence, object result, CancellationToken ct);
    Task CloseUnpaidAsync(long id, long actor, string evidence, GatewayStatus remote, CancellationToken ct);
}
