using System;
using System.Collections.Generic;

namespace TKSC.Domain.Entities;

public partial class Policy
{
    public long Id { get; set; }

    public string Code { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string? Value { get; set; }

    public long? ApprovedBy { get; set; }

    public DateTimeOffset? EffectiveFrom { get; set; }

    public DateTimeOffset? EffectiveTo { get; set; }

    public virtual User? ApprovedByNavigation { get; set; }
}
