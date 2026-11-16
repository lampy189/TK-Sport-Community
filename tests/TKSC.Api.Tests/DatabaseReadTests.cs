using Microsoft.Data.SqlClient;
using be_da.Infrastructure.Persistence;
using Xunit;

namespace TKSC.Api.Tests;

public sealed class DatabaseReadTests
{
    [DatabaseFact]
    public async Task ApprovedGoogleColumnAndAccountQueryWorkWithoutWritingData()
    {
        var connectionString = Environment.GetEnvironmentVariable("TKSC_TEST_SQL_CONNECTION")!;
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = new SqlCommand("""
            SELECT COUNT(*) FROM sys.indexes
            WHERE object_id=OBJECT_ID('dbo.users') AND name='UX_users_google_subject'
              AND is_unique=1 AND has_filter=1;
            """, connection);
        Assert.Equal(1, (int)(await command.ExecuteScalarAsync())!);
        var store = new SqlAccountStore(connectionString);
        // An ID outside the identity-generated range exercises the real SELECT/mapping path.
        Assert.Null(await store.FindByIdAsync(long.MinValue, CancellationToken.None));
    }
}

internal sealed class DatabaseFactAttribute : FactAttribute
{
    public DatabaseFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("TKSC_TEST_SQL_CONNECTION")))
            Skip = "Set TKSC_TEST_SQL_CONNECTION to opt into read-only SQL Server checks.";
    }
}
