using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.EntityFrameworkCore;
using MimeKit;
using TKSC.Infrastructure.DbContext;

namespace TKSC.Infrastructure.Notifications;

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
                    var db = scope.ServiceProvider.GetRequiredService<Sep490Context>();
                    var ids = await db.Notifications.AsNoTracking().Where(n => n.Channel == "email" && n.ReferenceType != null && n.ReferenceType.StartsWith("ticket_")
                        && (n.Status == "pending" || n.Status == "retry1" || n.Status == "retry2" || n.Status == "retry3" || n.Status == "retry4"))
                        .OrderBy(n => n.Id).Take(20).Select(n => n.Id).ToListAsync(stoppingToken);
                    foreach (var id in ids) await DeliverAsync(id, stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
                catch (Exception ex) { logger.LogWarning("Email queue unavailable ({ErrorType}). Will retry.", ex.GetType().Name); }
            }
            try { await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken); }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
        }
    }
    private async Task DeliverAsync(long id, CancellationToken stoppingToken)
    {
        var username = config["Email:Username"];
        var password = config["Email:Password"];
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password)) return;
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(20));
        var ct = timeout.Token;
        using var scope = scopes.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<Sep490Context>();
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        // Lock only the queue row. Concurrent workers must observe the saved sent/retry state.
        var notification = await db.Notifications.FromSqlInterpolated(
            $"SELECT * FROM notifications WITH (UPDLOCK, ROWLOCK) WHERE id = {id}").SingleOrDefaultAsync(ct);
        if (notification is null || !IsPending(notification.Status)) return;
        var user = await db.Users.AsNoTracking().SingleAsync(u => u.Id == notification.UserId, ct);
        if (user.DeletedAt is not null || !string.Equals(user.Status, "Active", StringComparison.OrdinalIgnoreCase))
            notification.Status = "suppressed";
        else
        {
            try
            {
                var ticket = await db.Tickets.AsNoTracking().Where(t => t.Id == notification.ReferenceId)
                    .Select(t => new { t.TicketCode, t.Status, t.AmountVnd, t.Session.SessionDate,
                        t.Session.TimeSlot.StartTime, t.Session.TimeSlot.EndTime, Court = t.Session.Court.Name,
                        Venue = t.Session.Court.Venue.Name, Sport = t.Session.Sport.Name, Holder = t.Player.User.FullName }).SingleOrDefaultAsync(ct);
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress("TK Sport Community", config["Email:From"] ?? username));
                message.To.Add(new MailboxAddress(user.FullName, user.Email));
                message.Subject = notification.Title ?? "Thông báo vé TK Sport";
                // Stable ID helps mail clients deduplicate retries after a process crash.
                message.MessageId = $"ticket-event-{id}@tksc.local";
                var body = $"Xin chào {user.FullName},\n\n{notification.Title}\n";
                if (ticket is not null)
                    body += $"\nNgười chơi: {ticket.Holder}\nMôn: {ticket.Sport}\nSân: {ticket.Court} — {ticket.Venue}\nNgày: {ticket.SessionDate:dd/MM/yyyy}\nGiờ: {ticket.StartTime:HH:mm} – {ticket.EndTime:HH:mm}\nGiá vé: {ticket.AmountVnd:N0} VND\nMã vé: {ticket.TicketCode}\nTrạng thái vé hiện tại: {ticket.Status}\n";
                var webUrl = config["Email:WebUrl"];
                if (Uri.TryCreate(webUrl, UriKind.Absolute, out var uri) && uri.Scheme is "https" or "http")
                    body += $"\nXem vé và QR check-in: {uri.ToString().TrimEnd('/')}/player/home?view=tickets\n";
                body += "\nVui lòng xem trạng thái mới nhất trong Vé của tôi. Email này không thay thế xác nhận hoàn tiền.\nTK Sport Community";
                message.Body = new TextPart("plain") { Text = body };
                using var smtp = new SmtpClient { Timeout = 15000 };
                var port = config.GetValue("Email:Port", 587);
                await smtp.ConnectAsync(config["Email:Host"] ?? "smtp.gmail.com", port,
                    port == 465 ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTls, ct);
                await smtp.AuthenticateAsync(username, password, ct);
                await smtp.SendAsync(message, ct);
                notification.Status = "sent";
                // A disconnect failure after SMTP accepted the message must not schedule a duplicate.
                try { await smtp.DisconnectAsync(true, ct); } catch { }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { throw; }
            catch (Exception ex)
            {
                notification.Status = NextFailure(notification.Status);
                logger.LogWarning("Ticket email {NotificationId} failed ({ErrorType}); state {State}.", id, ex.GetType().Name, notification.Status);
            }
        }
        // Save retry/sent even if the SMTP attempt timed out. Never alter the ticket transaction.
        await db.SaveChangesAsync(stoppingToken);
        await transaction.CommitAsync(stoppingToken);
    }
    public static bool IsPending(string? status) => status is "pending" or "retry1" or "retry2" or "retry3" or "retry4";
    public static string NextFailure(string? status) => status switch
    { "pending" => "retry1", "retry1" => "retry2", "retry2" => "retry3", "retry3" => "retry4", _ => "failed" };
}
