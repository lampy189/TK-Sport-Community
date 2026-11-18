namespace TKSC.Domain.Entities;
public class TicketSupportMessage
{
 public long Id { get; set; }
 public long TicketId { get; set; }
 public long SenderId { get; set; }
 public bool IsStaff { get; set; }
 public string Content { get; set; } = "";
 public byte[]? ImageData { get; set; }
 public string? ImageType { get; set; }
 public DateTimeOffset CreatedAt { get; set; }
}
