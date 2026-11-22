using System.Reflection;
using System.Runtime.ExceptionServices;
using Microsoft.EntityFrameworkCore;
using TKSC.Application.DTOs;
using TKSC.Application.Interfaces.Repositories;
using TKSC.Application.Services;
using TKSC.Domain.Assessments;
using TKSC.Domain.Entities;
using TKSC.Infrastructure.Persistence.Contexts;
using TKSC.Infrastructure.Persistence.Repositories;
using Xunit;

namespace TKSC.Api.Tests;

public class RatingTicketIsolationTests
{
    private sealed class AssessmentClock : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(2026, 11, 17, 12, 0, 0, TimeSpan.Zero);
    }
    private static readonly TimeProvider Clock = new AssessmentClock();
    [Theory]
    [InlineData("paid")]
    [InlineData("review_required")]
    public async Task PublishingChangedBandPreservesTicketsAndPaymentDataIncludingOnRetry(string paymentStatus)
    {
        await using var db = await Setup(paymentStatus);
        var service = AssessmentService(db);
        var request = new PublishAssessmentRequest(0, 20, "B2");

        var result = await service.PublishAsync(1, 10, request, default);
        Assert.Equal("published", result.Status);
        Assert.Equal("B4", (await db.PlayerSportProfiles.SingleAsync()).CurrentBand);
        Assert.Single(await db.PlayerRatingHistories.ToListAsync());
        await AssertTicketDataUnchanged(db, paymentStatus);

        await service.PublishAsync(1, 10, request, default);
        Assert.Single(await db.PlayerRatingHistories.ToListAsync());
        await AssertTicketDataUnchanged(db, paymentStatus);
    }

    [Fact]
    public async Task ManagerCorrectionChangesRatingWithoutTouchingTicketsOrPayments()
    {
        await using var db = await Setup("paid");
        await AssessmentService(db).PublishAsync(1, 10, new(0, 20, "B2"), default);
        var history = await db.PlayerRatingHistories.SingleAsync();
        db.RatingAppeals.Add(new RatingAppeal
        {
            Id = 1, AssessmentId = 1, PlayerSportProfileId = 1,
            SubmittedBy = 2, Status = "UnderReview"
        });
        db.AuditLogs.Add(new AuditLog
        {
            EntityType = "RatingAppeal", EntityId = 1, ActorId = 10,
            Action = "referee_review", NewValue = "{}"
        });
        await db.SaveChangesAsync();
        var repository = TransactionlessRepository<ISkillReviewRepository>.Create(
            new SkillReviewRepository(db, Clock), db);
        var service = new SkillReviewService(repository, Clock);

        await service.DecideAppeal(1, 30, new AppealDecision("Correct observed result", true, 85, history.Id), default);

        Assert.Equal("B5", (await db.PlayerSportProfiles.SingleAsync()).CurrentBand);
        Assert.Equal((short)85, (await db.PlayerSportProfiles.SingleAsync()).CurrentScore);
        Assert.Equal("Corrected", (await db.RatingAppeals.SingleAsync()).Status);
        Assert.Equal(2, await db.PlayerRatingHistories.CountAsync());
        await AssertTicketDataUnchanged(db, "paid");
    }

    private static SkillAssessmentService AssessmentService(Sep490Context db) => new(
        TransactionlessRepository<ISkillAssessmentRepository>.Create(
            new SkillAssessmentRepository(db, Clock), db), Clock);

    private static async Task<Sep490Context> Setup(string paymentStatus)
    {
        var db = new Sep490Context(new DbContextOptionsBuilder<Sep490Context>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var sport = new Sport { Id = 1, Code = "badminton", Name = "Badminton" };
        var rubric = new Rubric
        {
            Id = 1, Sport = sport, Version = BadmintonScoring.RubricVersion, Name = "Test rubric",
            RubricDimensions = Enumerable.Range(1, 5).Select(i => new RubricDimension
                { Id = i, Code = $"D{i}", Name = $"Criterion {i}", WeightPercent = 20 }).ToList()
        };
        var player = new Player
        {
            Id = 2, User = new User { Id = 2, Email = "player@test.invalid", FullName = "Player", Status = "Active", RoleId = 1 }
        };
        var profile = new PlayerSportProfile
        {
            Id = 1, Player = player, Sport = sport, CurrentScore = 20, CurrentBand = "B2", RatingStatus = "Provisional"
        };
        var court = new Court { Id = 1, Name = "Court", Venue = new Venue { Id = 1, Name = "Venue" } };
        var slot = new TimeSlot { Id = 1, StartTime = new(18, 0), EndTime = new(20, 0) };
        var observed = new Session
        {
            Id = 1, Sport = sport, Court = court, TimeSlot = slot, SessionDate = new(2026, 11, 17), Status = "Open",
            SessionReferees = new List<SessionReferee> { new() { RefereeId = 10 } }
        };
        db.Assessments.Add(new Assessment
        {
            Id = 1, PlayerSportProfile = profile, Rubric = rubric, Session = observed,
            AssessorId = 10, AssessmentType = "on_court", Status = "draft",
            AssessmentScores = Enumerable.Range(1, 5).Select(i => new AssessmentScore
                { DimensionId = i, Score = 3 }).ToList()
        });
        var order = new SocialBookingOrder { Id = 1, OrderCode = "PAID", PayerId = 9, Status = "paid" };
        db.Tickets.Add(new Ticket
            { Id = 1, Player = player, Session = observed, Order = order, TicketCode = "ATTENDED", Status = "checkedin", CheckedInBy = 10 });
        db.Tickets.Add(new Ticket
        {
            Id = 2, Player = player, Order = order, TicketCode = "FUTURE", AmountVnd = 100, Status = "confirmed",
            Session = new Session
            {
                Id = 2, Sport = sport, Court = court, TimeSlot = slot,
                SessionDate = new DateOnly(2026, 12, 17), Band = "B2", Status = "Open"
            }
        });
        db.Payments.Add(new Payment
            { Id = 1, Order = order, PayerId = 9, AmountVnd = 100, Status = paymentStatus, ReferenceCode = "PAYMENT", Method = "test" });
        db.TicketFunding.Add(new TicketFunding { Id = 1, TicketId = 2, PaymentId = 1, AmountVnd = 100 });
        await db.SaveChangesAsync();
        return db;
    }

    private static async Task AssertTicketDataUnchanged(Sep490Context db, string paymentStatus)
    {
        Assert.Equal("checkedin", (await db.Tickets.SingleAsync(t => t.Id == 1)).Status);
        var future = await db.Tickets.SingleAsync(t => t.Id == 2);
        Assert.Equal("confirmed", future.Status);
        Assert.Equal(100, future.AmountVnd);
        Assert.Equal(paymentStatus, (await db.Payments.SingleAsync()).Status);
        Assert.Equal(100, (await db.Payments.SingleAsync()).AmountVnd);
        Assert.Equal("paid", (await db.SocialBookingOrders.SingleAsync()).Status);
        var funding = await db.TicketFunding.SingleAsync();
        Assert.Equal(100, funding.AmountVnd);
        Assert.False(funding.Retired);
        Assert.Empty(await db.Refunds.ToListAsync());
        Assert.Empty(await db.Notifications.ToListAsync());
        Assert.False(await db.AuditLogs.AnyAsync(a => a.EntityType == "Ticket"));
    }
}

// Exercise the real repository queries and persistence on InMemory, bypassing only
// SQL Server transactions/application locks. This does not validate SQL concurrency.
public class TransactionlessRepository<T> : DispatchProxy where T : class
{
    private T target = null!;
    private Sep490Context db = null!;

    public static T Create(T target, Sep490Context db)
    {
        var proxy = Create<T, TransactionlessRepository<T>>();
        var wrapper = (TransactionlessRepository<T>)(object)proxy;
        wrapper.target = target;
        wrapper.db = db;
        return proxy;
    }

    protected override object? Invoke(MethodInfo? method, object?[]? args)
    {
        if (method!.Name is "MutateAsync" or "Write")
            return GetType().BaseType!.GetMethod(nameof(RunAsync), BindingFlags.Instance | BindingFlags.NonPublic)!
                .MakeGenericMethod(method.GetGenericArguments()).Invoke(this, new[] { args![0], method.Name == "Write" });
        try { return method.Invoke(target, args); }
        catch (TargetInvocationException e) when (e.InnerException != null)
        {
            ExceptionDispatchInfo.Capture(e.InnerException).Throw();
            throw;
        }
    }

    private async Task<TResult> RunAsync<TResult>(Func<Task<TResult>> action, bool save)
    {
        var result = await action();
        if (save) await db.SaveChangesAsync();
        return result;
    }
}
