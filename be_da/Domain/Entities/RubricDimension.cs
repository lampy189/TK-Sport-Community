using System;
using System.Collections.Generic;

namespace TKSC.Domain.Entities;

public partial class RubricDimension
{
    public long Id { get; set; }

    public long RubricId { get; set; }

    public string Code { get; set; } = null!;

    public string Name { get; set; } = null!;

    public short? WeightPercent { get; set; }

    public virtual ICollection<AssessmentScore> AssessmentScores { get; set; } = new List<AssessmentScore>();

    public virtual Rubric Rubric { get; set; } = null!;
}
