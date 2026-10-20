using System;
using System.Collections.Generic;

namespace be_da.Models;

public partial class SessionReferee
{
    public long Id { get; set; }

    public long SessionId { get; set; }

    public long RefereeId { get; set; }

    public long AssignedBy { get; set; }

    public virtual User AssignedByNavigation { get; set; } = null!;

    public virtual User Referee { get; set; } = null!;

    public virtual Session Session { get; set; } = null!;
}
