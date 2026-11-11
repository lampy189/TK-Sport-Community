using be_da.Authorization;
using be_da.Domain.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using TKSC.Application.DTOs;
using TKSC.Controllers;
using TKSC.Infrastructure.DbContext;
using Xunit;

namespace TKSC.Api.Tests;

public sealed class MultiCenterDatabaseTests
{
    [DatabaseFact]
    public async Task RealCatalogAndCartStayInSelectedCenterAndOrderRetainsOriginalCenter()
    {
        var connection = Environment.GetEnvironmentVariable("TKSC_TEST_SQL_CONNECTION")!;
        var options = new DbContextOptionsBuilder<Sep490Context>().UseSqlServer(connection)
            .AddInterceptors(new SportCenterConnectionInterceptor()).Options;
        await using var db = new Sep490Context(options);
        var actor = await db.Users.Where(u => u.Role.Code == "Player" && u.Status == "Active" && u.DeletedAt == null)
            .OrderBy(u => u.Id).FirstAsync();
        var accessor = new HttpContextAccessor();
        var previous = accessor.HttpContext;
        var http = new DefaultHttpContext { User = AccountPrincipal.Create(
            new Account(actor.Id, actor.Email, actor.FullName, "Player", "Active", "", false), true, 1) };
        http.Items["CenterScopeEnabled"] = true;
        http.Items["SportCenterId"] = 1L;
        accessor.HttpContext = http;
        var config = new ConfigurationBuilder().AddInMemoryCollection(
            new Dictionary<string,string?> { ["ConnectionStrings:Sep490"] = connection }).Build();
        var controller = new StoreController(db, config) { ControllerContext = new ControllerContext { HttpContext = http } };
        await db.Database.OpenConnectionAsync();
        await using var transaction = await db.Database.BeginTransactionAsync();
        try
        {
            var hn = Assert.IsAssignableFrom<IEnumerable<StoreProductItem>>(
                Assert.IsType<OkObjectResult>(await controller.GetOpenProducts()).Value).ToList();
            Assert.Equal(4, hn.Count);
            Assert.All(hn, p => Assert.StartsWith("HN-DEMO-", p.Sku));
            Assert.IsType<NotFoundObjectResult>(await controller.CreateCart(new([new CreateCartLine(1, 1)])));
            var created = Assert.IsType<StoreCartOrderResult>(Assert.IsType<OkObjectResult>(
                await controller.CreateCart(new([new CreateCartLine(hn.First().ProductId, 1)]))).Value);
            Assert.Equal(1L, await db.Database.SqlQueryRaw<long>("SELECT venue_id AS Value FROM dbo.orders WHERE id={0}", created.OrderId).SingleAsync());
            http.Items["SportCenterId"] = 2L;
            var vp = Assert.IsAssignableFrom<IEnumerable<StoreProductItem>>(
                Assert.IsType<OkObjectResult>(await controller.GetOpenProducts()).Value).ToList();
            Assert.Equal(4, vp.Count);
            Assert.DoesNotContain(vp, p => p.Sku.StartsWith("HN-DEMO-"));
            Assert.Equal(1L, await db.Database.SqlQueryRaw<long>("SELECT venue_id AS Value FROM dbo.orders WHERE id={0}", created.OrderId).SingleAsync());
        }
        finally
        {
            await transaction.RollbackAsync();
            accessor.HttpContext = previous;
        }
    }
}
