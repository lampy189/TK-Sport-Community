using TKSC.Application.DTOs;
namespace TKSC.Application.Services;

public static class TicketSupportRules
{
 public static string Report(string? category, string? content)
 {
  var label = category switch {
   "payment" => "Thanh toán lỗi", "court" => "Sân hỏng", "flood" => "Sân ngập",
   "refund" => "Chậm hoàn / thiếu quỹ hoàn", "other" => "Sự cố khác", _ => null
  };
  if (label == null) throw new PaymentException(400, "Chọn loại sự cố hợp lệ.");
  content = content?.Trim();
  if (content is not { Length: >= 10 and <= 1800 })
   throw new PaymentException(400, "Mô tả sự cố từ 10 đến 1800 ký tự.");
  return $"[Báo cáo sự cố · {label}]\n{content}";
 }
}
