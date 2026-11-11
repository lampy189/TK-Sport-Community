namespace TKSC.Domain.Entities;
public class TicketChange
{
 public long Id { get; set; }
 public long TicketId { get; set; }
 public long TargetSessionId { get; set; }
 public int OldPriceVnd { get; set; }
 public int NewPriceVnd { get; set; }
 public string Status { get; set; } = "requested";
 public long RequestedBy { get; set; }
 public long? ProcessedBy { get; set; }
 public long? NewTicketId { get; set; }
 public DateTimeOffset CreatedAt { get; set; }
 public DateTimeOffset? CompletedAt { get; set; }
}
public class TicketFunding
{
 public long Id { get; set; }
 public long TicketId { get; set; }
 public long PaymentId { get; set; }
 public int AmountVnd { get; set; }
 public bool Retired { get; set; }
}
