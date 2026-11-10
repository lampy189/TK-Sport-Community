using System.ComponentModel.DataAnnotations.Schema;
namespace TKSC.Domain.Entities;
public partial class Ticket
{
    // Loaded only for LATE- tickets; ordinary payments do not query the optional upgrade tables.
    [NotMapped] public bool LateEntryEligible { get; set; }
    [NotMapped] public DateTimeOffset? LateEntryEndsAt { get; set; }
}
