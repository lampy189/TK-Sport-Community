using System.ComponentModel.DataAnnotations.Schema;
namespace TKSC.Domain.Entities;
[Table("ticket_support_messages")]
public class TicketSupportMessage
{
 [Column("id")] public long Id { get; set; }
 [Column("ticket_id")] public long TicketId { get; set; }
 [Column("sender_id")] public long SenderId { get; set; }
 [Column("is_staff")] public bool IsStaff { get; set; }
 [Column("content")] public string Content { get; set; } = "";
 [Column("image_data")] public byte[]? ImageData { get; set; }
 [Column("image_type")] public string? ImageType { get; set; }
 [Column("created_at")] public DateTimeOffset CreatedAt { get; set; }
}
