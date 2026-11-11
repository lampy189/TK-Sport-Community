using TKSC.Domain.Exceptions;
using be_da.Application.Interfaces.Services;
using TKSC.Application.Interfaces.Repositories;
using TKSC.Application.DTOs.Persistence;
using TKSC.Application.DTOs;
using TKSC.Application.Services.Notifications;
using TKSC.Domain.Entities;

namespace TKSC.Application.Services;
public sealed class TicketSupportService(ITicketSupportRepository repository, TimeProvider clock) : ITicketSupportService
{
    private async Task<Ticket> Access(long id, long actor, bool staff)
    {
        var ticket = await repository.AccessTicketsAsync(id, actor, staff);
        if (ticket == null)
            throw new PaymentException(404, "Không tìm thấy vé.");
        return ticket;
    }

    public async Task<object> Inbox()
    {
        return await repository.InboxTicketsListAsync();
    }

    public async Task<object> List(long id, long actor, bool staff)
    {
        var t = await Access(id, actor, staff);
        var messages = await repository.ListTicketSupportMessagesListAsync(id);
        return new
        {
            canProvidePayout = actor == t.Order.PayerId,
            messages
        };
    }

    public static (byte[]? Bytes, string? Type) ValidateImage(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return (null, null);
        if (value.Length > 2800000)
            throw new PaymentException(400, "Ảnh tối đa 2 MB.");
        byte[] bytes;
        try
        {
            bytes = Convert.FromBase64String(value);
        }
        catch (FormatException)
        {
            throw new PaymentException(400, "Dữ liệu ảnh không hợp lệ.");
        }

        if (bytes.Length > 2097152)
            throw new PaymentException(400, "Ảnh tối đa 2 MB.");
        if (bytes.Length >= 24 && bytes.AsSpan(0, 8).SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }) && bytes.AsSpan(12, 4).SequenceEqual("IHDR"u8))
            return (bytes, "image/png");
        if (bytes.Length >= 4 && bytes[0] == 255 && bytes[1] == 216 && bytes[2] == 255 && bytes[^2] == 255 && bytes[^1] == 217)
            return (bytes, "image/jpeg");
        throw new PaymentException(400, "Chỉ nhận ảnh PNG hoặc JPG. Không nhận SVG hoặc tệp khác.");
    }

    public Task<object> Report(long id, long actor, string? category, string? content, string? image) => Send(id, actor, false, TicketSupportRules.Report(category, content), image);
    public async Task<object> Send(long id, long actor, bool staff, string? content, string? image)
    {
        var t = await Access(id, actor, staff);
        content = content?.Trim() ?? "";
        if (content.Length > 2000 || (content.Length == 0 && string.IsNullOrEmpty(image)))
            throw new PaymentException(400, "Nhập tin nhắn tối đa 2000 ký tự hoặc chọn ảnh QR.");
        var upload = ValidateImage(image);
        var m = new TicketSupportMessage
        {
            TicketId = id,
            SenderId = actor,
            IsStaff = staff,
            Content = content,
            ImageData = upload.Bytes,
            ImageType = upload.Type,
            CreatedAt = clock.GetUtcNow()
        };
        repository.AddTicketSupportMessages(m);
        if (staff)
            repository.AddNotifications(TicketNotifications.Create("ticket_staff_reply", id, t.Order.PayerId, t.Player.UserId));
        else
        {
            // Player requests can read across centers; scope notification recipients explicitly.
            var venueId = await repository.SendSessionsAsync(t);
            var recipients = await repository.SendUsersListAsync(venueId);
            foreach (var recipient in recipients)
                repository.AddNotifications(new Notification { UserId = recipient, Channel = "web", ReferenceType = "ticket_player_message", ReferenceId = id, Title = $"Vé #{id} có thông tin mới từ người chơi. Mở Sự cố để xem và phản hồi.", Status = "unread" });
        }

        await repository.SaveAsync();
        return new
        {
            m.Id
        };
    }

    public async Task<(byte[] Bytes, string Type)> Image(long id, long messageId, long actor, bool staff)
    {
        await Access(id, actor, staff);
        var m = await repository.ImageTicketSupportMessagesAsync(id, messageId);
        if (m?.ImageData == null)
            throw new PaymentException(404, "Không tìm thấy ảnh.");
        return (m.ImageData, m.ImageType!);
    }
}
