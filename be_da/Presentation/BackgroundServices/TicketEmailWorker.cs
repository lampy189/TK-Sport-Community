using be_da.Application.Interfaces.Services;

namespace TKSC.Presentation.BackgroundServices;
public sealed class TicketEmailWorker(IServiceScopeFactory scopes, IConfiguration config, ILogger<TicketEmailWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            if (config.GetValue<bool>("Email:Enabled") && !string.IsNullOrWhiteSpace(config["Email:Password"]))
            {
                try
                {
                    using var scope = scopes.CreateScope();
                    var ids = await scope.ServiceProvider.GetRequiredService<ITicketEmailService>().PendingAsync(stoppingToken);
                    foreach (var id in ids)
                        await DeliverAsync(id, stoppingToken);
                }
                catch (OperationCanceledException)when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    logger.LogWarning("Email queue unavailable ({ErrorType}). Will retry.", ex.GetType().Name);
                }
            }

            try
            {
                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
            }
            catch (OperationCanceledException)when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }

    private async Task DeliverAsync(long id, CancellationToken ct)
    {
        using var scope = scopes.CreateScope();
        await scope.ServiceProvider.GetRequiredService<ITicketEmailService>().DeliverAsync(id, ct);
    }

    public static bool IsPending(string? status) => TKSC.Application.Services.TicketEmailService.IsPending(status);
    public static string NextFailure(string? status) => TKSC.Application.Services.TicketEmailService.NextFailure(status);
}
