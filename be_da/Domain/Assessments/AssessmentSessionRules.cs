using System.Linq.Expressions;
using TKSC.Domain.Entities;

namespace TKSC.Domain.Assessments;

public static class AssessmentSessionRules
{
    public static Expression<Func<Session, bool>> Ongoing(DateTimeOffset now)
    {
        var local = now.ToOffset(TimeSpan.FromHours(7)).DateTime;
        var today = DateOnly.FromDateTime(local);
        var time = TimeOnly.FromDateTime(local);
        return s => s.Status != "cancelled" && s.Status != "suspended" && s.Status != "completed"
            && s.SessionDate == today && s.TimeSlot.StartTime <= time && time < s.TimeSlot.EndTime;
    }
}
