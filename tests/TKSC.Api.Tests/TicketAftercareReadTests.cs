using Microsoft.EntityFrameworkCore;
using TKSC.Infrastructure.DbContext;
using TKSC.Infrastructure.Repositories;
using TKSC.Application.Services;
using Xunit;
namespace TKSC.Api.Tests;
<<<<<<< HEAD
=======
[Collection("SQL Server")]
>>>>>>> 0d40ca35efaf07c5d4887d6f8febc66c57f5578c
public class TicketAftercareReadTests
{
 [DatabaseFact]
 public async Task ApprovedTablesAndReadModelsQueryRealSqlWithoutWrites()
 {
  var connection=Environment.GetEnvironmentVariable("TKSC_TEST_SQL_CONNECTION")!;
  await using var db=new Sep490Context(new DbContextOptionsBuilder<Sep490Context>().UseSqlServer(connection).Options);
  _=await db.TicketChanges.AsNoTracking().Take(1).ToListAsync();
  _=await db.TicketSupportMessages.AsNoTracking().Take(1).ToListAsync();
  var support=new TicketSupportService(db,TimeProvider.System);
  Assert.NotNull(await support.Inbox());
  var denied=await Assert.ThrowsAsync<TKSC.Application.DTOs.PaymentException>(()=>support.List(-1,-1,false));
  _=await db.TicketFunding.AsNoTracking().Take(1).ToListAsync();
  var service=new TicketAftercareService(db,TimeProvider.System);
  Assert.NotNull(await service.SessionsAsync()); Assert.NotNull(await service.ChangesAsync());
  Assert.NotNull(await service.ReviewsAsync()); Assert.NotNull(await service.RefundsAsync());
  var payer=await db.SocialBookingOrders.Select(o=>(long?)o.PayerId).FirstOrDefaultAsync();
  if(payer.HasValue) Assert.NotNull(await new PaymentRepository(db).GetTicketsAsync(payer.Value));
 }
}
