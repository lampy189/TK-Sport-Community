using be_da.Infrastructure.ExternalServices;
using Microsoft.EntityFrameworkCore;
using TKSC.Infrastructure.Persistence.Contexts;

namespace TKSC.Presentation.BackgroundServices;

public sealed class TicketPushWorker(IServiceScopeFactory scopes, IConfiguration config, ILogger<TicketPushWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                if (config.GetValue<bool>("Firebase:Enabled"))
                {
                    using var scope = scopes.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<Sep490Context>();
                    var sender = scope.ServiceProvider.GetRequiredService<FirebaseNotificationService>();
                    var pending = await db.Notifications.AsNoTracking().Where(n => n.Channel == "push" && n.Status == "pending")
                        .OrderBy(n => n.Id).Take(20).ToListAsync(ct);
                    foreach (var n in pending)
                    {
                        // Commit the claim before network I/O; do not resend ambiguous deliveries.
                        if (await db.Notifications.Where(x => x.Id == n.Id && x.Status == "pending")
                            .ExecuteUpdateAsync(s => s.SetProperty(x => x.Status, "sending"), ct) != 1) continue;
                        var status = "failed";
                        try
                        {
                            status = await sender.DeliverAsync(n.UserId, "TK Sport Community", n.Title ?? "Thông báo mới",
                                new Dictionary<string,string> { ["eventId"] = n.Id.ToString(),
                                    [n.ReferenceType?.StartsWith("goods_") == true ? "goodsOrderId" : "ticketId"] = n.ReferenceId.ToString() ?? "" }, ct);
                        }
                        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
                        catch (Exception ex) { logger.LogWarning("Push event {Id} failed ({ErrorType}).", n.Id, ex.GetType().Name); }
                        await db.Notifications.Where(x => x.Id == n.Id).ExecuteUpdateAsync(s => s.SetProperty(x => x.Status, status), ct);
                    }
                }
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { break; }
            catch (Exception ex) { logger.LogWarning("Push queue unavailable ({ErrorType}).", ex.GetType().Name); }
            try { await Task.Delay(TimeSpan.FromSeconds(10), ct); }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { break; }
        }
    }
}
