using System;
using System.Collections.Generic;

namespace be_da.Models;

public partial class UserFcmToken
{
    public long Id { get; set; }

    public long UserId { get; set; }

    public string FcmToken { get; set; } = null!;

    public string? DeviceType { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public virtual User User { get; set; } = null!;
}
