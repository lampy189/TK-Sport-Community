using System;
using System.Collections.Generic;

namespace be_da.Models;

public partial class AssessmentScore
{
    public long Id { get; set; }

    public long AssessmentId { get; set; }

    public long DimensionId { get; set; }

    public short? Score { get; set; }

    public virtual Assessment Assessment { get; set; } = null!;

    public virtual RubricDimension Dimension { get; set; } = null!;
}
