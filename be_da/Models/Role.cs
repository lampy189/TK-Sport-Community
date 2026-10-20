using System;
using System.Collections.Generic;

namespace be_da.Models;

public partial class Role
{
    public long Id { get; set; }

    public string Code { get; set; } = null!;

    public string Name { get; set; } = null!;

    public virtual ICollection<User> Users { get; set; } = new List<User>();
}
