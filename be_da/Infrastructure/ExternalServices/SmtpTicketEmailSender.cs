using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using TKSC.Application.DTOs;
using TKSC.Application.Interfaces.External;

namespace TKSC.Infrastructure.ExternalServices;
public sealed class SmtpTicketEmailSender(IConfiguration config) : ITicketEmailSender
{
    public async Task SendAsync(OutgoingEmail email, CancellationToken ct)
    {
        var username = config["Email:Username"]!;
        var password = config["Email:Password"]!;
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress("TK Sport Community", config["Email:From"] ?? username));
        message.To.Add(new MailboxAddress(email.RecipientName, email.RecipientEmail));
        message.Subject = email.Subject;
        message.MessageId = $"ticket-event-{email.EventId}@tksc.local";
        message.Body = new TextPart("plain")
        {
            Text = email.Body
        };
        using var smtp = new SmtpClient
        {
            Timeout = 15000
        };
        var port = config.GetValue("Email:Port", 587);
        await smtp.ConnectAsync(config["Email:Host"] ?? "smtp.gmail.com", port, port == 465 ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTls, ct);
        await smtp.AuthenticateAsync(username, password, ct);
        await smtp.SendAsync(message, ct);
        try
        {
            await smtp.DisconnectAsync(true, ct);
        }
        catch
        {
        }
    }
}
