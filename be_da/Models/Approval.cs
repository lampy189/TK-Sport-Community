using System;
using System.Collections.Generic;

namespace be_da.Models;

public partial class Approval
{
    public long Id { get; set; }

    public string? EntityType { get; set; }

    public long? EntityId { get; set; }

    public long RequestedBy { get; set; }

    public long? ApprovedBy { get; set; }

    public string? Status { get; set; }

    public virtual User? ApprovedByNavigation { get; set; }

    public virtual User RequestedByNavigation { get; set; } = null!;
}
