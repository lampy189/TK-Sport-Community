using System;
using System.Collections.Generic;

namespace be_da.Models;

public partial class Sport
{
    public long Id { get; set; }

    public string Code { get; set; } = null!;

    public string Name { get; set; } = null!;

    public virtual ICollection<Court> Courts { get; set; } = new List<Court>();

    public virtual ICollection<PlayerSportProfile> PlayerSportProfiles { get; set; } = new List<PlayerSportProfile>();

    public virtual ICollection<Rubric> Rubrics { get; set; } = new List<Rubric>();

    public virtual ICollection<Session> Sessions { get; set; } = new List<Session>();
}
