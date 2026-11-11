
namespace TKSC.Domain.Entities;
public partial class Ticket
{
    // Loaded only for LATE- tickets; ordinary payments do not query the optional upgrade tables.
    public bool LateEntryEligible { get; set; }
    public DateTimeOffset? LateEntryEndsAt { get; set; }
}
