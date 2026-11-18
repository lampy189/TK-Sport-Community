using be_da.Application.DTOs;
using be_da.Application.Interfaces.External;
using be_da.Infrastructure.Persistence.Repositories;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace TKSC.Api.Tests;

public sealed class ShiftTests
{
    private sealed record Scope(string Role, long? VenueId) : IDataAccessContext
    {
        public long? RestrictedVenueId => VenueId;
    }
    private static SqlSessionManagementRepository Repo(string connection, string role, long venue = 1) =>
        new(new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["ConnectionStrings:Sep490"] = connection }).Build(), new Scope(role, venue));

    [Fact]
    public async Task WrongRoleAndForeignCenterAreDeniedBeforeOpeningConnection()
    {
        var repo = Repo("invalid", "Player");
        Assert.Equal(OperationOutcome.Denied, (await repo.AcceptShiftAsync(1, 1, "Player", 1, default)).Outcome);
        Assert.Equal(OperationOutcome.Denied, (await repo.AcceptShiftAsync(2, 1, "Staff", 1, default)).Outcome);
        Assert.Empty(await repo.MyShiftsAsync(2, 1, "Staff", default));
    }

    [ShiftDatabaseFact]
    public async Task AssignmentAcceptanceLifecycleIsScopedIdempotentAndConflictSafe()
    {
        // Only creates and drops a uniquely named disposable database. Never changes the configured app DB.
        var name = "TKSC_ShiftTest_" + Guid.NewGuid().ToString("N");
        var builder = new SqlConnectionStringBuilder(Environment.GetEnvironmentVariable("TKSC_SHIFT_TEST_SERVER")) { InitialCatalog = "master", Pooling = false };
        await using var master = new SqlConnection(builder.ConnectionString);
        await master.OpenAsync();
        await new SqlCommand($"CREATE DATABASE [{name}]", master).ExecuteNonQueryAsync();
        try
        {
            builder.InitialCatalog = name;
            await using var connection = new SqlConnection(builder.ConnectionString);
            await connection.OpenAsync();
            async Task Run(string sql) => await new SqlCommand(sql, connection).ExecuteNonQueryAsync();
            await Run("""
                CREATE TABLE roles(id bigint PRIMARY KEY,code varchar(30));
                CREATE TABLE users(id bigint PRIMARY KEY,full_name nvarchar(100),role_id bigint,venue_id bigint,status varchar(20),deleted_at datetime2 NULL);
                CREATE TABLE sports(id bigint PRIMARY KEY,name nvarchar(100));
                CREATE TABLE courts(id bigint PRIMARY KEY,name nvarchar(100),venue_id bigint,sport_id bigint,is_active bit);
                CREATE TABLE time_slots(id bigint PRIMARY KEY,start_time time,end_time time,label nvarchar(100));
                CREATE TABLE sessions(id bigint PRIMARY KEY,sport_id bigint,court_id bigint,time_slot_id bigint,session_date date,band varchar(100),capacity smallint,price_vnd int,status varchar(30));
                CREATE TABLE session_referees(id bigint IDENTITY PRIMARY KEY,session_id bigint,referee_id bigint,assigned_by bigint,UNIQUE(session_id,referee_id));
                CREATE TABLE session_staff(id bigint IDENTITY PRIMARY KEY,session_id bigint,staff_id bigint,role_in_session nvarchar(100),UNIQUE(session_id,staff_id));
                CREATE TABLE audit_logs(id bigint IDENTITY PRIMARY KEY,actor_id bigint,action varchar(100),entity_type varchar(100),entity_id bigint,old_value nvarchar(max),new_value nvarchar(max));
                CREATE TABLE tickets(id bigint,session_id bigint,order_id bigint,status varchar(30));
                CREATE TABLE social_booking_orders(id bigint,expires_at datetime2);
                INSERT roles VALUES(1,'Manager'),(2,'Referee'),(3,'Staff');
                INSERT users VALUES(1,N'Manager',1,1,'Active',NULL),(2,N'Referee',2,1,'Active',NULL),(3,N'Staff',3,1,'Active',NULL),(4,N'Other staff',3,1,'Active',NULL),(5,N'Other center',3,2,'Active',NULL);
                INSERT sports VALUES(1,N'Badminton');
                INSERT courts VALUES(1,N'Court 1',1,1,1),(2,N'Court 2',1,1,1),(3,N'Foreign court',2,1,1);
                INSERT time_slots VALUES(1,'16:00','18:00',N'16–18'),(2,'18:00','20:00',N'18–20');
                INSERT sessions VALUES(1,1,1,1,'2099-01-01',NULL,8,100000,'Draft'),(2,1,2,1,'2099-01-01',NULL,8,100000,'Draft'),(3,1,3,1,'2099-01-01',NULL,8,100000,'Draft');
                """);
            var manager = Repo(builder.ConnectionString, "Manager");
            var staff = Repo(builder.ConnectionString, "Staff");
            var referee = Repo(builder.ConnectionString, "Referee");
            async Task<bool> Accepted() => (await staff.MyShiftsAsync(1, 3, "Staff", default)).Single().Staff.Single(p => p.Id == 3).Accepted;
            async Task<OperationOutcome> Assign(long session, long[] refs, long[] employees) => (await manager.SetAssignmentsAsync(1, 1, session, new(refs, employees), default)).Outcome;
            Assert.Equal(OperationOutcome.Success, await Assign(1, [2], [3]));
            Assert.False(await Accepted());
            Assert.Equal(OperationOutcome.Denied, (await staff.AcceptShiftAsync(1, 4, "Staff", 1, default)).Outcome);
            Assert.Equal(OperationOutcome.Missing, (await staff.AcceptShiftAsync(1, 3, "Staff", 3, default)).Outcome);
            Assert.Equal(OperationOutcome.Invalid, await Assign(1, [2], [5]));
            Assert.Equal(OperationOutcome.Conflict, await Assign(2, [], [3]));
            Assert.Equal(OperationOutcome.Success, (await staff.AcceptShiftAsync(1, 3, "Staff", 1, default)).Outcome);
            Assert.Equal(OperationOutcome.Success, (await referee.AcceptShiftAsync(1, 2, "Referee", 1, default)).Outcome);
            Assert.True(await Accepted());
            Assert.Equal(OperationOutcome.Success, (await staff.AcceptShiftAsync(1, 3, "Staff", 1, default)).Outcome);
            Assert.Equal(2, Convert.ToInt32(await new SqlCommand("SELECT COUNT(*) FROM audit_logs WHERE action='shift_accepted'", connection).ExecuteScalarAsync()));
            Assert.Equal(OperationOutcome.Success, await Assign(1, [2], [3]));
            Assert.True(await Accepted());
            Assert.Equal(OperationOutcome.Success, await Assign(1, [2], []));
            Assert.Empty(await staff.MyShiftsAsync(1, 3, "Staff", default));
            Assert.Equal(OperationOutcome.Success, await Assign(1, [2], [3]));
            Assert.False(await Accepted());
            await staff.AcceptShiftAsync(1, 3, "Staff", 1, default);
            Assert.Equal(OperationOutcome.Success, (await manager.SaveAsync(1, 1, 1, new(1, 1, 2, new DateOnly(2099, 1, 1), null, 8, 100000), default)).Outcome);
            Assert.False(await Accepted());
            await staff.AcceptShiftAsync(1, 3, "Staff", 1, default);
            Assert.True(await Accepted());
            await Run("UPDATE sessions SET status='Open' WHERE id=1;");
            Assert.Equal(OperationOutcome.Conflict, await Assign(1, [], [3]));
            await Run("UPDATE sessions SET status='cancelled' WHERE id=1;");
            Assert.Equal(OperationOutcome.Conflict, await Assign(1, [2], [3]));
            await Run("DELETE FROM audit_logs WHERE action='shift_accepted';");
            Assert.Equal(OperationOutcome.Conflict, (await staff.AcceptShiftAsync(1, 3, "Staff", 1, default)).Outcome);
            await Run("UPDATE sessions SET status='Draft',session_date='2020-01-01' WHERE id=1;");
            Assert.Equal(OperationOutcome.Conflict, (await staff.AcceptShiftAsync(1, 3, "Staff", 1, default)).Outcome);
        }
        finally
        {
            await new SqlCommand($"ALTER DATABASE [{name}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{name}];", master).ExecuteNonQueryAsync();
        }
    }
}

internal sealed class ShiftDatabaseFactAttribute : FactAttribute
{
    public ShiftDatabaseFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("TKSC_SHIFT_TEST_SERVER")))
            Skip = "Set TKSC_SHIFT_TEST_SERVER to allow creating a disposable shift-test database.";
    }
}
