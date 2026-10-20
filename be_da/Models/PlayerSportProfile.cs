using System;
using System.Collections.Generic;

namespace be_da.Models;

public partial class PlayerSportProfile
{
    public long Id { get; set; }

    public long PlayerId { get; set; }

    public long SportId { get; set; }

    public short? CurrentScore { get; set; }

    public string? CurrentBand { get; set; }

    public string? RatingStatus { get; set; }

    public virtual ICollection<Assessment> Assessments { get; set; } = new List<Assessment>();

    public virtual Player Player { get; set; } = null!;

    public virtual ICollection<PlayerRatingHistory> PlayerRatingHistories { get; set; } = new List<PlayerRatingHistory>();

    public virtual ICollection<RatingAppeal> RatingAppeals { get; set; } = new List<RatingAppeal>();

    public virtual Sport Sport { get; set; } = null!;
}
