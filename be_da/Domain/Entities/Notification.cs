using System;
using System.Collections.Generic;

namespace TKSC.Domain.Entities;

public partial class Notification
{
    public long Id { get; set; }

    public long UserId { get; set; }

    public string? Channel { get; set; }

    public string? Title { get; set; }

    public string? ReferenceType { get; set; }

    public long? ReferenceId { get; set; }

    public string? Status { get; set; }

    public virtual User User { get; set; } = null!;
}
