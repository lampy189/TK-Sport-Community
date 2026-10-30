using System;
using System.Collections.Generic;

namespace TKSC.Domain.Entities;

public partial class SessionStaff
{
    public long Id { get; set; }

    public long SessionId { get; set; }

    public long StaffId { get; set; }

    public string? RoleInSession { get; set; }

    public virtual Session Session { get; set; } = null!;

    public virtual User Staff { get; set; } = null!;
}
