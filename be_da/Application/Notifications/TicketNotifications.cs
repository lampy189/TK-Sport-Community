using TKSC.Domain.Entities;
namespace TKSC.Application.Notifications;

public static class TicketNotifications
{
    public static string? Event(string before, string after, string oldOrder, string newOrder)
    {
        before = before.ToLowerInvariant(); after = after.ToLowerInvariant();
        if (before != after)
        {
            if (after == "confirmed") return "ticket_paid";
            if (after == "cancelled") return newOrder.Equals("paid", StringComparison.OrdinalIgnoreCase) ? "ticket_refund_requested" : "ticket_cancelled";
            if (after == "checkedin") return "ticket_checked_in";
            if (after == "expired") return "ticket_expired";
        }
        return !oldOrder.Equals(newOrder, StringComparison.OrdinalIgnoreCase) && newOrder.Equals("review_required", StringComparison.OrdinalIgnoreCase)
            ? "ticket_payment_review" : null;
    }
    public static string Title(string kind, long ticketId) => kind switch
    {
        "ticket_reserved" => $"Đã giữ chỗ vé #{ticketId}. Vui lòng thanh toán trong thời hạn giữ chỗ.",
        "ticket_paid" => $"Thanh toán thành công. Vé #{ticketId} đã được xác nhận, QR vào sân đã sẵn sàng.",
        "ticket_refund_requested" => $"Đã hủy vé #{ticketId}. Yêu cầu hoàn tiền đang chờ nhân viên xử lý; tiền chưa được hoàn ngay.",
        "ticket_cancelled" => $"Đã hủy vé #{ticketId} và trả lại chỗ.",
        "ticket_checked_in" => $"Vé #{ticketId} đã check-in. Chúc bạn có buổi chơi vui vẻ!",
        "ticket_expired" => $"Vé #{ticketId} đã hết thời gian giữ chỗ.",
        _ => $"Thanh toán của vé #{ticketId} đang được kiểm tra. Vui lòng không thanh toán lại."
    };
    public static IEnumerable<Notification> Create(string kind, long ticketId, params long[] recipients) =>
        recipients.Distinct().SelectMany(userId => new[] { "web", "email" }.Select(channel => new Notification
        {
            UserId = userId, Channel = channel, ReferenceType = kind, ReferenceId = ticketId,
            Title = Title(kind, ticketId), Status = channel == "web" ? "unread" : "pending"
        }));
}
