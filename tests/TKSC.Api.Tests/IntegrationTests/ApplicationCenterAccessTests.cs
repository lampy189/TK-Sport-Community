using be_da.Application.Interfaces.External;
using be_da.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using TKSC.Domain.Entities;
using TKSC.Domain.Exceptions;
using TKSC.Infrastructure.Persistence.Contexts;
using Xunit;

namespace TKSC.Api.Tests;

public sealed class ApplicationCenterAccessTests
{
    private sealed record Scope(string Role, long? VenueId) : IDataAccessContext
    {
        public long? RestrictedVenueId => VenueId;
    }

    [Theory]
    [InlineData("Staff", 1L, 1)]
    [InlineData("Manager", 2L, 1)]
    [InlineData("Referee", 1L, 1)]
    [InlineData("Staff", null, 0)]
    [InlineData("Owner", null, 2)]
    [InlineData("Player", 1L, 2)]
    public async Task ScopeIsEvaluatedPerContextAndPersonnelWithoutAssignmentSeeNothing(string role, long? venue, int expected)
    {
        var options = new DbContextOptionsBuilder<Sep490Context>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        await using (var seed = new Sep490Context(options))
        {
            foreach (var center in new[] { 1L, 2L })
            {
                var product = new Product { Id = center, Sku = $"C{center}", Name = $"Center {center}" };
                seed.Products.Add(product);
                seed.Entry(product).Property("VenueId").CurrentValue = center;
            }
            await seed.SaveChangesAsync();
        }
        await using var db = new Sep490Context(options, new Scope(role, venue));
        Assert.Equal(expected, await db.Products.CountAsync());
        if (expected == 1) Assert.Equal(venue, (await db.Products.SingleAsync()).Id);
        await using var other = new Sep490Context(options, new Scope("Staff", 2));
        Assert.Equal(2, (await other.Products.SingleAsync()).Id);
    }

    [Fact]
    public async Task AttachedUpdatesDeletesAndForeignParentInsertsCannotBypassFilters()
    {
        var options = new DbContextOptionsBuilder<Sep490Context>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        await using (var seed = new Sep490Context(options))
        {
            var product = new Product { Id = 2, Sku = "OTHER", Name = "Original" };
            seed.Products.Add(product);
            seed.Entry(product).Property("VenueId").CurrentValue = 2L;
            await seed.SaveChangesAsync();
        }
        await using var db = new Sep490Context(options, new Scope("Staff", 1));
        var attached = new Product { Id = 2, Name = "Forged" };
        db.Attach(attached);
        db.Entry(attached).Property(x => x.Name).IsModified = true;
        await Assert.ThrowsAsync<PaymentException>(() => db.SaveChangesAsync());
        db.ChangeTracker.Clear();
        db.Remove(new Product { Id = 2 });
        Assert.Throws<PaymentException>(() => db.SaveChanges());
        db.ChangeTracker.Clear();
        db.StockItems.Add(new StockItem { ProductId = 2 });
        await Assert.ThrowsAsync<PaymentException>(() => db.SaveChangesAsync());
        await using var all = new Sep490Context(options);
        Assert.Equal("Original", (await all.Products.SingleAsync()).Name);
        Assert.Empty(await all.StockItems.ToListAsync());
    }

    [Fact]
    public void PersonnelAssignmentOverridesRequestedBrowsingCenter()
    {
        Assert.Equal(-1, CenterAccess.PersonnelVenue(new Scope("Manager", null)));
        Assert.Equal(1, CenterAccess.BrowsingVenue(new Scope("Staff", 1)));
    }
}
