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

namespace TKSC.Api.Tests.UnitTests.SkillAssessment;

public sealed class AssessmentSequenceTests(ITestOutputHelper output)
{
    [Fact]
    public async Task UTC018_Open_CreatesDraft()
    {
        await using var fixture = await AssessmentFixture.Create(output, "Draft with five unobserved marks, revision and opening audit");
        var draft = await fixture.Open();
        Assert.Equal("draft", draft.Status);
        Assert.True(draft.Revision > 0);
        Assert.Equal(5, draft.Marks.Count);
        Assert.All(draft.Marks, mark => Assert.Null(mark.Score));
        Assert.Equal(BadmintonScoring.RubricVersion, draft.Rubric.Version);
        Assert.Single(fixture.Db.AuditLogs, log => log.Action == "assessment_opened");
    }

    [Fact]
    public async Task UTC019_Open_ReopensDraft()
    {
        await using var fixture = await AssessmentFixture.Create(output, "Original ID and revision; only one draft and opening audit");
        var first = await fixture.Open();
        var reopened = await fixture.Open();
        Assert.Equal(first.Id, reopened.Id);
        Assert.Equal(first.Revision, reopened.Revision);
        Assert.Single(fixture.Db.Assessments);
        Assert.Single(fixture.Db.AuditLogs);
    }

    [Fact]
    public async Task UTC020_Open_RejectsUnassignedReferee()
    {
        await using var fixture = await AssessmentFixture.Create(output, "403; no assessment or audit written");
        await Reject(403, () => fixture.Open(actor: 99));
        Assert.Empty(fixture.Db.Assessments);
        Assert.Empty(fixture.Db.AuditLogs);
    }

    [Fact]
    public async Task UTC021_Open_RejectsTicketNotCheckedIn()
    {
        await using var fixture = await AssessmentFixture.Create(output, "409; no draft");
        fixture.Ticket.Status = "confirmed";
        fixture.Ticket.CheckedInBy = null;
        await fixture.Db.SaveChangesAsync();
        await Reject(409, () => fixture.Open());
        Assert.Empty(fixture.Db.Assessments);
    }

    [Fact]
    public async Task UTC022_Open_RejectsUnpaidTicket()
    {
        await using var fixture = await AssessmentFixture.Create(output, "409; no draft");
        fixture.Ticket.Order.Status = "pending";
        await fixture.Db.SaveChangesAsync();
        await Reject(409, () => fixture.Open());
        Assert.Empty(fixture.Db.Assessments);
    }

    [Fact]
    public async Task UTC023_Open_RejectsSelfAssessment()
    {
        await using var fixture = await AssessmentFixture.Create(output, "403; no draft");
        fixture.Session.SessionReferees.Add(new SessionReferee { RefereeId = 2 });
        await fixture.Db.SaveChangesAsync();
        await Reject(403, () => fixture.Open(actor: 2));
        Assert.Empty(fixture.Db.Assessments);
    }

    [Fact]
    public async Task UTC024_Open_RejectsMissingSportProfile()
    {
        await using var fixture = await AssessmentFixture.Create(output, "409; no draft");
        await Reject(409, () => fixture.Open(player: 99));
        Assert.Empty(fixture.Db.Assessments);
    }

    [Fact]
    public async Task UTC025_Open_RejectsMissingRubric()
    {
        await using var fixture = await AssessmentFixture.Create(output, "404; no draft");
        fixture.Rubric.IsActive = false;
        await fixture.Db.SaveChangesAsync();
        await Reject(404, () => fixture.Open());
        Assert.Empty(fixture.Db.Assessments);
    }

    [Fact]
    public async Task UTC026_Open_AllowsExactDeadline()
    {
        await using var fixture = await AssessmentFixture.Create(output, "New draft created at the inclusive boundary");
        fixture.Clock.Now = new(2026, 11, 20, 13, 0, 0, TimeSpan.Zero);
        Assert.Equal("draft", (await fixture.Open()).Status);
    }

    [Fact]
    public async Task UTC027_Open_RejectsAfterDeadline()
    {
        await using var fixture = await AssessmentFixture.Create(output, "409; no draft");
        fixture.Clock.Now = new(2026, 11, 20, 13, 0, 1, TimeSpan.Zero);
        await Reject(409, () => fixture.Open());
        Assert.Empty(fixture.Db.Assessments);
    }

    [Fact]
    public async Task UTC028_Save_PersistsMarksAndAudit()
    {
        await using var fixture = await AssessmentFixture.Create(output, "Total 75 and B4; evidence persisted; draft revision advances; published profile stays at 20/B2");
        var draft = await fixture.Open();
        var marks = Marks();
        marks[0] = new MarkInput(1, 3, "Observed", [7, 8, 6, 8]);
        var saved = await fixture.Save(draft, marks);
        Assert.Equal("draft", saved.Status);
        Assert.True(saved.Revision > draft.Revision);
        Assert.Equal((short)75, saved.TotalScore);
        Assert.Equal("B4", saved.BandResult);
        Assert.Equal("Observed", saved.Marks[0].Evidence);
        Assert.Equal(new short[] { 7, 8, 6, 8 }, saved.Marks[0].SliderScores);
        Assert.Single(fixture.Db.AuditLogs, log => log.Action == "assessment_saved");
        fixture.AssertProfileUnchanged();
    }

    [Fact]
    public async Task UTC029_Save_AllowsUnobservedCriterion()
    {
        await using var fixture = await AssessmentFixture.Create(output, "Draft total and band stay null; profile and history unchanged");
        var draft = await fixture.Open();
        var marks = Marks();
        marks[0] = marks[0] with { Score = null };
        var saved = await fixture.Save(draft, marks);
        Assert.Null(saved.Marks[0].Score);
        Assert.Null(saved.TotalScore);
        Assert.Null(saved.BandResult);
        fixture.AssertProfileUnchanged();
    }

    [Fact]
    public async Task UTC030_Save_RejectsStaleRevision()
    {
        await using var fixture = await AssessmentFixture.Create(output, "409; no save audit or score changes");
        var draft = await fixture.Open();
        await Reject(409, () => fixture.Save(draft with { Revision = 0 }, Marks()));
        Assert.Single(fixture.Db.AuditLogs);
        Assert.All(fixture.Db.AssessmentScores, score => Assert.Null(score.Score));
    }

    [Fact]
    public async Task UTC031_Save_RejectsIncompleteMarks()
    {
        await using var fixture = await AssessmentFixture.Create(output, "400; marks and audit unchanged");
        var draft = await fixture.Open();
        await Reject(400, () => fixture.Save(draft, Marks()[..4]));
        Assert.Single(fixture.Db.AuditLogs);
        Assert.All(fixture.Db.AssessmentScores, score => Assert.Null(score.Score));
    }

    [Fact]
    public async Task UTC032_Save_RejectsInvalidSliders()
    {
        await using var fixture = await AssessmentFixture.Create(output, "400; no save audit");
        var draft = await fixture.Open();
        var marks = Marks();
        marks[0] = marks[0] with { SliderScores = [5, 6, 7, 11] };
        await Reject(400, () => fixture.Save(draft, marks));
        Assert.Single(fixture.Db.AuditLogs);
    }

    [Fact]
    public async Task UTC033_Save_RejectsPublishedAssessment()
    {
        await using var fixture = await AssessmentFixture.Create(output, "409; published score and publication history preserved");
        var draft = await fixture.CompleteDraft();
        var published = await fixture.Publish(draft);
        await Reject(409, () => fixture.Save(published, Marks()));
        Assert.Equal("published", fixture.Db.Assessments.Single().Status);
        Assert.Equal((short)75, fixture.Profile.CurrentScore);
        Assert.Single(fixture.Db.PlayerRatingHistories);
    }

    [Fact]
    public async Task UTC034_Publish_PublishesFirstRating()
    {
        await using var fixture = await AssessmentFixture.Create(output, "Published; profile Rated 75/B4; one history and publication audit");
        var draft = await fixture.CompleteDraft();
        var result = await fixture.Publish(draft);
        Assert.Equal("published", result.Status);
        Assert.Equal("Rated", fixture.Profile.RatingStatus);
        Assert.Equal((short)75, fixture.Profile.CurrentScore);
        Assert.Equal("B4", fixture.Profile.CurrentBand);
        var history = Assert.Single(fixture.Db.PlayerRatingHistories);
        Assert.Equal((short)20, history.OldScore);
        Assert.Equal((short)75, history.NewScore);
        Assert.Equal(draft.Id, history.AssessmentId);
        Assert.Single(fixture.Db.AuditLogs, log => log.Action == "assessment_published");
    }

    [Fact]
    public async Task UTC035_Publish_RetriesWithoutDuplicate()
    {
        await using var fixture = await AssessmentFixture.Create(output, "Published result returned; no additional history or audit");
        var draft = await fixture.CompleteDraft();
        var first = await fixture.Publish(draft);
        var retry = await fixture.Publish(draft);
        Assert.Equal(first.Id, retry.Id);
        Assert.Equal(first.Revision, retry.Revision);
        Assert.Equal("published", retry.Status);
        Assert.Single(fixture.Db.PlayerRatingHistories);
        Assert.Single(fixture.Db.AuditLogs, log => log.Action == "assessment_published");
    }

    [Fact]
    public async Task UTC036_Publish_RejectsUnobservedMarks()
    {
        await using var fixture = await AssessmentFixture.Create(output, "409; stays draft; profile and history unchanged");
        var draft = await fixture.Open();
        await Reject(409, () => fixture.Publish(draft));
        fixture.AssertDraftPreserved(draft);
    }

    [Fact]
    public async Task UTC037_Publish_RejectsStaleRevision()
    {
        await using var fixture = await AssessmentFixture.Create(output, "409; stays draft; profile and history unchanged");
        var draft = await fixture.CompleteDraft();
        await Reject(409, () => fixture.Publish(draft with { Revision = 0 }));
        fixture.AssertDraftPreserved(draft);
    }

    [Fact]
    public async Task UTC038_Publish_RejectsChangedProfile()
    {
        await using var fixture = await AssessmentFixture.Create(output, "409; stays draft; current profile and history unchanged");
        var draft = await fixture.CompleteDraft();
        await Reject(409, () => fixture.Publish(draft, expectedScore: 21));
        fixture.AssertDraftPreserved(draft);
    }

    [Fact]
    public async Task UTC039_Publish_RejectsNewerObservation()
    {
        await using var fixture = await AssessmentFixture.Create(output, "409; older draft and current profile stay intact");
        var draft = await fixture.CompleteDraft();
        await fixture.AddObservation(days: 1, total: 60);
        await Reject(409, () => fixture.Publish(draft));
        fixture.AssertDraftPreserved(draft);
    }

    [Fact]
    public async Task UTC040_Publish_RejectsMissingPriorObservation()
    {
        await using var fixture = await AssessmentFixture.Create(output, "409; remains draft; no publication history");
        await fixture.SetRated(score: 65, band: "B4");
        var draft = await fixture.CompleteDraft();
        await Reject(409, () => fixture.Publish(draft, 65, "B4"));
        fixture.AssertDraftPreserved(draft, 65, "B4");
    }

    [Fact]
    public async Task UTC041_Publish_RejectsConfirmedBandChange()
    {
        await using var fixture = await AssessmentFixture.Create(output, "409; confirmed band B3 and draft retained; no history");
        await fixture.SetRated(score: 55, band: "B3");
        var draft = await fixture.CompleteDraft();
        await fixture.AddObservation(days: -1, total: 65);
        await Reject(409, () => fixture.Publish(draft, 55, "B3"));
        fixture.AssertDraftPreserved(draft, 55, "B3");
    }

    [Fact]
    public async Task UTC042_Publish_PublishesPeriodicSameBand()
    {
        await using var fixture = await AssessmentFixture.Create(output, "Published profile 70/B4; raw observation remains 75; one history");
        await fixture.SetRated(score: 65, band: "B4");
        var draft = await fixture.CompleteDraft();
        await fixture.AddObservation(days: -1, total: 64);
        var result = await fixture.Publish(draft, 65, "B4");
        Assert.Equal("published", result.Status);
        Assert.Equal((short)75, result.TotalScore);
        Assert.Equal((short)70, fixture.Profile.CurrentScore);
        Assert.Equal("B4", fixture.Profile.CurrentBand);
        Assert.Equal((short)70, Assert.Single(fixture.Db.PlayerRatingHistories).NewScore);
    }

    private static MarkInput[] Marks() =>
        Enumerable.Range(1, 5).Select(id => new MarkInput(id, 3, null)).ToArray();

    private async Task Reject(int status, Func<Task> action)
    {
        var error = await Record.ExceptionAsync(action);
        output.WriteLine(error is AssessmentException assessmentError
            ? $"Actual: AssessmentException (HTTP {assessmentError.StatusCode}): {assessmentError.Message}"
            : $"Actual: {error?.ToString() ?? "No exception"}");
        var rejected = Assert.IsType<AssessmentException>(error);
        Assert.Equal(status, rejected.StatusCode);
    }

    private sealed class AssessmentClock : TimeProvider
    {
        public DateTimeOffset Now { get; set; } = new(2026, 11, 17, 12, 0, 0, TimeSpan.Zero);

        public override DateTimeOffset GetUtcNow() => Now;
    }

    private sealed class AssessmentFixture : IAsyncDisposable
    {
        public Sep490Context Db { get; }
        public AssessmentClock Clock { get; } = new();
        public Rubric Rubric { get; }
        public PlayerSportProfile Profile { get; }
        public Session Session { get; }
        public Ticket Ticket { get; }

        private readonly SkillAssessmentService service;

        private AssessmentFixture(ITestOutputHelper output)
        {
            this.output = output;
            Db = new Sep490Context(new DbContextOptionsBuilder<Sep490Context>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
            service = new SkillAssessmentService(
                TransactionlessRepository<ISkillAssessmentRepository>.Create(new SkillAssessmentRepository(Db, Clock), Db),
                Clock);

            var sport = new Sport { Id = 1, Code = "badminton", Name = "Badminton" };
            Rubric = new Rubric
            {
                Id = 1, Sport = sport, Name = "Badminton pilot", IsActive = true, Version = BadmintonScoring.RubricVersion,
                RubricDimensions = Enumerable.Range(1, 5).Select(id => new RubricDimension
                {
                    Id = id, Code = $"D{id}", Name = $"Criterion {id}", WeightPercent = 20
                }).ToList()
            };
            var player = new Player
            {
                Id = 2,
                User = new User { Id = 2, Email = "player@test.invalid", FullName = "Player", Status = "Active", RoleId = 1 }
            };
            Profile = new PlayerSportProfile
            {
                Id = 1, Player = player, Sport = sport, CurrentScore = 20, CurrentBand = "B2", RatingStatus = "Provisional"
            };
            Session = new Session
            {
                Id = 1, Sport = sport, Status = "Open", SessionDate = new(2026, 11, 17),
                TimeSlot = new TimeSlot { Id = 1, StartTime = new(18, 0), EndTime = new(20, 0) },
                Court = new Court { Id = 1, Name = "Court", Venue = new Venue { Id = 1, Name = "Venue" } },
                SessionReferees = new List<SessionReferee> { new() { RefereeId = 10 } }
            };
            Ticket = new Ticket
            {
                Id = 1, Player = player, Session = Session, TicketCode = "ATTENDED",
                Status = "checkedin", CheckedInBy = 10,
                Order = new SocialBookingOrder { Id = 1, OrderCode = "ORDER", PayerId = 2, Status = "paid" }
            };
        }

        public static async Task<AssessmentFixture> Create(ITestOutputHelper output, string expected)
        {
            var fixture = new AssessmentFixture(output);
            fixture.Db.AddRange(fixture.Profile, fixture.Rubric, fixture.Ticket);
            await fixture.Db.SaveChangesAsync();
            output.WriteLine($"Expected: {expected}");
            return fixture;
        }

        private readonly ITestOutputHelper output;

        public Task<AssessmentView> Open(long actor = 10, long player = 2) =>
            Trace("OpenAsync", new
            {
                sessionId = Session.Id, playerId = player, actorId = actor, now = Clock.Now,
                refereeIds = Session.SessionReferees.Select(referee => referee.RefereeId).ToArray(),
                ticketStatus = Ticket.Status, orderStatus = Ticket.Order.Status,
                checkedInBy = Ticket.CheckedInBy, activeRubric = Rubric.IsActive
            }, () => service.OpenAsync(Session.Id, player, actor, default));

        public Task<AssessmentView> Save(AssessmentView draft, IReadOnlyList<MarkInput> marks) =>
            Trace("SaveAsync", new { assessmentId = draft.Id, actorId = 10, draft.Revision, marks },
                () => service.SaveAsync(draft.Id, 10, new SaveAssessmentRequest(draft.Revision, marks), default));

        public async Task<AssessmentView> CompleteDraft() => await Save(await Open(), Marks());

        public Task<AssessmentView> Publish(AssessmentView draft, short expectedScore = 20, string expectedBand = "B2") =>
            Trace("PublishAsync", new
            {
                assessmentId = draft.Id, actorId = 10, draft.Revision, expectedScore, expectedBand,
                profileScore = Profile.CurrentScore, profileBand = Profile.CurrentBand,
                ratingStatus = Profile.RatingStatus,
                observations = Db.Assessments.Where(item => item.Id != draft.Id)
                    .Select(item => new { item.Id, item.Session!.SessionDate, item.Status, item.TotalScore }).ToArray()
            }, () => service.PublishAsync(draft.Id, 10, new PublishAssessmentRequest(draft.Revision, expectedScore, expectedBand), default));

        private async Task<AssessmentView> Trace(string method, object input, Func<Task<AssessmentView>> run)
        {
            output.WriteLine($"Input ({method}): {JsonSerializer.Serialize(input)}");
            var result = await run();
            output.WriteLine($"Actual ({method}): {JsonSerializer.Serialize(new
            {
                result.Id, result.Status, result.Revision, result.TotalScore, result.BandResult,
                result.CurrentScore, result.CurrentBand, rubric = result.Rubric.Version, result.Marks
            })}");
            return result;
        }

        public async Task SetRated(short score, string band)
        {
            Profile.CurrentScore = score;
            Profile.CurrentBand = band;
            Profile.RatingStatus = "Rated";
            await Db.SaveChangesAsync();
        }

        public async Task AddObservation(int days, short total)
        {
            Db.Assessments.Add(new Assessment
            {
                PlayerSportProfile = Profile, Rubric = Rubric, AssessorId = 10,
                AssessmentType = "on_court", Status = "published", TotalScore = total,
                Session = new Session
                {
                    Sport = Session.Sport, Court = Session.Court, TimeSlot = Session.TimeSlot,
                    SessionDate = Session.SessionDate.AddDays(days), Status = "Open"
                }
            });
            await Db.SaveChangesAsync();
        }

        public void AssertProfileUnchanged(short score = 20, string band = "B2")
        {
            Assert.Equal(score, Profile.CurrentScore);
            Assert.Equal(band, Profile.CurrentBand);
            Assert.Empty(Db.PlayerRatingHistories);
        }

        public void AssertDraftPreserved(AssessmentView draft, short score = 20, string band = "B2")
        {
            Assert.Equal("draft", Db.Assessments.Single(item => item.Id == draft.Id).Status);
            Assert.DoesNotContain(Db.AuditLogs, log => log.Action == "assessment_published");
            AssertProfileUnchanged(score, band);
        }

        public async ValueTask DisposeAsync()
        {
            output.WriteLine($"Actual persisted state: {JsonSerializer.Serialize(new
            {
                assessments = Db.Assessments.Select(item => new { item.Id, item.Status, item.TotalScore, item.BandResult }).ToArray(),
                profile = new { Profile.RatingStatus, Profile.CurrentScore, Profile.CurrentBand },
                history = Db.PlayerRatingHistories.Select(item => new { item.OldScore, item.NewScore, item.NewBand }).ToArray(),
                audit = Db.AuditLogs.Select(item => item.Action).ToArray()
            })}");
            await Db.DisposeAsync();
        }
    }
}
