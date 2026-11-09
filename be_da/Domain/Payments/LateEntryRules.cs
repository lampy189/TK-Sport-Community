using TKSC.Application.DTOs;
namespace TKSC.Domain.Payments;

public static class LateEntryRules
{
    public const string CodePrefix = "LATE-";
    public static bool IsLate(string? code) => code?.StartsWith(CodePrefix, StringComparison.Ordinal) == true;
    public static DateTimeOffset End(DateOnly date, TimeOnly time) => new(date.ToDateTime(time), TimeSpan.FromHours(7));
    public static bool Running(DateTimeOffset start, DateTimeOffset end, string status, DateTimeOffset now) =>
        now >= start && now < end && status.ToLowerInvariant() is "open" or "published" or "ongoing";
    public static DateTimeOffset HoldExpiry(DateTimeOffset now, DateTimeOffset end) => now.AddMinutes(5) < end ? now.AddMinutes(5) : end;
    public static void CheckSale(DateTimeOffset start, DateTimeOffset end, string status, DateTimeOffset now, int seats, int available, int price)
    {
        if (!Running(start, end, status, now)) throw new PaymentException(409, "Chỉ rao suất trong buổi đang diễn ra và chưa kết thúc.");
        if (seats < 1 || seats > available) throw new PaymentException(409, "Số suất mở bán vượt số chỗ còn trống, đã trừ vé đã trả tiền và giữ chỗ.");
        if (price is < 1000 or > 100000000) throw new PaymentException(400, "Giá vé phải từ 1.000 đến 100.000.000 VNĐ.");
    }
}
