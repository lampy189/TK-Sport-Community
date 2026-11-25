using TKSC.Domain.Entities;

namespace TKSC.Domain.Assessments;

public static class AssessmentSessionRules
{
    private static readonly TimeSpan VenueOffset = TimeSpan.FromHours(7);
    private static readonly TimeSpan CompletionWindow = TimeSpan.FromDays(3);

    public static DateTimeOffset Deadline(DateOnly date, TimeOnly endTime) =>
        new DateTimeOffset(date.ToDateTime(endTime), VenueOffset).Add(CompletionWindow);

    public static bool CanAssess(Session session, DateTimeOffset now)
    {
        var startsAt = new DateTimeOffset(session.SessionDate.ToDateTime(session.TimeSlot.StartTime), VenueOffset);
        return !string.Equals(session.Status, "cancelled", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(session.Status, "suspended", StringComparison.OrdinalIgnoreCase)
            && startsAt <= now && now <= Deadline(session.SessionDate, session.TimeSlot.EndTime);
    }
}
