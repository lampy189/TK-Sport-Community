using Microsoft.EntityFrameworkCore;
using TKSC.Application.Services;
using TKSC.Domain.Entities;
using TKSC.Domain.Exceptions;
using TKSC.Infrastructure.Persistence.Contexts;
using TKSC.Infrastructure.Persistence.Repositories;
using Xunit;

namespace TKSC.Api.Tests;
public class FutureTicketFundingTests
{
    private static async Task<(Sep490Context Db, PlayerSportProfile Profile, Ticket Ticket)> Setup(int price)
    {
        var db = new Sep490Context(new DbContextOptionsBuilder<Sep490Context>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var player = new Player
        {
            Id = 2,
            User = new User
            {
                Id = 2,
                Email = "holder@test.invalid",
                FullName = "Holder",
                Status = "Active",
                RoleId = 1
            }
        };
        var profile = new PlayerSportProfile
        {
            Id = 2,
            Player = player,
            SportId = 1,
            CurrentScore = 80,
            CurrentBand = "B4"
        };
        var session = new Session
        {
            Id = 4,
            SportId = 1,
            SessionDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(3)),
            TimeSlot = new TimeSlot
            {
                Id = 1,
                StartTime = new(18, 0),
                EndTime = new(20, 0)
            },
            Band = "B3",
            Status = "Published"
        };
        var ticket = new Ticket
        {
            Id = 7,
            Player = player,
            Session = session,
            AmountVnd = price,
            Status = "confirmed",
            Order = new SocialBookingOrder
            {
                Id = 2,
                OrderCode = "EXCHANGED",
                PayerId = 9,
                Status = "paid"
            }
        };
        db.PlayerSportProfiles.Add(profile);
        db.Tickets.Add(ticket);
        db.Payments.Add(new Payment { Id = 10, OrderId = 1, PayerId = 9, AmountVnd = 100, Status = "paid", ReferenceCode = "ORIGINAL", Method = "test" });
        db.TicketFunding.Add(new() { Id = 1, TicketId = 7, PaymentId = 10, AmountVnd = Math.Min(price, 100) });
        if (price > 100)
        {
            db.Payments.Add(new Payment { Id = 20, OrderId = 2, PayerId = 9, AmountVnd = price - 100, Status = "paid", ReferenceCode = "DELTA", Method = "test" });
            db.TicketFunding.Add(new() { Id = 2, TicketId = 7, PaymentId = 20, AmountVnd = price - 100 });
        }

        await db.SaveChangesAsync();
        return (db, profile, ticket);
    }

    [Theory]
    [InlineData(80)]
    [InlineData(100)]
    [InlineData(140)]
    public async Task RatingChangeAfterExchangeUsesAllActiveSourcesAndOriginalPayer(int price)
    {
        var(db, profile, ticket) = await Setup(price);
        await using var owned = db;
        var service = new SkillAssessmentService(new SkillAssessmentRepository(db, TimeProvider.System), TimeProvider.System);
        await service.CheckFutureTicketsAsync(profile, 3, default);
        await db.SaveChangesAsync();
        var refunds = await db.Refunds.OrderBy(r => r.PaymentId).ToListAsync();
        Assert.Equal(price, refunds.Sum(r => r.AmountVnd));
        Assert.All(refunds, r => Assert.Equal(9, r.RecipientId));
        Assert.Equal("cancelled", ticket.Status);
        Assert.Equal(price > 100 ? 2 : 1, refunds.Count);
        Assert.All(refunds, r => Assert.NotEqual(2, r.RecipientId));
        await service.CheckFutureTicketsAsync(profile, 3, default);
        await db.SaveChangesAsync();
        Assert.Equal(refunds.Count, await db.Refunds.CountAsync());
    }

    [Theory]
    [InlineData("requested")]
    [InlineData("processing")]
    [InlineData("completed")]
    public async Task ExistingReservedOrCompletedRefundIsNotDuplicated(string status)
    {
        var(db, profile, ticket) = await Setup(80);
        await using var owned = db;
        db.Refunds.Add(new() { TicketId = 7, PaymentId = 10, RecipientId = 9, AmountVnd = 80, Status = status, InitiatedBy = 3 });
        await db.SaveChangesAsync();
        await new SkillAssessmentService(new SkillAssessmentRepository(db, TimeProvider.System), TimeProvider.System).CheckFutureTicketsAsync(profile, 3, default);
        await db.SaveChangesAsync();
        Assert.Single(await db.Refunds.ToListAsync());
        Assert.Equal("cancelled", ticket.Status);
    }

    [Theory]
    [InlineData("pending")]
    [InlineData("failed")]
    [InlineData("review_required")]
    public async Task UnverifiedSourceNeverCreatesRefundOrCancelsTicket(string status)
    {
        var(db, profile, ticket) = await Setup(80);
        await using var owned = db;
        (await db.Payments.FindAsync(10L))!.Status = status;
        await db.SaveChangesAsync();
        await Assert.ThrowsAsync<AssessmentException>(() => new SkillAssessmentService(new SkillAssessmentRepository(db, TimeProvider.System), TimeProvider.System).CheckFutureTicketsAsync(profile, 3, default));
        Assert.Equal("confirmed", ticket.Status);
        Assert.Empty(db.Refunds);
    }

    [Theory]
    [InlineData(21)]
    [InlineData(50)]
    [InlineData(100)]
    public async Task RefundsOnOtherTicketsCannotExceedTheOriginalPayment(int reserved)
    {
        var(db, profile, ticket) = await Setup(80);
        await using var owned = db;
        db.Refunds.Add(new() { TicketId = 999, PaymentId = 10, RecipientId = 9, AmountVnd = reserved, Status = "processing", InitiatedBy = 3 });
        await db.SaveChangesAsync();
        await Assert.ThrowsAsync<AssessmentException>(() => new SkillAssessmentService(new SkillAssessmentRepository(db, TimeProvider.System), TimeProvider.System).CheckFutureTicketsAsync(profile, 3, default));
        Assert.Equal("confirmed", ticket.Status);
    }
    [Theory]
    [InlineData(21)]
    [InlineData(50)]
    [InlineData(100)]
    public async Task UnsavedRefundsFromTheSameBatchAlsoReserveThePayment(int pending)
    {
        var (db, profile, ticket) = await Setup(80);
        await using var owned = db;
        db.Refunds.Add(new() { TicketId = 999, PaymentId = 10, RecipientId = 9, AmountVnd = pending, Status = "requested", InitiatedBy = 3 });
        await Assert.ThrowsAsync<AssessmentException>(() => new SkillAssessmentService(new SkillAssessmentRepository(db, TimeProvider.System), TimeProvider.System).CheckFutureTicketsAsync(profile, 3, default));
        Assert.Equal("confirmed", ticket.Status);
    }
}
