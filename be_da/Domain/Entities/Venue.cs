using System;
using System.Collections.Generic;

namespace TKSC.Domain.Entities;

public partial class Venue
{
    public long Id { get; set; }

    public string? Name { get; set; }

    public string? Address { get; set; }

    public bool? IsActive { get; set; }

    public virtual ICollection<Court> Courts { get; set; } = new List<Court>();
}
