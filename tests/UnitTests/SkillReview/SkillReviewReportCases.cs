using System.Text.Json;
using TKSC.Application.Services;
using TKSC.Domain.Entities;
using TKSC.Domain.Exceptions;
using Xunit;
using Xunit.Abstractions;

namespace TKSC.Api.Tests.UnitTests.SkillReview;

public sealed class SkillReviewReportCases(ITestOutputHelper output)
{
    [Fact]
    public void UTC001_ValidDisputeReason() => Reason("Toi muon khieu nai diem backhand vi trong tai cham sot diem");

    [Fact]
    public void UTC002_DetailedReason() => Reason("Trong tai chua ghi nhan dung cac pha smash va phong thu o tran dau cuoi cung ngay 20/11/2026. De nghi ban quan ly xem lai video ghi hinh va doi chieu bang diem tieu chi BM-CLB-V1 cua toi.");

    [Fact]
    public void UTC003_OneCharacter() => Reason("A");

    [Fact]
    public void UTC004_Exactly2000() => Reason(new string('x', 2000));

    [Fact]
    public void UTC005_TrimsWhitespace() => Reason("   Reason text with extra spaces   ");

    [Fact]
    public void UTC006_RejectsNull() => Reason(null, 400);

    [Fact]
    public void UTC007_RejectsEmpty() => Reason("", 400);

    [Fact]
    public void UTC008_RejectsWhitespace() => Reason("      ", 400);

    [Fact]
    public void UTC009_Rejects2001() => Reason(new string('x', 2001), 400);

    [Fact]
    public void UTC010_PublishedLaterToday() => Future("published", 0, 20, 0);

    [Fact]
    public void UTC011_OpenTomorrow() => Future("open", 1, 18, 0);

    [Fact]
    public void UTC012_PublishedInThreeDays() => Future("published", 3, 19, 0);

    [Fact]
    public void UTC013_RejectsStartedSession() => Future("open", 0, 17, 55, 409);

    [Fact]
    public void UTC014_RejectsSuspendedSession() => Future("suspended", 1, 18, 0, 409);

    [Fact]
    public void UTC015_Accepts75() => Score(75);

    [Fact]
    public void UTC016_Accepts60() => Score(60);

    [Fact]
    public void UTC017_Accepts100() => Score(100);

    private static string FormatReason(string? value) => value is { Length: >= 2000 }
        ? $"string('x', {value.Length})"
        : JsonSerializer.Serialize(value);

    private void Reason(string? value, int? status = null)
    {
        var expected = value?.Trim();
        Check("SkillReviewRules.Reason", $"value={FormatReason(value)}", expected ?? "null", status,
            () => SkillReviewRules.Reason(value), FormatReason);
    }

    private void Future(string status, int days, int hour, int minute, int? expectedStatus = null)
    {
        var now = new DateTimeOffset(2026, 11, 10, 18, 0, 0, TimeSpan.FromHours(7));
        var session = new Session
        {
            Status = status, SessionDate = DateOnly.FromDateTime(now.Date).AddDays(days),
            TimeSlot = new TimeSlot { StartTime = new(hour, minute) }
        };
        Check("SkillReviewRules.Future",
            $"status={status}, date={session.SessionDate:yyyy-MM-dd}, start={session.TimeSlot.StartTime:HH:mm}, now={now:O}",
            "void", expectedStatus, () => { SkillReviewRules.Future(session, now); return "void"; });
    }

    private void Score(short score) => Check("SkillReviewRules.CorrectedScore", $"score={score}",
        score.ToString(), null, () => SkillReviewRules.CorrectedScore(score).ToString());

    private void Check(string method, string input, string expected, int? status, Func<string> action, Func<string?, string>? format = null)
    {
        format ??= value => value ?? "null";
        output.WriteLine($"Method: {method}");
        output.WriteLine($"Input: {input}");
        output.WriteLine($"Expected: {(status is null ? format(expected) : $"HTTP {status}")}");
        string? actual = null;
        var error = Record.Exception(() => { actual = action(); });
        output.WriteLine(error is AssessmentException rejected
            ? $"Actual: HTTP {rejected.StatusCode} {rejected.Message}"
            : $"Actual: {error?.ToString() ?? format(actual)}");
        if (status is int expectedStatus)
            Assert.Equal(expectedStatus, Assert.IsType<AssessmentException>(error).StatusCode);
        else
        {
            Assert.Null(error);
            Assert.Equal(expected, actual);
        }
    }
}
