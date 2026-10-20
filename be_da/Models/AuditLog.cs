using System;
using System.Collections.Generic;

namespace be_da.Models;

public partial class AuditLog
{
    public long Id { get; set; }

    public long? ActorId { get; set; }

    public string? Action { get; set; }

    public string? EntityType { get; set; }

    public long? EntityId { get; set; }

    public string? OldValue { get; set; }

    public string? NewValue { get; set; }

    public virtual User? Actor { get; set; }
}
