using System;
using System.Collections.Generic;

namespace be_da.Models;

public partial class ShiftReconciliation
{
    public long Id { get; set; }

    public DateOnly? ShiftDate { get; set; }

    public long StaffId { get; set; }

    public int? ExpectedCash { get; set; }

    public int? ActualCash { get; set; }

    public int? ExpectedBank { get; set; }

    public int? ActualBank { get; set; }

    public int? Difference { get; set; }

    public string? Status { get; set; }

    public long? ApprovedBy { get; set; }

    public virtual User? ApprovedByNavigation { get; set; }

    public virtual ICollection<ReconciliationItem> ReconciliationItems { get; set; } = new List<ReconciliationItem>();

    public virtual User Staff { get; set; } = null!;
}
