using System.Linq.Expressions;
using TKSC.Domain.Entities;

namespace TKSC.Domain.Assessments;

public static class RatingPublicationRules
{
    // A self-survey also appends history, but never makes the player officially rated.
    public static readonly Expression<Func<PlayerRatingHistory, bool>> OfficialHistory = h =>
        h.Assessment != null && h.Assessment.SessionId != null && h.Assessment.Status == "published";
}
