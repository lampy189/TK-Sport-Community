using be_da.Infrastructure.Persistence;
using be_da.Presentation.Authorization;
using be_da.Infrastructure.Persistence.Repositories;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using TKSC.Infrastructure.Persistence.Contexts;
using TKSC.Infrastructure.Persistence.Repositories;
using Xunit;
namespace TKSC.Api.Tests;
[Collection("SQL Server")]
public sealed class LateEntryDatabaseReadTests
{
    [DatabaseFact]
    public async Task StandaloneDatabaseQuotesRealSessionWithoutPersonnelAssignmentAndClosesAtEnd()
    {
        var connection = Environment.GetEnvironmentVariable("TKSC_TEST_SQL_CONNECTION")!;
        await using var sql = new SqlConnection(connection);
        await SportCenterConnection.OpenAsync(sql);
        if (!await LateEntrySql.IsStandaloneSchema(sql, CancellationToken.None)) return;
        await LateEntrySql.RequireSchema(sql, null, CancellationToken.None);
        await using var command = new SqlCommand("""
            SELECT TOP (1) s.id,s.session_date,ts.start_time,ts.end_time,s.price_vnd
            FROM dbo.sessions s JOIN dbo.time_slots ts ON ts.id=s.time_slot_id
            JOIN dbo.courts c ON c.id=s.court_id JOIN dbo.venues v ON v.id=c.venue_id
            WHERE LOWER(s.status) IN ('published','open','ongoing') AND v.is_active=1
              AND s.price_vnd BETWEEN 2000 AND 100000000 AND ts.end_time>ts.start_time
            ORDER BY s.id DESC;
            """, sql);
        long id;
        DateTimeOffset start, end;
        int basePrice;
        await using (var reader = await command.ExecuteReaderAsync())
        {
            Assert.True(await reader.ReadAsync(), "A published session is required for this read-only database check.");
            id = reader.GetInt64(0);
            var date = DateOnly.FromDateTime(reader.GetDateTime(1));
            start = TKSC.Domain.Payments.LateEntryRules.End(date, TimeOnly.FromTimeSpan(reader.GetTimeSpan(2)));
            end = TKSC.Domain.Payments.LateEntryRules.End(date, TimeOnly.FromTimeSpan(reader.GetTimeSpan(3)));
            basePrice = reader.GetInt32(4);
        }
        var config = new ConfigurationBuilder().AddInMemoryCollection(
            new Dictionary<string,string?> { ["ConnectionStrings:Sep490"] = connection }).Build();
        var clock = new FixedClock(start.AddTicks((end-start).Ticks / 2));
        var store = new SqlLateEntryStore(config, clock);
        var offers = await store.List(null, id, true, CancellationToken.None);
        var offer = Assert.Single(offers);
        Assert.Equal((int)decimal.Ceiling(basePrice * .5m), offer.PriceVnd);
        Assert.Equal(end, offer.EndsAt);
        clock.Now = end;
        Assert.Empty(await store.List(null, id, true, CancellationToken.None));
    }

    private sealed class FixedClock(DateTimeOffset now) : TimeProvider
    {
        public DateTimeOffset Now { get; set; } = now;
        public override DateTimeOffset GetUtcNow() => Now;
    }

    [DatabaseFact]
    public async Task ScopedCheckInQueriesCompileAndOptionalUpgradeDoesNotBreakPublicRead()
    {
        var connection=Environment.GetEnvironmentVariable("TKSC_TEST_SQL_CONNECTION")!;
        var options=new DbContextOptionsBuilder<Sep490Context>().UseSqlServer(connection).AddInterceptors(new SportCenterConnectionInterceptor(new be_da.Presentation.Authorization.HttpDataAccessContext(new Microsoft.AspNetCore.Http.HttpContextAccessor()))).Options;
        await using var db=new Sep490Context(options);var repo=new PaymentRepository(db);
        Assert.Empty(await repo.GetAssignedSessionsAsync(long.MinValue));
        Assert.Null(await repo.GetCheckInContextAsync("LATE-nonexistent-read-check",long.MinValue));
        var config=new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?>{{"ConnectionStrings:Sep490",connection}}).Build();
        var store=new SqlLateEntryStore(config,TimeProvider.System);
        Assert.NotNull(await store.List(null,long.MinValue,false,CancellationToken.None));
        await using var sql=new SqlConnection(connection);
        await SportCenterConnection.OpenAsync(sql);
        Assert.False(await LateEntrySql.Eligible(sql,null,long.MinValue,long.MinValue,DateTimeOffset.UtcNow,CancellationToken.None));
    }
}
