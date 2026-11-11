using be_da.Infrastructure.Persistence;
using be_da.Presentation.Authorization;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using TKSC.Domain.Entities;
using TKSC.Infrastructure.Persistence.Contexts;
using Xunit;

namespace TKSC.Api.Tests;
[Collection("SQL Server")]
public sealed class SportCenterDatabaseTests
{
    [DatabaseFact]
    public async Task EfConfirmationPostsReceiptWithinTheSameRollbackTransaction()
    {
        var options = new DbContextOptionsBuilder<Sep490Context>()
            .UseSqlServer(Environment.GetEnvironmentVariable("TKSC_TEST_SQL_CONNECTION"))
            .AddInterceptors(new SportCenterConnectionInterceptor(new be_da.Presentation.Authorization.HttpDataAccessContext(new Microsoft.AspNetCore.Http.HttpContextAccessor()))).Options;
        await using var db = new Sep490Context(options);
        await db.Database.OpenConnectionAsync();
        await using var tx = await db.Database.BeginTransactionAsync();
        try
        {
            // Create isolated prerequisites inside this rollback transaction, without relying on a seed email.
            var venue = new Venue { Name = "SQL-ROLLBACK-TEST-" + Guid.NewGuid().ToString("N"), IsActive = true };
            db.Venues.Add(venue); await db.SaveChangesAsync();
            var role = await db.Roles.FirstAsync(r => r.Code == "Staff");
            var actor = new User { Email = $"sql-test-{Guid.NewGuid():N}@example.invalid", FullName = "Rollback test Staff", RoleId = role.Id, Status = "Active" };
            db.Users.Add(actor); await db.SaveChangesAsync();
            await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE dbo.users SET venue_id={venue.Id} WHERE id={actor.Id}");
            await db.Database.ExecuteSqlInterpolatedAsync($"EXEC sys.sp_set_session_context @key=N'tksc_venue',@value={venue.Id}");
            // Create only the prerequisite order through SQL. The behavior under test remains
            // EF payment confirmation and its atomic goods-revenue trigger, not optional stock schema.
            await using var insert = new SqlCommand("""
                SET NOCOUNT ON;
                INSERT dbo.orders(order_code,staff_id,total_amount,status,payment_state,handover_state,venue_id)
                VALUES (@code,@staff,100,'Cart','Unpaid','Pending',@venue);
                SELECT CONVERT(bigint,SCOPE_IDENTITY());
                """, (SqlConnection)db.Database.GetDbConnection(), (SqlTransaction)tx.GetDbTransaction());
            insert.Parameters.AddWithValue("@code", $"SC-TEST-{Guid.NewGuid():N}");
            insert.Parameters.AddWithValue("@staff", actor.Id);
            insert.Parameters.AddWithValue("@venue", venue.Id);
            var orderId = (long)(await insert.ExecuteScalarAsync())!;
            var payment = new Payment { GoodsOrderId=orderId,AmountVnd=100,Method="cash",Status="pending",ReferenceCode=$"SC-TEST-{Guid.NewGuid():N}",ConfirmedBy=actor.Id };
            db.Payments.Add(payment);await db.SaveChangesAsync();
            payment.Status="paid";await db.SaveChangesAsync();
            var count=await db.Database.SqlQueryRaw<int>("SELECT COUNT(*) AS Value FROM dbo.revenue_entries WHERE payment_id={0}",payment.Id).SingleAsync();
            Assert.Equal(1,count);
            payment.ConfirmedBy=actor.Id;db.Entry(payment).Property(p=>p.Status).IsModified=true;await db.SaveChangesAsync();
            Assert.Equal(1,await db.Database.SqlQueryRaw<int>("SELECT COUNT(*) AS Value FROM dbo.revenue_entries WHERE payment_id={0}",payment.Id).SingleAsync());
        }
        finally { await tx.RollbackAsync(); }
    }
}
