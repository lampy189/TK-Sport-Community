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
        var centers = await db.Database.SqlQueryRaw<long>("""
            SELECT DISTINCT p.venue_id AS Value FROM dbo.products p
            JOIN dbo.stock_items si ON si.product_id=p.id
            JOIN dbo.venues v ON v.id=p.venue_id
            WHERE p.is_active=1 AND si.quantity_on_hand>0 AND v.is_active=1
            ORDER BY Value
            """).ToArrayAsync();
        Assert.True(centers.Length >= 2, "Two active centers with stocked products are required for this integration check.");
        var firstCenter = centers[0];
        var secondCenter = centers[1];
        var accessor = new HttpContextAccessor();
        var previous = accessor.HttpContext;
        var http = new DefaultHttpContext { User = AccountPrincipal.Create(
            new Account(actor.Id, actor.Email, actor.FullName, "Player", "Active", "", false), true, firstCenter) };
        http.Items["CenterScopeEnabled"] = true;
        http.Items["SportCenterId"] = firstCenter;
        accessor.HttpContext = http;
        var config = new ConfigurationBuilder().AddInMemoryCollection(
            new Dictionary<string,string?> { ["ConnectionStrings:Sep490"] = connection }).Build();
        var controller = new StoreController(new TKSC.Application.Services.StoreService(new TKSC.Infrastructure.Persistence.Repositories.StoreRepository(db, config, new HttpDataAccessContext(accessor)), TimeProvider.System)) { ControllerContext = new ControllerContext { HttpContext = http } };
        await db.Database.OpenConnectionAsync();
        await using var transaction = await db.Database.BeginTransactionAsync();
        try
        {
            var firstProducts = Assert.IsAssignableFrom<IEnumerable<StoreProductItem>>(
                Assert.IsType<OkObjectResult>(await controller.GetOpenProducts()).Value).ToList();
            Assert.NotEmpty(firstProducts);
            foreach (var product in firstProducts)
                Assert.Equal(firstCenter, await db.Database.SqlQueryRaw<long>("SELECT venue_id AS Value FROM dbo.products WHERE id={0}", product.ProductId).SingleAsync());
            http.Items["SportCenterId"] = secondCenter;
            var secondProducts = Assert.IsAssignableFrom<IEnumerable<StoreProductItem>>(
                Assert.IsType<OkObjectResult>(await controller.GetOpenProducts()).Value).ToList();
            Assert.NotEmpty(secondProducts);
            foreach (var product in secondProducts)
                Assert.Equal(secondCenter, await db.Database.SqlQueryRaw<long>("SELECT venue_id AS Value FROM dbo.products WHERE id={0}", product.ProductId).SingleAsync());
            Assert.Empty(firstProducts.Select(p => p.ProductId).Intersect(secondProducts.Select(p => p.ProductId)));
            http.Items["SportCenterId"] = firstCenter;
            Assert.IsType<NotFoundObjectResult>(await controller.CreateCart(new([new CreateCartLine(secondProducts[0].ProductId, 1)]), "counter-cart-outside-center-1"));
            var cart = new CreateCartRequest([new CreateCartLine(firstProducts.First(p => p.QuantityOnHand > 0).ProductId, 1)]);
            var response = Assert.IsType<ObjectResult>(await controller.CreateCart(cart, "counter-cart-local-center-1"));
            Assert.Equal(StatusCodes.Status201Created, response.StatusCode);
            var created = Assert.IsType<StoreCartOrderResult>(response.Value);
            var retried = Assert.IsType<StoreCartOrderResult>(Assert.IsType<OkObjectResult>(await controller.CreateCart(cart, "counter-cart-local-center-1")).Value);
            Assert.Equal(created.OrderId, retried.OrderId);
            Assert.Equal(firstCenter, await db.Database.SqlQueryRaw<long>("SELECT venue_id AS Value FROM dbo.orders WHERE id={0}", created.OrderId).SingleAsync());
            http.Items["SportCenterId"] = secondCenter;
            var afterSwitch = Assert.IsAssignableFrom<IEnumerable<StoreProductItem>>(
                Assert.IsType<OkObjectResult>(await controller.GetOpenProducts()).Value).ToList();
            Assert.Equal(secondProducts.Select(p => p.ProductId), afterSwitch.Select(p => p.ProductId));
            Assert.Equal(firstCenter, await db.Database.SqlQueryRaw<long>("SELECT venue_id AS Value FROM dbo.orders WHERE id={0}", created.OrderId).SingleAsync());
        }
        finally
        {
            await transaction.RollbackAsync();
            accessor.HttpContext = previous;
        }
    }
}
