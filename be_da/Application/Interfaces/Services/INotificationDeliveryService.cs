using TKSC.Application.DTOs;
namespace be_da.Application.Interfaces.Services;
public interface INotificationDeliveryService
{
    Task<List<EmailDeliveryItem>> EmailsAsync(long userId, CancellationToken ct);
    Task<int> RetryAsync(long userId, long id, CancellationToken ct);
    Task RegisterDeviceAsync(long userId, string token, CancellationToken ct);
    Task RemoveDeviceAsync(long userId, string token, CancellationToken ct);
}
