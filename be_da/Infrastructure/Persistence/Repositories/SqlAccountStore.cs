using be_da.Domain.Exceptions;
using be_da.Application.DTOs;
using System.Data;
using Microsoft.Data.SqlClient;
using be_da.Application.Interfaces.Repositories;
using be_da.Application.Services.Accounts;
using be_da.Domain.Entities;

namespace be_da.Infrastructure.Persistence.Repositories;
public sealed class SqlAccountStore(string connectionString, be_da.Application.Interfaces.External.IDataAccessContext? dataScope = null) : IAccountStore
{
    private const string AccountSelect = """
        SELECT u.id, u.email, u.full_name, r.code, u.status, u.google_subject, u.deleted_at, u.venue_id
        FROM dbo.users u JOIN dbo.roles r ON r.id=u.role_id
        """;
    // Older single-center databases do not have personnel assignments yet.
    // Choose the projection before SQL compilation; CASE cannot hide a missing column.
    private static async Task<string> AccountSelectAsync(SqlConnection connection, CancellationToken ct)
    {
        await using var command = new SqlCommand("SELECT COL_LENGTH('dbo.users', 'venue_id');", connection);
        var column = await command.ExecuteScalarAsync(ct);
        return column is null or DBNull ? AccountSelect.Replace("u.venue_id", "CAST(NULL AS bigint) AS venue_id") : AccountSelect;
    }

    public async Task<Account?> FindByGoogleSubjectAsync(string subject, CancellationToken cancellationToken)
    {
        await using var connection = new SqlConnection(connectionString);
        await be_da.Infrastructure.Persistence.SportCenterConnection.OpenAsync(connection, cancellationToken, dataScope);
        await using var command = new SqlCommand(await AccountSelectAsync(connection, cancellationToken) + " WHERE u.google_subject=@subject;", connection);
        command.Parameters.Add("@subject", SqlDbType.VarChar, 255).Value = subject;
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? Read(reader) : null;
    }

    public async Task<Account?> FindByIdAsync(long id, CancellationToken cancellationToken)
    {
        await using var connection = new SqlConnection(connectionString);
        await be_da.Infrastructure.Persistence.SportCenterConnection.OpenAsync(connection, cancellationToken, dataScope);
        await using var command = new SqlCommand(await AccountSelectAsync(connection, cancellationToken) + " WHERE u.id=@id;", connection);
        command.Parameters.Add("@id", SqlDbType.BigInt).Value = id;
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? Read(reader) : null;
    }

    public async Task<Account?> FindByEmailAsync(string email, CancellationToken cancellationToken)
    {
        await using var connection = new SqlConnection(connectionString);
        await be_da.Infrastructure.Persistence.SportCenterConnection.OpenAsync(connection, cancellationToken, dataScope);
        await using var command = new SqlCommand(await AccountSelectAsync(connection, cancellationToken) + " WHERE u.email=@email;", connection);
        command.Parameters.Add("@email", SqlDbType.NVarChar, 320).Value = email;
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? Read(reader) : null;
    }

    public async Task<Account> FindOrRegisterGoogleAsync(string subject, string email, string fullName, CancellationToken cancellationToken)
    {
        await using var connection = new SqlConnection(connectionString);
        await be_da.Infrastructure.Persistence.SportCenterConnection.OpenAsync(connection, cancellationToken, dataScope);
        // Serialize first registration across requests. Unique indexes are the final safety net.
        var accountSelect = await AccountSelectAsync(connection, cancellationToken);
        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        try
        {
            await using var find = new SqlCommand(accountSelect.Replace("dbo.users u JOIN", "dbo.users u WITH (UPDLOCK, HOLDLOCK) JOIN") + " WHERE u.google_subject=@subject;", connection, transaction);
            find.Parameters.Add("@subject", SqlDbType.VarChar, 255).Value = subject;
            Account? existing;
            await using (var reader = await find.ExecuteReaderAsync(cancellationToken))
                existing = await reader.ReadAsync(cancellationToken) ? Read(reader) : null;
            if (existing is not null)
            {
                await transaction.CommitAsync(cancellationToken);
                return existing; // Preserve local email, role and status; provider data cannot promote roles.
            }

            await using var duplicate = new SqlCommand("SELECT COUNT_BIG(*) FROM dbo.users WITH (UPDLOCK, HOLDLOCK) WHERE email=@email;", connection, transaction);
            duplicate.Parameters.Add("@email", SqlDbType.NVarChar, 320).Value = email;
            if ((long)(await duplicate.ExecuteScalarAsync(cancellationToken))! != 0)
                throw new SignInRejectedException("account_link_required");
            await using var insert = new SqlCommand("""
                DECLARE @role bigint = (SELECT id FROM dbo.roles WHERE code='Player');
                IF @role IS NULL THROW 50001, 'Player role is not configured.', 1;
                INSERT dbo.users(role_id,email,full_name,status,google_subject)
                VALUES (@role,@email,@name,'Active',@subject);
                DECLARE @id bigint = CONVERT(bigint,SCOPE_IDENTITY());
                INSERT dbo.players(user_id) VALUES (@id);
                INSERT dbo.account_audit_logs(user_id,actor_id,action,new_value)
                VALUES (@id,@id,'GoogleRegistration','Player');
                SELECT @id;
                """, connection, transaction);
            insert.Parameters.Add("@subject", SqlDbType.VarChar, 255).Value = subject;
            insert.Parameters.Add("@email", SqlDbType.NVarChar, 320).Value = email;
            insert.Parameters.Add("@name", SqlDbType.NVarChar, 255).Value = fullName;
            var id = (long)(await insert.ExecuteScalarAsync(cancellationToken))!;
            await transaction.CommitAsync(cancellationToken);
            return new Account(id, email, fullName, "Player", "Active", subject, false);
        }
        catch (SqlException exception)when (exception.Number is 2601 or 2627 or 1205)
        {
            // Concurrent creation: fail closed. A fresh login can resolve the committed subject.
            throw new SignInRejectedException("sign_in_retry_required");
        }
    }

    private static Account Read(SqlDataReader reader) => new(reader.GetInt64(0), reader.GetString(1), reader.GetString(2), reader.GetString(3), reader.GetString(4), reader.IsDBNull(5) ? "" : reader.GetString(5), !reader.IsDBNull(6), reader.IsDBNull(7) ? null : reader.GetInt64(7));
}
