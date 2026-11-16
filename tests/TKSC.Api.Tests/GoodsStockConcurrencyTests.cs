using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using TKSC.Domain.Entities;
using TKSC.Infrastructure.Persistence.Contexts;
using TKSC.Infrastructure.Persistence.Repositories;
using Xunit;

namespace TKSC.Api.Tests;

// Opt-in only: a disposable, already migrated TEST database. Never runs against Sep490.
public sealed class GoodsStockConcurrencyTests
{
    [GoodsStockDatabaseFact]
    public async Task LastItemHasOneWinnerExpiredHoldReleasesAndSalePostsOnce()
    {
        var cs=Environment.GetEnvironmentVariable("TKSC_PAYMENTS_TEST_SQL")!;
        var name=new SqlConnectionStringBuilder(cs).InitialCatalog;
        Assert.Contains("test",name,StringComparison.OrdinalIgnoreCase);
        await using var setup=new SqlConnection(cs);await setup.OpenAsync();
        var tag="PAY05-"+Guid.NewGuid().ToString("N");
        long product=0,stock=0,first=0,second=0,actor=0;
        try
        {
            await using(var cmd=new SqlCommand("""
                DECLARE @actor bigint=(SELECT TOP(1) id FROM dbo.users ORDER BY id),@venue bigint=(SELECT TOP(1) id FROM dbo.venues ORDER BY id);
                IF @actor IS NULL OR @venue IS NULL THROW 51000,'Seed test users and venues first.',1;
                INSERT dbo.products(sku,name,unit_price_vnd,is_active,venue_id) VALUES(@tag,@tag,1000,1,@venue);
                DECLARE @product bigint=SCOPE_IDENTITY();
                INSERT dbo.stock_items(product_id,quantity_on_hand,reorder_level,venue_id) VALUES(@product,1,0,@venue);
                DECLARE @stock bigint=SCOPE_IDENTITY();
                INSERT dbo.orders(order_code,staff_id,total_amount,status,payment_state,handover_state,venue_id) VALUES(@tag+'-1',@actor,1000,'Cart','Unpaid','Pending',@venue);
                DECLARE @first bigint=SCOPE_IDENTITY();
                INSERT dbo.orders(order_code,staff_id,total_amount,status,payment_state,handover_state,venue_id) VALUES(@tag+'-2',@actor,1000,'Cart','Unpaid','Pending',@venue);
                DECLARE @second bigint=SCOPE_IDENTITY();
                INSERT dbo.order_items(order_id,product_id,quantity,unit_price_vnd,subtotal_vnd) VALUES(@first,@product,1,1000,1000),(@second,@product,1,1000,1000);
                SELECT @product,@stock,@first,@second,@actor;
                """,setup))
            {
                cmd.Parameters.AddWithValue("@tag",tag);await using var r=await cmd.ExecuteReaderAsync();Assert.True(await r.ReadAsync());
                product=r.GetInt64(0);stock=r.GetInt64(1);first=r.GetInt64(2);second=r.GetInt64(3);actor=r.GetInt64(4);
            }
            Sep490Context Db()=>new(new DbContextOptionsBuilder<Sep490Context>().UseSqlServer(cs).Options);
            Order Order(long id)=>new(){Id=id,OrderItems=[new(){ProductId=product,Quantity=1,UnitPriceVnd=1000,SubtotalVnd=1000}]};
            var gate=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            async Task<bool> Reserve(long id)
            {
                await gate.Task;
                await using var db=Db();await using var tx=await db.Database.BeginTransactionAsync();
                try
                {
                    await new GoodsPaymentRepository(db,TimeProvider.System).ReserveAsync(Order(id),DateTimeOffset.UtcNow.AddMinutes(15),false,default);
                    await tx.CommitAsync();return true;
                }
                catch(SqlException e) when(e.Number is 51010 or 1205) { await tx.RollbackAsync();return false; }
            }
            var one=Reserve(first);var two=Reserve(second);gate.SetResult();
            var results=await Task.WhenAll(one,two);Assert.Single(results,x=>x);
            var winner=results[0]?first:second;var loser=results[0]?second:first;
            await using(var blocked=new SqlCommand("UPDATE dbo.stock_items SET quantity_on_hand=0 WHERE id=@stock",setup))
            {
                blocked.Parameters.AddWithValue("@stock",stock);
                Assert.Equal(51010,(await Assert.ThrowsAsync<SqlException>(()=>blocked.ExecuteNonQueryAsync())).Number);
            }
            await using(var expire=new SqlCommand("UPDATE dbo.goods_stock_reservations SET expires_at=DATEADD(minute,-1,SYSUTCDATETIME()) WHERE order_id=@id",setup))
            { expire.Parameters.AddWithValue("@id",winner);await expire.ExecuteNonQueryAsync(); }
            await using(var db=Db())
            {
                await using var tx=await db.Database.BeginTransactionAsync();var repo=new GoodsPaymentRepository(db,TimeProvider.System);
                await repo.ReserveAsync(Order(loser),DateTimeOffset.UtcNow.AddMinutes(15),false,default);
                Assert.True(await repo.CommitHoldAsync(Order(loser),default));
                await repo.ConsumeHoldAsync(Order(loser),actor,default);await repo.ConsumeHoldAsync(Order(loser),actor,default);
                await tx.CommitAsync();
            }
            await using(var verify=new SqlCommand("SELECT quantity_on_hand,(SELECT COUNT(*) FROM dbo.stock_movements WHERE stock_item_id=@id AND movement_type='SaleOut') FROM dbo.stock_items WHERE id=@id",setup))
            {
                verify.Parameters.AddWithValue("@id",stock);await using var r=await verify.ExecuteReaderAsync();Assert.True(await r.ReadAsync());Assert.Equal(0,r.GetInt32(0));Assert.Equal(1,r.GetInt32(1));
            }
        }
        finally
        {
            if(product!=0)
            {
                await using var cleanup=new SqlCommand("""
                    DELETE dbo.goods_stock_reservations WHERE order_id IN (@one,@two);
                    DELETE dbo.stock_movements WHERE stock_item_id=@stock;
                    DELETE dbo.order_items WHERE order_id IN (@one,@two);
                    DELETE dbo.orders WHERE id IN (@one,@two);
                    DELETE dbo.stock_items WHERE id=@stock;
                    DELETE dbo.products WHERE id=@product;
                    """,setup);
                cleanup.Parameters.AddWithValue("@one",first);cleanup.Parameters.AddWithValue("@two",second);cleanup.Parameters.AddWithValue("@stock",stock);cleanup.Parameters.AddWithValue("@product",product);
                await cleanup.ExecuteNonQueryAsync();
            }
        }
    }
}
internal sealed class GoodsStockDatabaseFactAttribute : FactAttribute
{
    public GoodsStockDatabaseFactAttribute()
    {
        if(string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("TKSC_PAYMENTS_TEST_SQL")))
            Skip="Requires an explicitly supplied disposable TEST database with migration 018 installed.";
    }
}
