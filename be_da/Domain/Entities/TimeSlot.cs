using System;
using System.Collections.Generic;

namespace TKSC.Domain.Entities;

public partial class TimeSlot
{
    public long Id { get; set; }

    public TimeOnly StartTime { get; set; }

    public TimeOnly EndTime { get; set; }

    public string? Label { get; set; }

    public virtual ICollection<Session> Sessions { get; set; } = new List<Session>();
}
