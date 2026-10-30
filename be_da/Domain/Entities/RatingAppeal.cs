using System;
using System.Collections.Generic;

namespace TKSC.Domain.Entities;

public partial class RatingAppeal
{
    public long Id { get; set; }

    public long PlayerSportProfileId { get; set; }

    public long SubmittedBy { get; set; }

    public long? AssessmentId { get; set; }

    public string? Status { get; set; }

    public long? ReviewedBy { get; set; }

    public virtual Assessment? Assessment { get; set; }

    public virtual PlayerSportProfile PlayerSportProfile { get; set; } = null!;

    public virtual User? ReviewedByNavigation { get; set; }

    public virtual User SubmittedByNavigation { get; set; } = null!;
}
