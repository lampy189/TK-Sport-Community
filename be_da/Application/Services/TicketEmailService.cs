using TKSC.Application.DTOs;
using TKSC.Application.Interfaces.External;
using TKSC.Application.Interfaces.Repositories;
using be_da.Application.Interfaces.Services;

namespace TKSC.Application.Services;
public sealed class TicketEmailService(ITicketEmailRepository repository, ITicketEmailSender sender, IConfiguration config, ILogger<TicketEmailService> logger) : ITicketEmailService
{
    public Task<List<long>> PendingAsync(CancellationToken ct) => repository.PendingAsync(ct);
    public async Task DeliverAsync(long id, CancellationToken stoppingToken)
    {
        if (string.IsNullOrWhiteSpace(config["Email:Username"]) || string.IsNullOrWhiteSpace(config["Email:Password"]))
            return;
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(20));
        var ct = timeout.Token;
        await repository.WithLockedAsync(id, async notification =>
        {
            if (notification is null || !IsPending(notification.Status))
                return false;
            var user = await repository.UserAsync(notification.UserId, ct);
            if (user.DeletedAt is not null || !string.Equals(user.Status, "Active", StringComparison.OrdinalIgnoreCase))
                notification.Status = "suppressed";
            else
            {
                try
                {
                    var ticket = await repository.TicketAsync(notification.ReferenceId, ct);
                    var body = $"Xin chào {user.FullName},\n\n{notification.Title}\n";
                    if (ticket is not null)
                        body += $"\nNgười chơi: {ticket.Holder}\nMôn: {ticket.Sport}\nSân: {ticket.Court} — {ticket.Venue}\nNgày: {ticket.SessionDate:dd/MM/yyyy}\nGiờ: {ticket.StartTime:HH:mm} – {ticket.EndTime:HH:mm}\nGiá vé: {ticket.AmountVnd:N0} VND\nMã vé: {ticket.TicketCode}\nTrạng thái vé hiện tại: {ticket.Status}\n";
                    var webUrl = config["Email:WebUrl"];
                    if (Uri.TryCreate(webUrl, UriKind.Absolute, out var uri) && uri.Scheme is "https" or "http")
                        body += $"\nXem vé và QR check-in: {uri.ToString().TrimEnd('/')}/player/home?view=tickets\n";
                    body += "\nVui lòng xem trạng thái mới nhất trong Vé của tôi. Email này không thay thế xác nhận hoàn tiền.\nTK Sport Community";
                    await sender.SendAsync(new OutgoingEmail(id, user.FullName, user.Email, notification.Title ?? "Thông báo vé TK Sport", body), ct);
                    notification.Status = "sent";
                }
                catch (OperationCanceledException)when (stoppingToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    notification.Status = NextFailure(notification.Status);
                    logger.LogWarning("Ticket email {NotificationId} failed ({ErrorType}); state {State}.", id, ex.GetType().Name, notification.Status);
                }
            }

            return true;
        }, ct, stoppingToken);
    }

    public static bool IsPending(string? status) => status is "pending" or "retry1" or "retry2" or "retry3" or "retry4";
    public static string NextFailure(string? status) => status switch
    {
        "pending" => "retry1",
        "retry1" => "retry2",
        "retry2" => "retry3",
        "retry3" => "retry4",
        _ => "failed"
    };
}
