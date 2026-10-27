using System;
using System.Collections.Generic;

namespace TKSC.Domain.Entities;

public partial class ReconciliationItem
{
    public long Id { get; set; }

    public long ReconciliationId { get; set; }

    public string? ItemType { get; set; }

    public long? ReferenceId { get; set; }

    public int? ExpectedAmount { get; set; }

    public int? ActualAmount { get; set; }

    public int? Difference { get; set; }

    public virtual ShiftReconciliation Reconciliation { get; set; } = null!;
}
