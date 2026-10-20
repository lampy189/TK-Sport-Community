using System;
using System.Collections.Generic;

namespace be_da.Models;

public partial class TimeSlot
{
    public long Id { get; set; }

    public TimeOnly StartTime { get; set; }

    public TimeOnly EndTime { get; set; }

    public string? Label { get; set; }

    public virtual ICollection<Session> Sessions { get; set; } = new List<Session>();
}
