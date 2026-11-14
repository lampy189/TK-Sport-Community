using be_da.Infrastructure.Persistence;
using TKSC.Domain.Exceptions;
using be_da.Application.DTOs;
using be_da.Presentation.Authorization;
using be_da.Domain.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using TKSC.Application.DTOs;
using TKSC.Presentation.Controllers;
using TKSC.Infrastructure.Persistence.Contexts;
using Xunit;

namespace TKSC.Api.Tests;

[Collection("SQL Server")]
public sealed class MultiCenterDatabaseTests
{
    [DatabaseFact]
    public async Task RealCatalogAndCartStayInSelectedCenterAndOrderRetainsOriginalCenter()
    {
        var connection = Environment.GetEnvironmentVariable("TKSC_TEST_SQL_CONNECTION")!;
        var options = new DbContextOptionsBuilder<Sep490Context>().UseSqlServer(connection)
            .AddInterceptors(new SportCenterConnectionInterceptor(new be_da.Presentation.Authorization.HttpDataAccessContext(new Microsoft.AspNetCore.Http.HttpContextAccessor()))).Options;
        await using var db = new Sep490Context(options);
        var actor = await db.Users.Where(u => u.Role.Code == "Player" && u.Status == "Active" && u.DeletedAt == null)
            .OrderBy(u => u.Id).FirstAsync();
        // Imported databases assign different IDs and SKUs. Test tenancy, not a particular seed snapshot.
        var venues = await db.Database.SqlQueryRaw<long>("SELECT DISTINCT venue_id AS Value FROM dbo.products WHERE venue_id IS NOT NULL ORDER BY venue_id").ToListAsync();
        Assert.True(venues.Count >= 2, "This integration fixture requires catalogs in two centers.");
        var firstVenue=venues[0]; var secondVenue=venues[1];
        var firstProductIds=await db.Database.SqlQueryRaw<long>("SELECT id AS Value FROM dbo.products WHERE venue_id={0}",firstVenue).ToListAsync();
        var secondProductIds=await db.Database.SqlQueryRaw<long>("SELECT id AS Value FROM dbo.products WHERE venue_id={0}",secondVenue).ToListAsync();
        var accessor = new HttpContextAccessor();
        var previous = accessor.HttpContext;
        var http = new DefaultHttpContext { User = AccountPrincipal.Create(
            new Account(actor.Id, actor.Email, actor.FullName, "Player", "Active", "", false), true, firstVenue) };
        http.Items["CenterScopeEnabled"] = true;
        http.Items["SportCenterId"] = firstVenue;
        accessor.HttpContext = http;
        var config = new ConfigurationBuilder().AddInMemoryCollection(
            new Dictionary<string,string?> { ["ConnectionStrings:Sep490"] = connection }).Build();
        var controller = new StoreController(new TKSC.Application.Services.StoreService(new TKSC.Infrastructure.Persistence.Repositories.StoreRepository(db, config, new HttpDataAccessContext(accessor)), TimeProvider.System)) { ControllerContext = new ControllerContext { HttpContext = http } };
        await db.Database.OpenConnectionAsync();
        await using var transaction = await db.Database.BeginTransactionAsync();
        try
        {
            var hn = Assert.IsAssignableFrom<IEnumerable<StoreProductItem>>(
                Assert.IsType<OkObjectResult>(await controller.GetOpenProducts()).Value).ToList();
            Assert.NotEmpty(hn);
            Assert.All(hn, p => Assert.Contains(p.ProductId,firstProductIds));
            Assert.IsType<NotFoundObjectResult>(await controller.CreateCart(new([new CreateCartLine(secondProductIds.First(), 1)]), "counter-cart-outside-center-1"));
            var createdResponse = Assert.IsType<ObjectResult>(
                await controller.CreateCart(new([new CreateCartLine(hn.First().ProductId, 1)]), "counter-cart-local-center-1"));
            Assert.Equal(StatusCodes.Status201Created, createdResponse.StatusCode);
            var created = Assert.IsType<StoreCartOrderResult>(createdResponse.Value);
            var retried = Assert.IsType<StoreCartOrderResult>(Assert.IsType<OkObjectResult>(
                await controller.CreateCart(new([new CreateCartLine(hn.First().ProductId, 1)]), "counter-cart-local-center-1")).Value);
            Assert.Equal(created.OrderId, retried.OrderId);
            Assert.Equal(firstVenue, await db.Database.SqlQueryRaw<long>("SELECT venue_id AS Value FROM dbo.orders WHERE id={0}", created.OrderId).SingleAsync());
            http.Items["SportCenterId"] = secondVenue;
            var vp = Assert.IsAssignableFrom<IEnumerable<StoreProductItem>>(
                Assert.IsType<OkObjectResult>(await controller.GetOpenProducts()).Value).ToList();
            Assert.NotEmpty(vp);
            Assert.All(vp,p=>Assert.Contains(p.ProductId,secondProductIds));
            Assert.DoesNotContain(vp,p=>firstProductIds.Contains(p.ProductId));
            Assert.Equal(firstVenue, await db.Database.SqlQueryRaw<long>("SELECT venue_id AS Value FROM dbo.orders WHERE id={0}", created.OrderId).SingleAsync());
        }
        finally
        {
            await transaction.RollbackAsync();
            accessor.HttpContext = previous;
        }
    }
}
