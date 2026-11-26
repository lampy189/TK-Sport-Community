using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TKSC.Application.DTOs;
using TKSC.Application.Interfaces.Repositories;
using TKSC.Application.Services;
using TKSC.Domain.Assessments;
using TKSC.Domain.Entities;
using TKSC.Domain.Exceptions;
using TKSC.Infrastructure.Persistence.Contexts;
using TKSC.Infrastructure.Persistence.Repositories;
using Xunit;
using Xunit.Abstractions;

namespace TKSC.Api.Tests.UnitTests.SkillReview;

public sealed class RatingAppealSequenceTests(ITestOutputHelper output)
{
    [Fact]
    public async Task UTC018_CreatesPendingAndNotifies()
    {
        output.WriteLine("V03 RequestAppeal | Expected: Return appeal ID; persist Pending, trimmed reason, one request audit and one call to notify assigned referee");
        await using var fixture = await Fixture.Create(output);
        var id = await fixture.Request("  Check backhand score  ");
        var row = Assert.Single(fixture.Db.RatingAppeals);
        Assert.Equal(id, row.Id);
        Assert.Equal("Pending", row.Status);
        Assert.Equal(2, row.SubmittedBy);
        var notice = Assert.Single(fixture.Notices);
        Assert.Equal((1L, (long?)10, id, "referee_rating_appeal"), notice);
        using var audit = JsonDocument.Parse(Assert.Single(fixture.Db.AuditLogs).NewValue!);
        Assert.Equal("Check backhand score", audit.RootElement.GetProperty("Reason").GetString());
    }

    [Fact]
    public async Task UTC019_ReusesActiveAppeal()
    {
        output.WriteLine("V03 RequestAppeal | Expected: Same appeal ID; one appeal, one audit and one notification call");
        await using var fixture = await Fixture.Create(output);
        var first = await fixture.Request("First appeal");
        var second = await fixture.Request("Repeated submission");
        Assert.Equal(first, second);
        Assert.Single(fixture.Db.RatingAppeals);
        Assert.Single(fixture.Db.AuditLogs);
        Assert.Single(fixture.Notices);
    }

    [Fact]
    public async Task UTC020_RejectsAnotherPlayersResult()
    {
        output.WriteLine("V03 RequestAppeal | Expected: HTTP 404; no appeal, audit or notification");
        await using var fixture = await Fixture.Create(output);
        await fixture.Reject(404, () => fixture.Request(actor: 99));
        fixture.AssertNoAppeal();
    }

    [Fact]
    public async Task UTC021_RejectsDraftAssessment()
    {
        output.WriteLine("V03 RequestAppeal | Expected: HTTP 404; no appeal, audit or notification");
        await using var fixture = await Fixture.Create(output);
        fixture.Assessment.Status = "draft";
        await fixture.Db.SaveChangesAsync();
        await fixture.Reject(404, () => fixture.Request());
        fixture.AssertNoAppeal();
    }

    [Fact]
    public async Task UTC022_RejectsBlankReason()
    {
        output.WriteLine("V03 RequestAppeal | Expected: HTTP 400; no appeal, audit or notification");
        await using var fixture = await Fixture.Create(output);
        await fixture.Reject(400, () => fixture.Request("   "));
        fixture.AssertNoAppeal();
    }

    [Fact]
    public async Task UTC023_SavesAssignedRefereeReview()
    {
        output.WriteLine("V03 ReviewAppeal | Expected: Return appeal ID; status UnderReview; one referee_review audit with trimmed reason");
        await using var fixture = await Fixture.Create(output);
        var id = await fixture.Request();
        Assert.Equal(id, await fixture.Review(id, "  Verified observation  "));
        Assert.Equal("UnderReview", fixture.Appeal.Status);
        using var audit = JsonDocument.Parse(Assert.Single(fixture.Db.AuditLogs.Where(log => log.Action == "referee_review")).NewValue!);
        Assert.Equal("Verified observation", audit.RootElement.GetProperty("Reason").GetString());
    }

    [Fact]
    public async Task UTC024_RejectsUnassignedReferee()
    {
        output.WriteLine("V03 ReviewAppeal | Expected: HTTP 404; appeal stays Pending; no referee_review");
        await using var fixture = await Fixture.Create(output);
        var id = await fixture.Request();
        await fixture.Reject(404, () => fixture.Review(id, actor: 99));
        Assert.Equal("Pending", fixture.Appeal.Status);
        Assert.DoesNotContain(fixture.Db.AuditLogs, log => log.Action == "referee_review");
    }

    [Fact]
    public async Task UTC025_RejectsSelfReview()
    {
        output.WriteLine("V03 ReviewAppeal | Expected: HTTP 403; appeal stays Pending");
        await using var fixture = await Fixture.Create(output);
        var id = await fixture.Request();
        fixture.Assessment.Session!.SessionReferees.Add(new SessionReferee { RefereeId = 2 });
        await fixture.Db.SaveChangesAsync();
        await fixture.Reject(403, () => fixture.Review(id, actor: 2));
        Assert.Equal("Pending", fixture.Appeal.Status);
    }

    [Fact]
    public async Task UTC026_RejectsClosedAppeal()
    {
        output.WriteLine("V03 ReviewAppeal | Expected: HTTP 409; appeal stays Kept; prior review audit count stays 1");
        await using var fixture = await Fixture.Create(output);
        var id = await fixture.Reviewed();
        await fixture.Decide(id);
        await fixture.Reject(409, () => fixture.Review(id));
        Assert.Equal("Kept", fixture.Appeal.Status);
        Assert.Single(fixture.Db.AuditLogs.Where(log => log.Action == "referee_review"));
    }

    [Fact]
    public async Task UTC027_KeepsRating()
    {
        output.WriteLine("V03 DecideAppeal | Expected: Kept; rating 65/B4 and original history preserved; reviewer 30 and decision audit saved");
        await using var fixture = await Fixture.Create(output);
        var id = await fixture.Reviewed();
        Assert.Equal("Kept", await fixture.Decide(id));
        fixture.AssertDecision("Kept", 65, "B4", 1);
    }

    [Fact]
    public async Task UTC028_CorrectsRating()
    {
        output.WriteLine("V03 DecideAppeal | Expected: Corrected; profile 85/B5; one correction history; reviewer 30 and decision audit saved");
        await using var fixture = await Fixture.Create(output);
        var id = await fixture.Reviewed();
        Assert.Equal("Corrected", await fixture.Decide(id, true, 85));
        fixture.AssertDecision("Corrected", 85, "B5", 2);
        var correction = fixture.Db.PlayerRatingHistories.OrderBy(x => x.Id).Last();
        Assert.Equal((short)65, correction.OldScore);
        Assert.Equal((short)85, correction.NewScore);
        Assert.Equal(1, correction.AssessmentId);
    }

    [Fact]
    public async Task UTC029_RejectsStaleHistory()
    {
        output.WriteLine("V03 DecideAppeal | Expected: HTTP 409; appeal stays UnderReview; score, history and audit unchanged");
        await using var fixture = await Fixture.Create(output);
        var id = await fixture.Reviewed();
        await fixture.Reject(409, () => fixture.Decide(id, true, 85, 999));
        fixture.AssertUndecided("UnderReview");
    }

    [Fact]
    public async Task UTC030_RejectsBeforeReview()
    {
        output.WriteLine("V03 DecideAppeal | Expected: HTTP 409; appeal stays Pending; no new history or decision audit");
        await using var fixture = await Fixture.Create(output);
        var id = await fixture.Request();
        await fixture.Reject(409, () => fixture.Decide(id));
        fixture.AssertUndecided("Pending");
    }

    [Fact]
    public async Task UTC031_RejectsMissingReviewAudit()
    {
        output.WriteLine("V03 DecideAppeal | Expected: HTTP 409; no decision, rating or history change");
        await using var fixture = await Fixture.Create(output);
        var id = await fixture.Request();
        fixture.Appeal.Status = "UnderReview";
        await fixture.Db.SaveChangesAsync();
        await fixture.Reject(409, () => fixture.Decide(id));
        fixture.AssertUndecided("UnderReview");
    }

    [Fact]
    public async Task UTC032_RejectsOriginalAssessor()
    {
        output.WriteLine("V03 DecideAppeal | Expected: HTTP 403; no decision, rating or history change");
        await using var fixture = await Fixture.Create(output);
        var id = await fixture.Reviewed();
        await fixture.Reject(403, () => fixture.Decide(id, actor: 10));
        fixture.AssertUndecided("UnderReview");
    }

    [Fact]
    public async Task UTC033_RejectsInvalidCorrection()
    {
        output.WriteLine("V03 DecideAppeal | Expected: HTTP 400; no decision, rating or history change");
        await using var fixture = await Fixture.Create(output);
        var id = await fixture.Reviewed();
        await fixture.Reject(400, () => fixture.Decide(id, true, 101));
        fixture.AssertUndecided("UnderReview");
    }

    [Fact]
    public async Task UTC034_RejectsDecisionReplay()
    {
        output.WriteLine("V03 DecideAppeal | Expected: HTTP 409; one decision audit and one correction history");
        await using var fixture = await Fixture.Create(output);
        var id = await fixture.Reviewed();
        await fixture.Decide(id, true, 85);
        await fixture.Reject(409, () => fixture.Decide(id, true, 85));
        fixture.AssertDecision("Corrected", 85, "B5", 2);
    }

    private sealed class Fixture : IAsyncDisposable
    {
        private readonly ITestOutputHelper output;
        private readonly SkillReviewService service;
        public Sep490Context Db { get; }
        public Assessment Assessment { get; }
        public PlayerSportProfile Profile => Assessment.PlayerSportProfile;
        public RatingAppeal Appeal => Db.RatingAppeals.Single();
        public List<(long Session, long? Referee, long Appeal, string Type)> Notices { get; } = [];

        private Fixture(ITestOutputHelper output)
        {
            this.output = output;
            Db = new Sep490Context(new DbContextOptionsBuilder<Sep490Context>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
            var repository = TransactionlessRepository<ISkillReviewRepository>.Create(
                new SkillReviewRepository(Db, TimeProvider.System), Db, (method, args) =>
                {
                    if (method.Name != nameof(ISkillReviewRepository.NotifyAssignedRefereesAsync))
                        return null;
                    Notices.Add(((long)args![0]!, (long?)args[1], (long)args[2]!, (string)args[3]!));
                    return Task.CompletedTask;
                });
            service = new SkillReviewService(repository, TimeProvider.System);
            var sport = new Sport { Id = 1, Code = "badminton", Name = "Badminton" };
            var player = new Player
            {
                Id = 2, User = new User { Id = 2, Email = "player@test.invalid", FullName = "Player", Status = "Active", RoleId = 1 }
            };
            Assessment = new Assessment
            {
                Id = 1, AssessorId = 10, Status = "published", TotalScore = 65, BandResult = "B4",
                AssessmentType = "on_court",
                PlayerSportProfile = new PlayerSportProfile
                {
                    Id = 1, Player = player, Sport = sport, CurrentScore = 65, CurrentBand = "B4", RatingStatus = "Rated"
                },
                Rubric = new Rubric
                {
                    Id = 1, Sport = sport, IsActive = true, Version = BadmintonScoring.RubricVersion, Name = "Badminton pilot",
                    RubricDimensions = Enumerable.Range(1, 5).Select(i => new RubricDimension
                    {
                        Id = i, Code = $"D{i}", Name = $"Criterion {i}", WeightPercent = 20
                    }).ToList()
                },
                Session = new Session
                {
                    Id = 1, Sport = sport, Status = "Open", SessionDate = new(2026, 11, 17),
                    TimeSlot = new TimeSlot { Id = 1, StartTime = new(18, 0), EndTime = new(20, 0) },
                    Court = new Court { Id = 1, Name = "Court", Venue = new Venue { Id = 1, Name = "Venue" } },
                    SessionReferees = new List<SessionReferee> { new() { RefereeId = 10 } }
                }
            };
        }

        public static async Task<Fixture> Create(ITestOutputHelper output)
        {
            var fixture = new Fixture(output);
            fixture.Db.Add(fixture.Assessment);
            fixture.Db.PlayerRatingHistories.Add(new PlayerRatingHistory
            {
                Id = 1, AssessmentId = 1, PlayerSportProfileId = 1, ChangedBy = 10,
                OldScore = 50, OldBand = "B3", NewScore = 65, NewBand = "B4"
            });
            await fixture.Db.SaveChangesAsync();
            return fixture;
        }

        public Task<long> Request(string reason = "Check score", long actor = 2) =>
            ReadId("RequestAppeal", new { assessmentId = 1, actor, reason, Assessment.Status },
                () => service.RequestAppeal(1, actor, new ReviewInput(reason), default));

        public Task<long> Review(long id, string reason = "Observed score verified", long actor = 10) =>
            ReadId("ReviewAppeal", new { appealId = id, actor, reason, Appeal.Status },
                () => service.ReviewAppeal(id, actor, new ReviewInput(reason), default));

        public async Task<long> Reviewed()
        {
            var id = await Request();
            await Review(id);
            return id;
        }

        public async Task<string?> Decide(long id, bool correct = false, short? score = null,
            long expectedHistoryId = 1, long actor = 30)
        {
            const string reason = "Decision recorded";
            var result = await Trace("DecideAppeal",
                new { appealId = id, actor, reason, correct, score, expectedHistoryId, Appeal.Status },
                () => service.DecideAppeal(id, actor, new AppealDecision(reason, correct, score, expectedHistoryId), default));
            return result.GetProperty("Status").GetString();
        }

        private async Task<long> ReadId(string method, object input, Func<Task<object>> action)
        {
            var result = await Trace(method, input, action);
            return result.EnumerateObject().Single().Value.GetInt64();
        }

        private async Task<JsonElement> Trace(string method, object input, Func<Task<object>> action)
        {
            output.WriteLine($"Input ({method}): {JsonSerializer.Serialize(input)}");
            var result = JsonSerializer.SerializeToElement(await action());
            output.WriteLine($"Actual ({method}): {result}");
            return result;
        }

        public async Task Reject(int status, Func<Task> action)
        {
            var error = await Record.ExceptionAsync(action);
            output.WriteLine(error is AssessmentException result
                ? $"Actual: HTTP {result.StatusCode} {result.Message}"
                : $"Actual: {error?.ToString() ?? "No exception"}");
            Assert.Equal(status, Assert.IsType<AssessmentException>(error).StatusCode);
        }

        public void AssertNoAppeal()
        {
            Assert.Empty(Db.RatingAppeals);
            Assert.Empty(Db.AuditLogs);
            Assert.Empty(Notices);
        }

        public void AssertDecision(string status, short score, string band, int historyCount)
        {
            Assert.Equal(status, Appeal.Status);
            Assert.Equal(30, Appeal.ReviewedBy);
            Assert.Equal(score, Profile.CurrentScore);
            Assert.Equal(band, Profile.CurrentBand);
            Assert.Equal(historyCount, Db.PlayerRatingHistories.Count());
            Assert.Single(Db.AuditLogs.Where(log => log.Action == "appeal_decided"));
        }

        public void AssertUndecided(string status)
        {
            Assert.Equal(status, Appeal.Status);
            Assert.Null(Appeal.ReviewedBy);
            Assert.Equal((short)65, Profile.CurrentScore);
            Assert.Equal("B4", Profile.CurrentBand);
            Assert.Single(Db.PlayerRatingHistories);
            Assert.DoesNotContain(Db.AuditLogs, log => log.Action == "appeal_decided");
        }

        public async ValueTask DisposeAsync()
        {
            try
            {
                output.WriteLine($"Actual stored state: {JsonSerializer.Serialize(new
                {
                    appeals = Db.RatingAppeals.Select(a => new { a.Id, a.Status, a.ReviewedBy }).ToArray(),
                    Profile.CurrentScore, Profile.CurrentBand,
                    history = Db.PlayerRatingHistories.Select(h => new { h.Id, h.OldScore, h.NewScore, h.NewBand }).ToArray(),
                    audit = Db.AuditLogs.Select(a => a.Action).ToArray(),
                    notificationCalls = Notices.Count
                })}");
            }
            finally
            {
                await Db.DisposeAsync();
            }
        }
    }
}
