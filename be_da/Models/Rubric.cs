using System;
using System.Collections.Generic;

namespace be_da.Models;

public partial class Rubric
{
    public long Id { get; set; }

    public long SportId { get; set; }

    public string Version { get; set; } = null!;

    public string Name { get; set; } = null!;

    public bool? IsActive { get; set; }

    public virtual ICollection<Assessment> Assessments { get; set; } = new List<Assessment>();

    public virtual ICollection<RubricDimension> RubricDimensions { get; set; } = new List<RubricDimension>();

    public virtual Sport Sport { get; set; } = null!;
}
