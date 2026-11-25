using Microsoft.EntityFrameworkCore;
using TKSC.Domain.Entities;
using TKSC.Infrastructure.Persistence.Contexts;
using TKSC.Infrastructure.Persistence.Repositories;
using Xunit;

namespace TKSC.Api.Tests;

public sealed class TicketHistoryTests
{
    [Fact]
    public async Task HistoryProtectsOtherOrdersAndPrivateAuditPayloads()
    {
        await using var db = new Sep490Context(new DbContextOptionsBuilder<Sep490Context>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var role = new Role { Id=1, Code="Player", Name="Player" };
        var holder = new User { Id=1, FullName="Holder", Email="holder@example.test", Status="active", Role=role };
        var payer = new User { Id=2, FullName="Payer", Email="payer@example.test", Status="active", Role=role };
        var payment = new Payment { Id=5, ReferenceCode="PRIVATE-PAYMENT", Status="confirmed", Method="payos", AmountVnd=80000 };
        var ticket = new Ticket { Id=41, Status="cancelled", AmountVnd=40000,
            Player=new Player { Id=1, User=holder },
            Order=new SocialBookingOrder { Id=3, Payer=payer, OrderCode="SOC-TEST", Status="paid", Payments=[payment] },
            Session=new Session { Id=22, Status="Open", Court=new Court { Id=1, Name="Court", Venue=new Venue { Id=1, Name="Venue" } },
                Sport=new Sport { Id=1, Name="Badminton", Code="BM" }, TimeSlot=new TimeSlot { Id=1 } } };
        db.Tickets.Add(ticket);
        db.Refunds.Add(new Refund { Id=7, Ticket=ticket, Payment=payment, Recipient=payer, InitiatedByNavigation=payer, AmountVnd=40000, Status="processing" });
        db.AuditLogs.AddRange(
            new AuditLog { Id=1, EntityType="Ticket", EntityId=41, Action="ticket_cancelled", NewValue="private note" },
            new AuditLog { Id=2, EntityType="Refund", EntityId=7, Action="refund_payout", NewValue="secret bank account" },
            new AuditLog { Id=3, EntityType="Refund", EntityId=7, Action="refund_transfer_reference", NewValue="private bank reference" },
            new AuditLog { Id=4, EntityType="Ticket", EntityId=99, Action="ticket_cancelled", NewValue="other ticket" });
        await db.SaveChangesAsync();
        var repository = new PaymentRepository(db);
        Assert.Empty(await repository.GetTicketsAsync(99));
        Assert.Null(await repository.GetOrderAsync(3,1));
        var held = Assert.Single(await repository.GetTicketsAsync(1));
        Assert.False(held.CanManage); Assert.True(held.PaidForOther);
        Assert.Equal("Payer",held.PayerName); Assert.Equal("SOC-TEST",held.OrderCode);
        Assert.Empty(held.Payments); Assert.Equal(new long[]{1,2},held.History.Select(h=>h.Sequence));
        var json=System.Text.Json.JsonSerializer.Serialize(held);
        Assert.DoesNotContain("secret",json); Assert.DoesNotContain("private",json); Assert.DoesNotContain("PRIVATE-PAYMENT",json);
        var paid=Assert.Single(await repository.GetTicketsAsync(2));
        Assert.True(paid.CanManage); Assert.Equal("PRIVATE-PAYMENT",Assert.Single(paid.Payments).ReferenceCode);
    }
}
