using TKSC.Application.DTOs;
using Microsoft.EntityFrameworkCore;
using TKSC.Infrastructure.Persistence.Contexts;
using TKSC.Infrastructure.Persistence.Repositories;
using TKSC.Application.Services;
using Xunit;
namespace TKSC.Api.Tests;
[Collection("SQL Server")]
public class TicketAftercareReadTests
{
 [DatabaseFact]
 public async Task ApprovedTablesAndReadModelsQueryRealSqlWithoutWrites()
 {
  var connection=Environment.GetEnvironmentVariable("TKSC_TEST_SQL_CONNECTION")!;
  await using var db=new Sep490Context(new DbContextOptionsBuilder<Sep490Context>().UseSqlServer(connection).Options);
  _=await db.TicketChanges.AsNoTracking().Take(1).ToListAsync();
  _=await db.TicketSupportMessages.AsNoTracking().Take(1).ToListAsync();
  var support=new TicketSupportService(new TKSC.Infrastructure.Persistence.Repositories.TicketSupportRepository(db,TimeProvider.System),TimeProvider.System);
  Assert.NotNull(await support.Inbox());
  var denied=await Assert.ThrowsAsync<TKSC.Domain.Exceptions.PaymentException>(()=>support.List(-1,-1,false));
  _=await db.TicketFunding.AsNoTracking().Take(1).ToListAsync();
  var service=new TicketAftercareService(new TKSC.Infrastructure.Persistence.Repositories.TicketAftercareRepository(db,TimeProvider.System),TimeProvider.System);
  Assert.NotNull(await service.SessionsAsync()); Assert.NotNull(await service.ChangesAsync());
  Assert.NotNull(await service.ReviewsAsync()); Assert.NotNull(await service.RefundsAsync());
  var payer=await db.SocialBookingOrders.Select(o=>(long?)o.PayerId).FirstOrDefaultAsync();
  if(payer.HasValue) Assert.NotNull(await new PaymentRepository(db).GetTicketsAsync(payer.Value));
 }
}
