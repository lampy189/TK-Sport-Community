using TKSC.Application.DTOs;
using TKSC.Application.Interfaces.Repositories;
using be_da.Application.Interfaces.Services;
using be_da.Application.Interfaces.Repositories;
namespace TKSC.Application.Services;
public sealed class NotificationDeliveryService(INotificationDeliveryRepository repository, IFriendshipStore devices) : INotificationDeliveryService
{
    public Task<List<EmailDeliveryItem>> EmailsAsync(long userId, CancellationToken ct) => repository.EmailsAsync(userId, ct);
    public Task<int> RetryAsync(long userId, long id, CancellationToken ct) => repository.RetryAsync(userId, id, ct);
    public Task RegisterDeviceAsync(long userId, string token, CancellationToken ct) => devices.SaveFcmTokenAsync(userId, token, "web", ct);
    public Task RemoveDeviceAsync(long userId, string token, CancellationToken ct) => repository.RemoveDeviceAsync(userId, token, ct);
}
