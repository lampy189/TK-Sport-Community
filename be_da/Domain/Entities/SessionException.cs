using System;
using System.Collections.Generic;

namespace TKSC.Domain.Entities;

public partial class SessionException
{
    public long Id { get; set; }

    public long SessionId { get; set; }

    public long PlayerId { get; set; }

    public long? ApprovedBy { get; set; }

    public string? Reason { get; set; }

    public virtual User? ApprovedByNavigation { get; set; }

    public virtual Player Player { get; set; } = null!;

    public virtual Session Session { get; set; } = null!;
}
