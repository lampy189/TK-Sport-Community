using System;
using System.Collections.Generic;

namespace TKSC.Domain.Entities;

public partial class Assessment
{
    public long Id { get; set; }

    public long PlayerSportProfileId { get; set; }

    public long? SessionId { get; set; }

    public long RubricId { get; set; }

    public string? AssessmentType { get; set; }

    public long AssessorId { get; set; }

    public string? Status { get; set; }

    public short? TotalScore { get; set; }

    public string? BandResult { get; set; }

    public virtual ICollection<AssessmentScore> AssessmentScores { get; set; } = new List<AssessmentScore>();

    public virtual User Assessor { get; set; } = null!;

    public virtual ICollection<PlayerRatingHistory> PlayerRatingHistories { get; set; } = new List<PlayerRatingHistory>();

    public virtual PlayerSportProfile PlayerSportProfile { get; set; } = null!;

    public virtual ICollection<RatingAppeal> RatingAppeals { get; set; } = new List<RatingAppeal>();

    public virtual Rubric Rubric { get; set; } = null!;

    public virtual Session? Session { get; set; }
}
