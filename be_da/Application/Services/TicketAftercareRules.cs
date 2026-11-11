using TKSC.Domain.Entities;
using TKSC.Application.DTOs;
namespace TKSC.Application.Services;
public static class TicketAftercareRules
{
<<<<<<< HEAD
=======
 public static string Evidence(string? text)
 {
  text = text?.Trim();
  if (text is not { Length: >= 10 and <= 1000 })
   throw new PaymentException(400,"Nhập nội dung/bằng chứng từ 10 đến 1000 ký tự.");
  return text;
 }
 public static string VenueDecision(string current, bool cancel)
 {
  if (current.Equals("completed",StringComparison.OrdinalIgnoreCase))
   throw new PaymentException(409,"Buổi đã kết thúc không thể báo dừng.");
  if (current.Equals("cancelled",StringComparison.OrdinalIgnoreCase) && !cancel)
   throw new PaymentException(409,"Buổi đã hủy không chuyển về tạm ngưng.");
  return cancel ? "cancelled" : "suspended";
 }
>>>>>>> 0d40ca35efaf07c5d4887d6f8febc66c57f5578c
 public static bool Disrupted(string status) => status.ToLowerInvariant() is "suspended" or "cancelled";
 public static bool Occupies(string status, DateTimeOffset? expires, DateTimeOffset now) =>
  status.ToLowerInvariant() is "confirmed" or "checkedin" or "paid" or "valid"
  || (status.ToLowerInvariant() is "pending" or "reserved" && expires > now);
 public static int Limit(int capacity) => Math.Min(capacity, 8);
 public static void CheckTarget(Session source, Session target, int occupied, DateTimeOffset now)
 {
  if (target.Id == source.Id || target.SportId != source.SportId) throw new PaymentException(409,"Chọn buổi khác cùng môn thể thao.");
  if (target.Status.ToLowerInvariant() is not ("open" or "published") || new DateTimeOffset(target.SessionDate.ToDateTime(target.TimeSlot.StartTime),TimeSpan.FromHours(7)) <= now)
   throw new PaymentException(409,"Buổi mới chưa mở hoặc đã bắt đầu.");
  if (occupied >= Limit(target.Capacity)) throw new PaymentException(409,"Buổi mới đã hết chỗ (tối đa 8 người).");
 }
 public static List<(long PaymentId,int Transfer,int Refund)> Split(IEnumerable<TicketFunding> funds, int targetPrice)
 {
  var left=targetPrice; var result=new List<(long,int,int)>();
  foreach(var fund in funds.OrderBy(f=>f.Id)) { var transfer=Math.Min(left,fund.AmountVnd); left-=transfer;result.Add((fund.PaymentId,transfer,fund.AmountVnd-transfer)); }
  return result;
 }
}
