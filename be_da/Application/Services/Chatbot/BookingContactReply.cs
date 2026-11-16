using be_da.Application.DTOs;
using be_da.Application.Interfaces.External;
using be_da.Application.Interfaces.Repositories;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace be_da.Application.Services.Chatbot;
public static class BookingContactReply
{
    public static ChatAnswer? Create(string question, ChatbotOptions options, bool recognizedIntent = false)
    {
        var normalized = new string (question.ToLowerInvariant().Replace('đ', 'd').Normalize(NormalizationForm.FormD).Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark).ToArray());
        var channel = Regex.IsMatch(normalized, @"\b(zalo|messenger|mess|facebook)\b");
        var booking = Regex.IsMatch(normalized, @"\b(dat san|thue san|lien he san)\b");
        if (!recognizedIntent && !channel && (!booking || Regex.IsMatch(normalized, @"\b(chinh sach|quy dinh)\b")))
            return null;
        var links = new List<BookingLink>();
        var messenger = ValidateUrl(options.MessengerUrl, "messenger");
        var zalo = ValidateUrl(options.ZaloUrl, "zalo");
        if (messenger is not null)
            links.Add(new("Mở Messenger", messenger));
        if (zalo is not null)
            links.Add(new("Mở Zalo", zalo));
        var answer = links.Count == 0 ? "Bạn muốn nhắn sân để đặt chỗ. Hiện sân chưa thiết lập link Messenger/Zalo nên tôi chưa thể mở kênh liên hệ. Bạn có thể chuẩn bị nội dung bên dưới để gửi khi có liên hệ của sân." : "Bạn có thể nhắn sân qua kênh liên hệ bên dưới. Sao chép nội dung yêu cầu rồi mở Messenger hoặc Zalo, dán vào cuộc trò chuyện và tự gửi. Chỉ những kênh đã được sân thiết lập mới hiển thị.";
        answer += "\n\nSân cần xác nhận ngày, giờ, sân trống và giá. Mở liên kết hoặc sao chép nội dung chưa tạo đơn hay giữ chỗ trong hệ thống.";
        var draft = "Chào sân, tôi muốn được hỗ trợ đặt sân.\nYêu cầu của tôi: " + question.Trim() + "\nNhờ sân kiểm tra chỗ trống, báo giá và xác nhận giúp tôi. Tôi sẽ bổ sung môn chơi, ngày, giờ và thời lượng nếu còn thiếu.";
        return new(answer, "contact", [], BookingContact: new(draft, links));
    }

    // Only configured direct chat destinations. No URLs supplied by the model or embedded in the question.
    public static string? ValidateUrl(string? value, string channel)
    {
        if (!Uri.TryCreate(value?.Trim(), UriKind.Absolute, out var uri) || uri.Scheme != "https" || !uri.IsDefaultPort || uri.UserInfo.Length != 0 || uri.Query.Length != 0 || uri.Fragment.Length != 0)
            return null;
        var host = uri.IdnHost.ToLowerInvariant();
        if (channel == "zalo" && host == "zalo.me" && Regex.IsMatch(uri.AbsolutePath, @"^/[a-zA-Z0-9._-]+/?$"))
            return uri.AbsoluteUri;
        if (channel == "messenger" && host == "m.me" && Regex.IsMatch(uri.AbsolutePath, @"^/[a-zA-Z0-9._-]+/?$"))
            return uri.AbsoluteUri;
        if (channel == "messenger" && (host == "messenger.com" || host == "www.messenger.com") && Regex.IsMatch(uri.AbsolutePath, @"^/t/[a-zA-Z0-9._-]+/?$"))
            return uri.AbsoluteUri;
        return null;
    }
}
