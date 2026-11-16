using be_da.Application.Interfaces.External;
using be_da.Application.DTOs;
using be_da.Infrastructure.Persistence.Repositories;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using TKSC.Domain.Entities;
using TKSC.Domain.Exceptions;
using TKSC.Infrastructure.Persistence.Contexts;
using Xunit;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace TKSC.Api.Tests;

[Collection("SQL Server")]
public sealed class ApplicationCenterDatabaseTests
{
    private sealed record Scope(string Role, long? VenueId) : IDataAccessContext
    {
        public long? RestrictedVenueId => Role == "Owner" ? null : VenueId;
    }
    private static string Connection => Environment.GetEnvironmentVariable("TKSC_TEST_SQL_CONNECTION")!;
    private static Sep490Context Db(IDataAccessContext? scope = null) => new(new DbContextOptionsBuilder<Sep490Context>().UseSqlServer(Connection).Options, scope);

    [DatabaseFact]
    public async Task PersonnelQueriesAreScopedWithSqlPolicyOffAndOwnerSeesBothCenters()
    {
        await using var all = Db();
        Assert.Equal(0, await all.Database.SqlQueryRaw<int>("SELECT CONVERT(int,is_enabled) AS Value FROM sys.security_policies WHERE name='SportCenterIsolation'").SingleAsync());
        var venues = await all.Courts.Select(c => c.VenueId).Distinct().OrderBy(x => x).ToArrayAsync();
        Assert.True(venues.Length >= 2);
        foreach (var venue in venues)
        {
            foreach (var role in new[] { "Staff", "Manager", "Referee" })
            {
                await using var scoped = Db(new Scope(role, venue));
                var sessions = await scoped.Sessions.Select(s => new { s.Id, s.Court.VenueId }).ToListAsync();
                Assert.NotEmpty(sessions);
                Assert.All(sessions, s => Assert.Equal(venue, s.VenueId));
                Assert.All(await scoped.Products.Select(p => EF.Property<long?>(p, "VenueId")).ToListAsync(), v => Assert.Equal(venue, v));
                // Compile and execute every scoped model query, including related financial/assessment rows.
                foreach (var entity in scoped.Model.GetEntityTypes().Where(e => e.GetQueryFilter() != null))
                {
                    var task = (Task)typeof(ApplicationCenterDatabaseTests).GetMethod(nameof(ReadIds), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!
                        .MakeGenericMethod(entity.ClrType).Invoke(null, [scoped])!;
                    await task;
                }
                var foreignSession = await all.Sessions.Where(s => s.Court.VenueId != venue).Select(s => s.Id).FirstAsync();
                Assert.Null(await scoped.Sessions.SingleOrDefaultAsync(s => s.Id == foreignSession));
            }
        }
        await using var owner = Db(new Scope("Owner", null));
        await using var unassigned = Db(new Scope("Staff", null));
        await using var player = Db(new Scope("Player", venues[0]));
        Assert.Equal(await all.Sessions.CountAsync(), await owner.Sessions.CountAsync());
        Assert.Equal(await all.Tickets.CountAsync(), await player.Tickets.CountAsync());
        Assert.Empty(await unassigned.Sessions.ToListAsync());
        Assert.Empty(await unassigned.Products.ToListAsync());
    }

    private static Task<List<long>> ReadIds<T>(Sep490Context db) where T : class => db.Set<T>().Select(x => EF.Property<long>(x, "Id")).Take(10).ToListAsync();

    [DatabaseFact]
    public async Task RealLoginRefreshAndApiIsolationWorkWithoutSqlPolicy()
    {
        await using var all = Db();
        var staff = await all.Users.Where(u => u.Role.Code == "Staff" && u.Status == "Active" && u.DeletedAt == null && EF.Property<long?>(u, "VenueId") != null)
            .Select(u => new { u.Email, Venue = EF.Property<long?>(u, "VenueId") }).ToListAsync();
        await using var app = new SqlApp();
        foreach (var actor in staff.GroupBy(s => s.Venue).Select(g => g.First()))
        {
            using var client = app.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost"), HandleCookies = true, AllowAutoRedirect = false });
            (await Post(client, "/api/auth/dev-login", new { email = actor.Email, portal = "staff", venueId = actor.Venue })).EnsureSuccessStatusCode();
            for (var i = 0; i < 2; i++)
            {
                var me = await client.GetFromJsonAsync<JsonElement>("/api/auth/me");
                Assert.Equal(actor.Venue!.Value.ToString(), me.GetProperty("selectedVenueId").GetString());
                var response = await client.GetAsync("/api/stock/products");
                response.EnsureSuccessStatusCode();
                var catalog = await response.Content.ReadFromJsonAsync<List<StockProduct>>();
                var expected = await all.Products.Where(p => EF.Property<long?>(p, "VenueId") == actor.Venue && p.IsActive == true).Select(p => p.Id).ToListAsync();
                Assert.Equal(expected.OrderBy(x => x), catalog!.Select(p => p.Id).OrderBy(x => x));
            }
            var foreign = await all.Products.Where(p => EF.Property<long?>(p, "VenueId") != actor.Venue).Select(p => p.Id).FirstAsync();
            Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/stock/products/{foreign}")).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync($"/api/stock/products/{foreign}/movements")).StatusCode);
            var foreignSession = await all.Sessions.Where(s => s.Court.VenueId != actor.Venue).Select(s => s.Id).FirstAsync();
            Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/sessions/{foreignSession}")).StatusCode);
            var ownOrderIds = await all.Orders.Where(o => EF.Property<long?>(o, "VenueId") == actor.Venue).Select(o => o.Id).ToListAsync();
            var ordersResponse = await client.GetAsync("/api/v1/commerce/goods-orders");
            ordersResponse.EnsureSuccessStatusCode();
            var orders = await ordersResponse.Content.ReadFromJsonAsync<JsonElement>();
            Assert.All(orders.EnumerateArray(), o => Assert.Contains(o.GetProperty("id").GetInt64(), ownOrderIds));
        }
    }

    private static async Task<HttpResponseMessage> Post(HttpClient client, string path, object payload)
    {
        var csrf = await client.GetFromJsonAsync<JsonElement>("/api/auth/csrf");
        using var request = new HttpRequestMessage(HttpMethod.Post, path) { Content = JsonContent.Create(payload) };
        request.Headers.Add("X-CSRF-TOKEN", csrf.GetProperty("token").GetString());
        return await client.SendAsync(request);
    }

    private sealed class SqlApp : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string,string?> {
                ["ConnectionStrings:Sep490"] = Connection, ["SportCenters:Enabled"] = "true",
                ["SportCenters:RequireAssignment"] = "true", ["Email:Enabled"] = "false",
                ["Logging:LogLevel:Default"] = "Error" }));
            builder.ConfigureTestServices(services => services.RemoveAll<IHostedService>());
        }
    }

    [DatabaseFact]
    public async Task AttachedForeignRowsAndForeignKeysAreRejectedBeforeWriting()
    {
        await using var all = Db();
        var products = await all.Products.Select(p => new { p.Id, Venue = EF.Property<long?>(p, "VenueId") }).ToListAsync();
        var own = products.First(p => p.Venue != null);
        var other = products.First(p => p.Venue != null && p.Venue != own.Venue);
        await using var db = Db(new Scope("Staff", own.Venue));
        await using var transaction = await db.Database.BeginTransactionAsync();
        try
        {
            var attached = new Product { Id = other.Id, Name = "MUST NOT WRITE" };
            db.Attach(attached);
            db.Entry(attached).Property(p => p.Name).IsModified = true;
            Assert.Equal(403, (await Assert.ThrowsAsync<PaymentException>(() => db.SaveChangesAsync())).StatusCode);
            db.ChangeTracker.Clear();
            db.StockItems.Add(new StockItem { ProductId = other.Id });
            await Assert.ThrowsAsync<PaymentException>(() => db.SaveChangesAsync());
            db.ChangeTracker.Clear();
            var allowed = await db.Products.SingleAsync(p => p.Id == own.Id);
            db.Entry(allowed).Property(p => p.Name).IsModified = true;
            Assert.Equal(1, await db.SaveChangesAsync());
        }
        finally { await transaction.RollbackAsync(); }
    }

    [DatabaseFact]
    public async Task DirectSqlStoresRejectForeignIdsAndLateEntryWorksWithoutPolicy()
    {
        await using var all = Db();
        var own = await all.Products.Select(p => EF.Property<long?>(p, "VenueId")).FirstAsync(v => v != null);
        var scope = new Scope("Staff", own);
        var foreignProduct = await all.Products.Where(p => EF.Property<long?>(p, "VenueId") != own).Select(p => new { p.Id, p.Sku }).FirstAsync();
        var foreignSession = await all.Sessions.Where(s => s.Court.VenueId != own).Select(s => s.Id).FirstAsync();
        var stock = new SqlStockStore(Connection, scope);
        Assert.Null(await stock.GetProductAsync(foreignProduct.Id, default));
        Assert.Null(await stock.LookupCodeAsync(foreignProduct.Sku, default));
        await Assert.ThrowsAsync<PaymentException>(() => stock.GetMovementsAsync(foreignProduct.Id, default));
        await Assert.ThrowsAsync<PaymentException>(() => stock.UpdateProductAsync(foreignProduct.Id, new UpdateProductRequest("Forbidden", "Other", 1000, 0, true), default));
        await Assert.ThrowsAsync<PaymentException>(() => stock.RecordMovementAsync(foreignProduct.Id, new StockMovementRequest("Receipt", 1), 1, default));
        var products = await stock.GetProductsAsync(new StockProductFilter(null, null, true), default);
        Assert.NotEmpty(products);
        Assert.DoesNotContain(products, p => p.Id == foreignProduct.Id);
        var supply = new SqlSessionSupplyStore(Connection, scope);
        await Assert.ThrowsAsync<PaymentException>(() => supply.GetDetailsAsync(foreignSession, default));
        await Assert.ThrowsAsync<PaymentException>(() => supply.IssueAsync(foreignSession, new IssueSupplyRequest(foreignProduct.Id, 1, 1), 1, default));
        var sessions = new SqlSessionStore(Connection, scope);
        Assert.Null(await sessions.GetSessionByIdAsync(foreignSession, null, default));
        await using var connection = new SqlConnection(Connection);
        await connection.OpenAsync();
        await LateEntrySql.RequireSchema(connection, null, default);
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?> { ["ConnectionStrings:Sep490"] = Connection }).Build();
        var revenue = new RevenueRepository(config, scope);
        var other = await all.Venues.Where(v => v.Id != own).Select(v => v.Id).FirstAsync();
        Assert.Null(await revenue.ReadAsync(new DateOnly(2026, 11, 1), new DateOnly(2026, 11, 30), other, default));
    }
}
