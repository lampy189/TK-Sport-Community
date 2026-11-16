using Microsoft.EntityFrameworkCore;
using TKSC.Infrastructure.Persistence.Contexts;
using TKSC.Infrastructure.Persistence.Repositories;
using TKSC.Domain.Exceptions;

namespace TKSC.Presentation.BackgroundServices;
public sealed class GoodsHoldExpiryWorker(IServiceScopeFactory scopes, ILogger<GoodsHoldExpiryWorker> logger, IConfiguration config) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Activation is an explicit deployment step after migration approval.
        if (!config.GetValue<bool>("GoodsReservations:ExpiryWorkerEnabled")) return;
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(30));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                using var scope = scopes.CreateScope();
                var db=scope.ServiceProvider.GetRequiredService<Sep490Context>();
                await using var tx=await db.Database.BeginTransactionAsync(stoppingToken);
                await GoodsStockHolds.Sweep(db,stoppingToken);
                await tx.CommitAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { return; }
            catch (Exception ex) { logger.LogWarning(ex,"Cannot sweep expired goods holds; availability still excludes expired holds."); }
        }
    }
}
