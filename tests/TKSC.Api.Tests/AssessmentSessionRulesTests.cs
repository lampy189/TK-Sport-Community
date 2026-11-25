using TKSC.Domain.Assessments;
using TKSC.Domain.Entities;
using Xunit;

namespace TKSC.Api.Tests;

public class AssessmentSessionRulesTests
{
    [Theory]
    [InlineData(8, 59, false)]
    [InlineData(9, 0, true)]
    [InlineData(10, 59, true)]
    [InlineData(11, 0, true)]
    public void UsesVietnamTimeAndEnforcesThreeDaysAfterEnd(int hour, int minute, bool expected)
    {
        var session = new Session { SessionDate = new DateOnly(2026, 11, 22), Status = "published",
            TimeSlot = new TimeSlot { StartTime = new TimeOnly(16, 0), EndTime = new TimeOnly(18, 0) } };
        var now = new DateTimeOffset(2026, 11, 22, hour, minute, 0, TimeSpan.Zero);
        Assert.Equal(expected, AssessmentSessionRules.CanAssess(session, now));
        session.SessionDate = session.SessionDate.AddDays(1);
        Assert.False(AssessmentSessionRules.CanAssess(session, now));
        session.SessionDate = session.SessionDate.AddDays(-2);
        session.Status = "Completed";
        Assert.True(AssessmentSessionRules.CanAssess(session, now));

        var deadline = AssessmentSessionRules.Deadline(session.SessionDate, session.TimeSlot.EndTime);
        Assert.Equal(new DateTimeOffset(2026, 11, 24, 18, 0, 0, TimeSpan.FromHours(7)), deadline);
        Assert.True(AssessmentSessionRules.CanAssess(session, deadline));
        Assert.False(AssessmentSessionRules.CanAssess(session, deadline.AddSeconds(1)));
    }

    [Theory]
    [InlineData("cancelled")]
    [InlineData("suspended")]
    [InlineData("Suspended")]
    public void UnavailableSessionsCannotBeScored(string status)
    {
        var session = new Session { SessionDate = new DateOnly(2026, 11, 22), Status = status,
            TimeSlot = new TimeSlot { StartTime = new TimeOnly(16, 0), EndTime = new TimeOnly(18, 0) } };
        Assert.False(AssessmentSessionRules.CanAssess(session, new DateTimeOffset(2026, 11, 23, 10, 0, 0, TimeSpan.Zero)));
    }
}
