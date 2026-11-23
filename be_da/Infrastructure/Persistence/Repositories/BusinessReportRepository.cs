using System.Data;
using be_da.Application.DTOs;
using be_da.Application.Interfaces.External;
using be_da.Application.Interfaces.Repositories;
using Microsoft.Data.SqlClient;
using TKSC.Domain.Exceptions;
namespace be_da.Infrastructure.Persistence.Repositories;

public sealed class BusinessReportRepository(IConfiguration configuration, IDataAccessContext scope) : IBusinessReportRepository
{
    public async Task<BusinessReport> ReadAsync(DateOnly from, DateOnly to, long? venueId, CancellationToken ct)
    {
        var owner=scope.Role.Equals("owner",StringComparison.OrdinalIgnoreCase);
        if (!owner && (scope.Role.ToLowerInvariant() is not ("staff" or "manager") || scope.VenueId is null || venueId.HasValue && venueId!=scope.VenueId))
            throw new PaymentException(403,"Không có quyền xem báo cáo cơ sở này.");
        venueId=owner?venueId:scope.VenueId;
        await using var c=new SqlConnection(configuration.GetConnectionString("Sep490"));
        await SportCenterConnection.OpenAsync(c,ct,scope);
        // All sections observe one consistent ledger/stock state, including concurrent postings.
        await using var tx=(SqlTransaction)await c.BeginTransactionAsync(IsolationLevel.Serializable,ct);
        await using var cmd=new SqlCommand(RevenueRepository.ReportingEntriesSql + "\n" + """
            SELECT v.id,v.name,
              COALESCE(SUM(CASE WHEN e.entry_type='social_receipt' THEN e.amount_vnd ELSE 0 END),0),
              COALESCE(SUM(CASE WHEN e.entry_type='goods_receipt' THEN e.amount_vnd ELSE 0 END),0),
              COALESCE(SUM(CASE WHEN e.entry_type='refund' THEN e.amount_vnd ELSE 0 END),0)
            FROM dbo.venues v LEFT JOIN reporting_entries e ON e.venue_id=v.id AND e.recognized_at>=@start AND e.recognized_at<@end
            WHERE @venue IS NULL OR v.id=@venue GROUP BY v.id,v.name ORDER BY v.id;
            SELECT p.venue_id,p.id,p.name,
              CONVERT(bigint,si.quantity_on_hand)-COALESCE(SUM(CASE WHEN m.created_at>=@start THEN CONVERT(bigint,m.quantity) ELSE 0 END),0),
              COALESCE(SUM(CASE WHEN m.created_at>=@start AND m.created_at<@end AND m.quantity>0 THEN CONVERT(bigint,m.quantity) ELSE 0 END),0),
              COALESCE(SUM(CASE WHEN m.created_at>=@start AND m.created_at<@end AND m.quantity<0 THEN -CONVERT(bigint,m.quantity) ELSE 0 END),0),
              CONVERT(bigint,si.quantity_on_hand)-COALESCE(SUM(CASE WHEN m.created_at>=@end THEN CONVERT(bigint,m.quantity) ELSE 0 END),0),
              CONVERT(bigint,si.quantity_on_hand),COUNT_BIG(CASE WHEN m.id IS NOT NULL AND m.created_at IS NULL THEN 1 END)
            FROM dbo.products p JOIN dbo.stock_items si ON si.product_id=p.id
            LEFT JOIN dbo.stock_movements m ON m.stock_item_id=si.id
            WHERE @venue IS NULL OR p.venue_id=@venue GROUP BY p.venue_id,p.id,p.name,si.quantity_on_hand ORDER BY p.venue_id,p.id;
            -- Session-date cohort: allocation is not consumption; only the final staff reconciliation supplies used quantity.
            SELECT c.venue_id,s.id,si.product_id,
              SUM(CASE WHEN m.movement_type='SessionIssue' THEN -CONVERT(bigint,m.quantity) ELSE 0 END),
              SUM(CASE WHEN m.movement_type='SessionReturn' THEN CONVERT(bigint,m.quantity) ELSE 0 END),
              MAX(TRY_CONVERT(bigint,JSON_VALUE(CASE WHEN ISJSON(a.new_value)=1 THEN a.new_value ELSE '{}' END,'$.used')))
            FROM dbo.sessions s JOIN dbo.courts c ON c.id=s.court_id
            JOIN dbo.stock_movements m ON m.reference_type='Session' AND m.reference_id=s.id AND m.movement_type IN ('SessionIssue','SessionReturn')
            JOIN dbo.stock_items si ON si.id=m.stock_item_id
            OUTER APPLY(SELECT TOP(1) al.new_value FROM dbo.audit_logs al WHERE al.entity_type='SessionSupply' AND al.entity_id=s.id
              AND al.action='supply_reconciled' AND TRY_CONVERT(bigint,JSON_VALUE(CASE WHEN ISJSON(al.new_value)=1 THEN al.new_value ELSE '{}' END,'$.productId'))=si.product_id ORDER BY al.id DESC) a
            WHERE s.session_date>=@from AND s.session_date<=@to AND (@venue IS NULL OR c.venue_id=@venue)
            GROUP BY c.venue_id,s.id,si.product_id ORDER BY c.venue_id,s.id,si.product_id;
            SELECT venue_id,id,shift_date,status,CONVERT(bigint,COALESCE(expected_bank,0)),CONVERT(bigint,COALESCE(actual_bank,0)),CONVERT(bigint,COALESCE(difference,0))
            FROM dbo.shift_reconciliations WHERE shift_date>=@from AND shift_date<=@to AND (@venue IS NULL OR venue_id=@venue)
            ORDER BY venue_id,shift_date,id;
            -- Current backlog, not historical period turnover. Legacy sources have no event timestamp.
            WITH payment_centers AS (
              SELECT p.id,p.status,p.amount_vnd,COALESCE(o.venue_id,v.venue_id) venue_id
              FROM dbo.payments p LEFT JOIN dbo.orders o ON o.id=p.goods_order_id
              OUTER APPLY(SELECT MIN(c.venue_id) venue_id FROM dbo.tickets t JOIN dbo.sessions s ON s.id=t.session_id
                JOIN dbo.courts c ON c.id=s.court_id WHERE t.order_id=p.order_id HAVING COUNT(DISTINCT c.venue_id)=1) v
            )
            SELECT p.venue_id,p.id,CAST(NULL AS bigint),'unposted_receipt',CONVERT(bigint,p.amount_vnd)
              FROM payment_centers p WHERE LOWER(p.status)='paid' AND p.venue_id IS NOT NULL AND (@venue IS NULL OR p.venue_id=@venue)
              AND NOT EXISTS(SELECT 1 FROM dbo.revenue_entries e WHERE e.payment_id=p.id AND e.refund_id IS NULL)
            UNION ALL
            SELECT p.venue_id,p.id,r.id,'pending_refund',CONVERT(bigint,COALESCE(r.amount_vnd,0))
              FROM payment_centers p JOIN dbo.refunds r ON r.payment_id=p.id
              WHERE LOWER(COALESCE(r.status,'')) NOT IN ('completed','rejected','cancelled')
              AND p.venue_id IS NOT NULL AND (@venue IS NULL OR p.venue_id=@venue);
            """,c,tx);
        cmd.Parameters.AddWithValue("@start",new DateTimeOffset(from.ToDateTime(TimeOnly.MinValue),TimeSpan.FromHours(7)));
        cmd.Parameters.AddWithValue("@end",new DateTimeOffset(to.AddDays(1).ToDateTime(TimeOnly.MinValue),TimeSpan.FromHours(7)));
        cmd.Parameters.AddWithValue("@from",from.ToDateTime(TimeOnly.MinValue));cmd.Parameters.AddWithValue("@to",to.ToDateTime(TimeOnly.MinValue));
        cmd.Parameters.Add("@venue",SqlDbType.BigInt).Value=(object?)venueId??DBNull.Value;
        var centers=new List<CenterRevenue>();var inventory=new List<InventoryReport>();var supplies=new List<SupplyReport>();var reconciliations=new List<OutstandingReport>();
        var pending=new List<PendingMoney>();
        await using(var r=await cmd.ExecuteReaderAsync(ct))
        {
            while(await r.ReadAsync(ct)) centers.Add(new(r.GetInt64(0),r.GetString(1),r.GetInt64(2),r.GetInt64(3),r.GetInt64(4)));
            if(venueId.HasValue && centers.Count==0) throw new PaymentException(404,"Không tìm thấy cơ sở.");
            await r.NextResultAsync(ct);
            while(await r.ReadAsync(ct)) inventory.Add(new(r.GetInt64(0),r.GetInt64(1),r.GetString(2),r.GetInt64(3),r.GetInt64(4),r.GetInt64(5),r.GetInt64(6),r.GetInt64(7),r.GetInt64(8)));
            await r.NextResultAsync(ct);
            while(await r.ReadAsync(ct)) supplies.Add(new(r.GetInt64(0),r.GetInt64(1),r.GetInt64(2),r.GetInt64(3),r.GetInt64(4),r.IsDBNull(5)?null:r.GetInt64(5)));
            await r.NextResultAsync(ct);
            while(await r.ReadAsync(ct)) reconciliations.Add(new(r.GetInt64(0),r.GetInt64(1),r.IsDBNull(2)?null:DateOnly.FromDateTime(r.GetDateTime(2)),r.IsDBNull(3)?null:r.GetString(3),r.GetInt64(4),r.GetInt64(5),r.GetInt64(6)));
            await r.NextResultAsync(ct);
            while(await r.ReadAsync(ct)) pending.Add(new(r.GetInt64(0),r.GetInt64(1),r.IsDBNull(2)?null:r.GetInt64(2),r.GetString(3),r.GetInt64(4)));
        }
        await tx.CommitAsync(ct);
        return new(from,to,venueId,centers,inventory,supplies,reconciliations,pending);
    }
}
