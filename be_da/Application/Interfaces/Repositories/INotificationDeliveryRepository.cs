using TKSC.Application.DTOs;
namespace TKSC.Application.Interfaces.Repositories;
public interface INotificationDeliveryRepository
{
    Task<List<EmailDeliveryItem>> EmailsAsync(long userId, CancellationToken ct);
    Task<int> RetryAsync(long userId, long id, CancellationToken ct);
    Task RemoveDeviceAsync(long userId, string token, CancellationToken ct);
}
