using TKSC.Domain.Entities;

namespace TKSC.Application.Services.Notifications;
public static class TicketNotifications
{
    public static string? Event(string before, string after, string oldOrder, string newOrder)
    {
        before = before.ToLowerInvariant();
        after = after.ToLowerInvariant();
        if (before != after)
        {
            if (after == "confirmed")
                return "ticket_paid";
            if (after == "cancelled")
                return newOrder.Equals("paid", StringComparison.OrdinalIgnoreCase) ? "ticket_refund_requested" : "ticket_cancelled";
            if (after == "checkedin")
                return "ticket_checked_in";
            if (after == "expired")
                return "ticket_expired";
        }

        return !oldOrder.Equals(newOrder, StringComparison.OrdinalIgnoreCase) && newOrder.Equals("review_required", StringComparison.OrdinalIgnoreCase) ? "ticket_payment_review" : null;
    }

    public static string Title(string kind, long ticketId) => kind switch
    {
        "ticket_staff_reply" => $"Staff đã phản hồi về vé #{ticketId}. Mở Vé của tôi để xem trao đổi.",
        "ticket_venue_issue" => $"Buổi chơi của vé #{ticketId} gặp sự cố sân. Mở Vé của tôi để chọn đổi buổi hoặc yêu cầu hoàn tiền.",
        "ticket_change_requested" => $"Đã nhận yêu cầu đổi vé #{ticketId}. Staff sẽ kiểm tra chỗ và chênh lệch giá trước khi xác nhận.",
        "ticket_changed" => $"Đổi vé thành công. Vé mới #{ticketId} và QR mới đã sẵn sàng.",
        "ticket_difference_refund" => $"Đã tạo yêu cầu hoàn chênh lệch giá của vé #{ticketId}. Tiền chưa được hoàn ngay.",
        "ticket_refund_completed" => $"Một khoản hoàn của vé #{ticketId} đã được Staff xác nhận chuyển trả. Xem vé để theo dõi các khoản còn lại.",
        "ticket_reserved" => $"Đã giữ chỗ vé #{ticketId}. Vui lòng thanh toán trong thời hạn giữ chỗ.",
        "ticket_paid" => $"Thanh toán thành công. Vé #{ticketId} đã được xác nhận, QR vào sân đã sẵn sàng.",
        "ticket_refund_requested" => $"Đã hủy vé #{ticketId}. Yêu cầu hoàn tiền đang chờ nhân viên xử lý; tiền chưa được hoàn ngay.",
        "ticket_cancelled" => $"Đã hủy vé #{ticketId} và trả lại chỗ.",
        "ticket_checked_in" => $"Vé #{ticketId} đã check-in. Chúc bạn có buổi chơi vui vẻ!",
        "ticket_expired" => $"Vé #{ticketId} đã hết thời gian giữ chỗ.",
        _ => $"Thanh toán của vé #{ticketId} đang được kiểm tra. Vui lòng không thanh toán lại."};
    public static IEnumerable<Notification> Create(string kind, long ticketId, params long[] recipients) => recipients.Distinct().SelectMany(userId => new[] { "web", "email" }.Select(channel => new Notification { UserId = userId, Channel = channel, ReferenceType = kind, ReferenceId = ticketId, Title = Title(kind, ticketId), Status = channel == "web" ? "unread" : "pending" }));
}
