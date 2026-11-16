using System;
using System.Collections.Generic;

namespace TKSC.Domain.Entities;

public partial class StockCount
{
    public long Id { get; set; }

    public long StockItemId { get; set; }

    public int CountedQuantity { get; set; }

    public int? SystemQuantity { get; set; }

    public int? Difference { get; set; }

    public long CountedBy { get; set; }

    public long? ApprovedBy { get; set; }

    // Upgrade 017: count -> proposed adjustment -> Manager decision -> applied once.
    public string Status { get; set; } = "Pending";

    public string? Reason { get; set; }

    public string? Evidence { get; set; }

    public string? EnteredUnit { get; set; }

    public int? EnteredQuantity { get; set; }

    public long? StockVersion { get; set; }

    public DateTimeOffset? CountedAt { get; set; }

    public DateTimeOffset? DecidedAt { get; set; }

    public string? DecisionNote { get; set; }

    public long? AppliedMovementId { get; set; }

    public virtual User? ApprovedByNavigation { get; set; }

    public virtual User CountedByNavigation { get; set; } = null!;

    public virtual StockItem StockItem { get; set; } = null!;
}
