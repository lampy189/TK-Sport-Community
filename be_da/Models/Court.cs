using System;
using System.Collections.Generic;

namespace be_da.Models;

public partial class Court
{
    public long Id { get; set; }

    public long VenueId { get; set; }

    public string? Name { get; set; }

    public long? SportId { get; set; }

    public bool? IsActive { get; set; }

    public virtual ICollection<Session> Sessions { get; set; } = new List<Session>();

    public virtual Sport? Sport { get; set; }

    public virtual Venue Venue { get; set; } = null!;
}
