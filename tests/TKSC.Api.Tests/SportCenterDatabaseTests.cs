using be_da.Authorization;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using TKSC.Domain.Entities;
using TKSC.Infrastructure.DbContext;
using Xunit;

namespace TKSC.Api.Tests;
public sealed class SportCenterDatabaseTests
{
    [DatabaseFact]
    public async Task EfConfirmationPostsReceiptWithinTheSameRollbackTransaction()
    {
        var options = new DbContextOptionsBuilder<Sep490Context>()
            .UseSqlServer(Environment.GetEnvironmentVariable("TKSC_TEST_SQL_CONNECTION"))
            .AddInterceptors(new SportCenterConnectionInterceptor()).Options;
        await using var db = new Sep490Context(options);
        await db.Database.OpenConnectionAsync();
        await using var tx = await db.Database.BeginTransactionAsync();
        try
        {
            var actor = await db.Users.Where(u => u.Role.Code == "Staff" && u.Status == "Active" && u.DeletedAt == null).OrderBy(u => u.Id).FirstAsync();
            await db.Database.ExecuteSqlInterpolatedAsync($"DECLARE @v bigint=(SELECT venue_id FROM dbo.users WHERE id={actor.Id}); EXEC sys.sp_set_session_context @key=N'tksc_venue',@value=@v;");
            var order = new Order { OrderCode=$"SC-TEST-{Guid.NewGuid():N}",StaffId=actor.Id,TotalAmount=100,Status="Cart",PaymentState="Unpaid",HandoverState="Pending" };
            db.Orders.Add(order);await db.SaveChangesAsync();
            var payment = new Payment { GoodsOrderId=order.Id,AmountVnd=100,Method="cash",Status="pending",ReferenceCode=$"SC-TEST-{Guid.NewGuid():N}",ConfirmedBy=actor.Id };
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
