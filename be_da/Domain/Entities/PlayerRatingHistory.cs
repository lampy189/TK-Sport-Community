using System;
using System.Collections.Generic;

namespace TKSC.Domain.Entities;

public partial class PlayerRatingHistory
{
    public long Id { get; set; }

    public long PlayerSportProfileId { get; set; }

    public short? OldScore { get; set; }

    public short? NewScore { get; set; }

    public string? OldBand { get; set; }

    public string? NewBand { get; set; }

    public long ChangedBy { get; set; }

    public long? AssessmentId { get; set; }

    public virtual Assessment? Assessment { get; set; }

    public virtual User ChangedByNavigation { get; set; } = null!;

    public virtual PlayerSportProfile PlayerSportProfile { get; set; } = null!;
}
