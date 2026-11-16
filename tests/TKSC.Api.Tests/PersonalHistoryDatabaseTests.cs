using Microsoft.EntityFrameworkCore;
using TKSC.Infrastructure.Persistence.Contexts;
using TKSC.Infrastructure.Persistence.Repositories;
using Xunit;

namespace TKSC.Api.Tests;

// Read-only acceptance checks: safe on the user's configured database, no gateway calls.
[Collection("SQL Server")]
public sealed class PersonalHistoryDatabaseTests
{
    private static Sep490Context Db() => new(new DbContextOptionsBuilder<Sep490Context>()
        .UseSqlServer(Environment.GetEnvironmentVariable("TKSC_TEST_SQL_CONNECTION")!).Options);

    [DatabaseFact]
    public async Task TicketHistoryReadsRealDataAndDeniesUnknownUser()
    {
        await using var db=Db();
        var repo=new PaymentRepository(db);
        Assert.Empty(await repo.GetTicketsAsync(-1));
        var payer=await db.SocialBookingOrders.Where(o=>o.Tickets.Any()).Select(o=>(long?)o.PayerId).FirstOrDefaultAsync();
        Assert.NotNull(payer);
        var rows=await repo.GetTicketsAsync(payer!.Value);
        Assert.NotEmpty(rows);
        Assert.All(rows.Where(t=>t.CanManage),t=>Assert.Equal(payer,t.PayerId));
        Assert.All(rows.Where(t=>!t.CanManage),t=>Assert.Empty(t.Payments));
    }

    [DatabaseFact]
    public async Task GoodsHistoryReadsRealBuyerOrders()
    {
        await using var db=Db();
        var buyer=await db.Orders.Where(o=>o.BuyerId!=null).Select(o=>o.BuyerId).FirstOrDefaultAsync();
        Assert.NotNull(buyer);
        var repo=new GoodsPaymentRepository(db,TimeProvider.System);
        var rows=await repo.ListForBuyerOrdersListAsync(buyer!.Value,default);
        Assert.All(rows,o=>Assert.Equal(buyer,o.BuyerId));
        if(rows.Count>0) Assert.NotNull(await repo.HistoryAsync(rows[0].Id,default));
    }
}
