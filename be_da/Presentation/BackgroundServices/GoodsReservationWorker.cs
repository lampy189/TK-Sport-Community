using be_da.Application.Interfaces.Services;
using Microsoft.Extensions.Options;
using TKSC.Application.Services;

namespace TKSC.Presentation.BackgroundServices;
// JOB-04 (RP3): releases the stock of unpaid goods orders whose hold has expired. Safe to run on several servers:
// each order is rechecked under the goods-payment lock before anything is released.
public sealed class GoodsReservationWorker(IServiceScopeFactory scopes, IOptions<GoodsOrderOptions> options, IHostEnvironment environment, ILogger<GoodsReservationWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.Value.ExpiryWorkerEnabled || environment.IsEnvironment("Testing"))
            return;
        var delay = TimeSpan.FromSeconds(Math.Clamp(options.Value.ExpirySweepSeconds, 5, 3600));
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopes.CreateScope();
                var expired = await scope.ServiceProvider.GetRequiredService<IGoodsPaymentService>().ExpireDueAsync(stoppingToken);
                if (expired > 0)
                    logger.LogInformation("Released stock holds of {Count} expired goods orders.", expired);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogWarning("Goods reservation expiry failed ({ErrorType}). Will retry.", ex.GetType().Name);
            }

            try
            {
                await Task.Delay(delay, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }
}
