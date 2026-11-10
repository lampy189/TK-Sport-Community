using be_da.Authorization;
using be_da.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using TKSC.Infrastructure.DbContext;
using TKSC.Infrastructure.Repositories;
using Xunit;
namespace TKSC.Api.Tests;
[Collection("SQL Server")]
public sealed class LateEntryDatabaseReadTests
{
    [DatabaseFact]
    public async Task ScopedCheckInQueriesCompileAndOptionalUpgradeDoesNotBreakPublicRead()
    {
        var connection=Environment.GetEnvironmentVariable("TKSC_TEST_SQL_CONNECTION")!;
        var options=new DbContextOptionsBuilder<Sep490Context>().UseSqlServer(connection).AddInterceptors(new SportCenterConnectionInterceptor()).Options;
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
