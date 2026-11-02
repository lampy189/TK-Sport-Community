using System.ComponentModel.DataAnnotations.Schema;
namespace TKSC.Domain.Entities;
[Table("ticket_changes")]
public class TicketChange
{
 [Column("id")] public long Id { get; set; }
 [Column("ticket_id")] public long TicketId { get; set; }
 [Column("target_session_id")] public long TargetSessionId { get; set; }
 [Column("old_price_vnd")] public int OldPriceVnd { get; set; }
 [Column("new_price_vnd")] public int NewPriceVnd { get; set; }
 [Column("status")] public string Status { get; set; } = "requested";
 [Column("requested_by")] public long RequestedBy { get; set; }
 [Column("processed_by")] public long? ProcessedBy { get; set; }
 [Column("new_ticket_id")] public long? NewTicketId { get; set; }
 [Column("created_at")] public DateTimeOffset CreatedAt { get; set; }
 [Column("completed_at")] public DateTimeOffset? CompletedAt { get; set; }
}
[Table("ticket_funding")]
public class TicketFunding
{
 [Column("id")] public long Id { get; set; }
 [Column("ticket_id")] public long TicketId { get; set; }
 [Column("payment_id")] public long PaymentId { get; set; }
 [Column("amount_vnd")] public int AmountVnd { get; set; }
 [Column("retired")] public bool Retired { get; set; }
}
