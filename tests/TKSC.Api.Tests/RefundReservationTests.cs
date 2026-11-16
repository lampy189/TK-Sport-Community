using Microsoft.EntityFrameworkCore;
using TKSC.Domain.Entities;
using TKSC.Infrastructure.Persistence.Contexts;
using TKSC.Infrastructure.Persistence.Repositories;
using Xunit;
namespace TKSC.Api.Tests;
public class RefundReservationTests
{
 [Theory][InlineData("failed",110)][InlineData("requested",110)][InlineData("rejected",80)]
 public async Task FailedRefundStillReservesMoneyButRejectedRefundReleasesIt(string status,long expected){await using var db=new Sep490Context(new DbContextOptionsBuilder<Sep490Context>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);db.Tickets.Add(new Ticket { Id=1,Status="cancelled",Session=new Session { Id=1,Status="Published",Court=new Court { Id=1,VenueId=1,Name="Court",Venue=new Venue {Id=1,Name="Venue"} } } });var current=new Refund{Id=1,TicketId=1,PaymentId=3,AmountVnd=80,Status="requested"};db.Refunds.AddRange(current,new(){Id=2,TicketId=1,PaymentId=3,AmountVnd=30,Status=status});await db.SaveChangesAsync();Assert.Equal(expected,await new TicketAftercareRepository(db,TimeProvider.System).CompleteRefundRefundsTotalAsync(current));}
 [Theory][InlineData("payos:BANK-01")][InlineData("staff:BANK-01")][InlineData("BANK-01")]
 public async Task IncomingReferenceCannotBeReusedAsOutgoingProof(string incoming){await using var db=new Sep490Context(new DbContextOptionsBuilder<Sep490Context>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);db.BankTransactions.Add(new(){Id=1,ExternalId=incoming,AmountVnd=100});await db.SaveChangesAsync();Assert.True(await new TicketAftercareRepository(db,TimeProvider.System).CompleteChangeBankTransactionsExistsAsync("BANK-01"));}
}
