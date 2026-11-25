using be_da.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using TKSC.Infrastructure.Persistence.Contexts;
using Xunit;

namespace TKSC.Api.Tests;

public sealed class OwnerPriceSqlTranslationTests
{
    [Fact]
    public void EffectivePricesTranslateAndSoldPriceColumnsRemainIndependent()
    {
        using var db = new Sep490Context(new DbContextOptionsBuilder<Sep490Context>()
            .UseSqlServer("Server=unused;Database=unused;Integrated Security=true;TrustServerCertificate=true").Options);
        var evaluatedAt = new DateTimeOffset(2026, 11, 15, 0, 0, 0, TimeSpan.Zero);
        var sql = db.Products.Select(p => new
        {
            Price = OwnerPrice.Effective("product", p.Id, p.UnitPriceVnd, evaluatedAt),
            Version = OwnerPrice.Version("product", p.Id, evaluatedAt)
        }).ToQueryString();
        Assert.Contains("[dbo].[OwnerEffectivePrice]", sql);
        Assert.Contains("[dbo].[OwnerPriceVersion]", sql);
        var sold = db.OrderItems.Select(i => new { i.UnitPriceVnd, i.PriceVersionId }).ToQueryString();
        Assert.DoesNotContain("OwnerEffectivePrice", sold);
        Assert.Contains("price_version_id", sold);
        Assert.DoesNotContain("OwnerEffectivePrice", db.Tickets.Select(t => t.AmountVnd).ToQueryString());
    }
}
