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
    [InlineData(11, 0, false)]
    public void UsesVietnamTimeAndExclusiveEnd(int hour, int minute, bool expected)
    {
        var session = new Session { SessionDate = new DateOnly(2026, 11, 22), Status = "published",
            TimeSlot = new TimeSlot { StartTime = new TimeOnly(16, 0), EndTime = new TimeOnly(18, 0) } };
        var rule = AssessmentSessionRules.Ongoing(new DateTimeOffset(2026, 11, 22, hour, minute, 0, TimeSpan.Zero)).Compile();
        Assert.Equal(expected, rule(session));
        session.SessionDate = session.SessionDate.AddDays(1);
        Assert.False(rule(session));
    }

    [Theory]
    [InlineData("cancelled")]
    [InlineData("suspended")]
    [InlineData("completed")]
    public void UnavailableSessionsCannotBeScored(string status)
    {
        var session = new Session { SessionDate = new DateOnly(2026, 11, 22), Status = status,
            TimeSlot = new TimeSlot { StartTime = new TimeOnly(16, 0), EndTime = new TimeOnly(18, 0) } };
        Assert.False(AssessmentSessionRules.Ongoing(new DateTimeOffset(2026, 11, 22, 10, 0, 0, TimeSpan.Zero)).Compile()(session));
    }
}
