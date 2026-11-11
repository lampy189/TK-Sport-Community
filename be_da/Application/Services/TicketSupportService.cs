using Microsoft.EntityFrameworkCore;
using TKSC.Application.DTOs;
using TKSC.Application.Notifications;
using TKSC.Domain.Entities;
using TKSC.Infrastructure.DbContext;
namespace TKSC.Application.Services;

public sealed class TicketSupportService(Sep490Context db, TimeProvider clock) : ITicketSupportService
{
 private async Task<Ticket> Access(long id, long actor, bool staff)
 {
  var ticket = await db.Tickets.AsNoTracking().Include(t => t.Order).Include(t => t.Player).Include(t => t.Session)
   .SingleOrDefaultAsync(t => t.Id == id && (staff || t.Order.PayerId == actor || t.Player.UserId == actor));
  if (ticket == null) throw new PaymentException(404, "Không tìm thấy vé.");
  return ticket;
 }
 public async Task<object> Inbox()
 {
  return await db.Tickets.AsNoTracking().Where(t => db.TicketSupportMessages.Any(m => m.TicketId == t.Id))
   .OrderByDescending(t => db.TicketSupportMessages.Where(m => m.TicketId == t.Id).Max(m => m.Id))
   .Select(t => new { t.Id, TicketId = t.Id, Holder = t.Player.User.FullName, t.Status,
    LastMessage = db.TicketSupportMessages.Where(m => m.TicketId == t.Id).OrderByDescending(m => m.Id).Select(m => m.Content).FirstOrDefault(),
    LastIsStaff = db.TicketSupportMessages.Where(m => m.TicketId == t.Id).OrderByDescending(m => m.Id).Select(m => m.IsStaff).FirstOrDefault(),
    LastSender = (from m in db.TicketSupportMessages join u in db.Users on m.SenderId equals u.Id where m.TicketId == t.Id && !m.IsStaff orderby m.Id descending select u.FullName).FirstOrDefault(),
    RefundStatus = db.Refunds.Where(r => r.TicketId == t.Id).OrderByDescending(r => r.Id).Select(r => r.Status).FirstOrDefault() })
   .ToListAsync();
 }
 public async Task<object> List(long id, long actor, bool staff)
 {
  var t = await Access(id, actor, staff);
  var messages = await (from m in db.TicketSupportMessages.AsNoTracking() join u in db.Users on m.SenderId equals u.Id
   where m.TicketId == id orderby m.Id select new {m.Id, m.Content, m.CreatedAt, m.IsStaff, Sender = u.FullName, HasImage = m.ImageData != null}).ToListAsync();
  return new { canProvidePayout = actor == t.Order.PayerId, messages };
 }
 public static (byte[]? Bytes, string? Type) ValidateImage(string? value)
 {
  if (string.IsNullOrEmpty(value)) return (null, null);
  if (value.Length > 2800000) throw new PaymentException(400, "Ảnh tối đa 2 MB.");
  byte[] bytes;
  try { bytes = Convert.FromBase64String(value); } catch (FormatException) { throw new PaymentException(400, "Dữ liệu ảnh không hợp lệ."); }
  if (bytes.Length > 2097152) throw new PaymentException(400, "Ảnh tối đa 2 MB.");
  if (bytes.Length >= 24 && bytes.AsSpan(0,8).SequenceEqual(new byte[]{137,80,78,71,13,10,26,10}) && bytes.AsSpan(12,4).SequenceEqual("IHDR"u8)) return (bytes, "image/png");
  if (bytes.Length >= 4 && bytes[0] == 255 && bytes[1] == 216 && bytes[2] == 255 && bytes[^2] == 255 && bytes[^1] == 217) return (bytes, "image/jpeg");
  throw new PaymentException(400, "Chỉ nhận ảnh PNG hoặc JPG. Không nhận SVG hoặc tệp khác.");
 }
 public Task<object> Report(long id, long actor, string? category, string? content, string? image)
  => Send(id, actor, false, TicketSupportRules.Report(category, content), image);
 public async Task<object> Send(long id, long actor, bool staff, string? content, string? image)
 {
  var t = await Access(id, actor, staff);
  content = content?.Trim() ?? "";
  if(content.Length > 2000 || (content.Length == 0 && string.IsNullOrEmpty(image))) throw new PaymentException(400, "Nhập tin nhắn tối đa 2000 ký tự hoặc chọn ảnh QR.");
  var upload = ValidateImage(image);
  var m = new TicketSupportMessage { TicketId = id, SenderId = actor, IsStaff = staff, Content = content, ImageData = upload.Bytes, ImageType = upload.Type, CreatedAt = clock.GetUtcNow() };
  db.TicketSupportMessages.Add(m);
  if (staff) db.Notifications.AddRange(TicketNotifications.Create("ticket_staff_reply", id, t.Order.PayerId, t.Player.UserId));
  else
  {
   // Player requests can read across centers; scope notification recipients explicitly.
   var venueId = await db.Sessions.Where(s => s.Id == t.SessionId).Select(s => s.Court.VenueId).SingleAsync();
   var recipients = await db.Users.Where(u => u.VenueId == venueId && u.DeletedAt == null && u.Status.ToLower() == "active" && (u.Role.Code.ToLower() == "staff" || u.Role.Code.ToLower() == "manager")).Select(u => u.Id).ToListAsync();
   foreach(var recipient in recipients) db.Notifications.Add(new Notification { UserId = recipient, Channel = "web", ReferenceType = "ticket_player_message", ReferenceId = id, Title = $"Vé #{id} có thông tin mới từ người chơi. Mở Sự cố để xem và phản hồi.", Status = "unread" });
  }
  await db.SaveChangesAsync(); return new { m.Id };
 }
 public async Task<(byte[] Bytes, string Type)> Image(long id, long messageId, long actor, bool staff)
 {
  await Access(id, actor, staff);
  var m = await db.TicketSupportMessages.AsNoTracking().SingleOrDefaultAsync(m => m.TicketId == id && m.Id == messageId);
  if(m?.ImageData == null) throw new PaymentException(404, "Không tìm thấy ảnh.");
  return (m.ImageData, m.ImageType!);
 }
}
