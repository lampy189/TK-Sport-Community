using System.Data;
using System.Text.Json;
using be_da.Application.DTOs;
using be_da.Application.Interfaces.Repositories;
using Microsoft.Data.SqlClient;
using TKSC.Domain.Exceptions;
using static be_da.Infrastructure.Persistence.Repositories.WorkflowSql;
namespace be_da.Infrastructure.Persistence.Repositories;
public sealed class TechnicalStore(IConfiguration config) : ITechnicalStore
{
    private async Task<SqlConnection> Open(CancellationToken ct)
    {
        var c = new SqlConnection(config.GetConnectionString("Sep490"));
        try { await c.OpenAsync(ct); return c; }
        catch { await c.DisposeAsync(); throw; }
    }
    public async Task<IReadOnlyList<TechnicalSetting>> SettingsAsync(CancellationToken ct)
    {
        await using var c = await Open(ct);
        var rows = await Rows(c, null, "SELECT id,new_value FROM (SELECT id,new_value,ROW_NUMBER() OVER(PARTITION BY JSON_VALUE(new_value,'$.Key') ORDER BY id DESC) n FROM dbo.audit_logs WHERE entity_type='TechnicalSetting' AND ISJSON(new_value)=1) q WHERE n=1", ct);
        return rows.Select(r => { var value = JsonSerializer.Deserialize<TechnicalSetting>(r["new_value"]!.ToString()!)!; return value with { Revision = Convert.ToInt64(r["id"]) }; }).ToArray();
    }
    public async Task SaveSettingAsync(string key, TechnicalSettingChange request, long actor, CancellationToken ct)
    {
        await using var c = await Open(ct); await using var tx = (SqlTransaction)await c.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        await Execute(c, tx, "DECLARE @rc int; EXEC @rc=sys.sp_getapplock @Resource='TKSC:technical-settings',@LockMode='Exclusive',@LockOwner='Transaction',@LockTimeout=10000; IF @rc<0 THROW 51000,'Settings busy',1;", ct);
        var revision = await Scalar(c, tx, "SELECT ISNULL(MAX(id),0) FROM dbo.audit_logs WHERE entity_type='TechnicalSetting' AND ISJSON(new_value)=1 AND JSON_VALUE(new_value,'$.Key')=@key", ct, ("@key", key));
        if (revision != request.Revision) throw new PaymentException(409, "Thiết lập đã thay đổi. Tải lại trước khi lưu.");
        if (request.Enabled && key == "GoodsReservations:ExpiryWorkerEnabled" && await Scalar(c, tx, "SELECT CASE WHEN OBJECT_ID('dbo.goods_stock_reservations','U') IS NOT NULL AND EXISTS(SELECT 1 FROM sys.triggers WHERE name='TR_stock_items_preserve_goods_holds' AND is_disabled=0) THEN 1 ELSE 0 END", ct) != 1) throw new PaymentException(409, "Chưa triển khai đầy đủ giữ tồn hàng hóa.");
        await Execute(c, tx, "INSERT dbo.audit_logs(actor_id,action,entity_type,entity_id,new_value) VALUES(@actor,'technical_setting_changed','TechnicalSetting',0,@value)", ct, ("@actor", actor), ("@value", JsonSerializer.Serialize(new { Key = key, request.Enabled, request.Reason, At = DateTimeOffset.UtcNow })));
        await tx.CommitAsync(ct);
    }
    public async Task<object> QueuesAsync(CancellationToken ct)
    {
        await using var c = await Open(ct);
        // Technical diagnostics expose aggregate delivery states only, never recipients, bodies or gateway payloads.
        return await Rows(c, null, "SELECT channel,status,COUNT_BIG(*) AS count FROM dbo.notifications WHERE channel IN ('email','push') GROUP BY channel,status ORDER BY channel,status", ct);
    }
    public async Task<IReadOnlyList<TechnicalIncident>> IncidentsAsync(CancellationToken ct)
    {
        await using var c = await Open(ct);
        var rows = await Rows(c, null, "SELECT TOP(100) entity_id,id,new_value FROM (SELECT entity_id,id,new_value,ROW_NUMBER() OVER(PARTITION BY entity_id ORDER BY id DESC) n FROM dbo.audit_logs WHERE entity_type='TechnicalIncident' AND ISJSON(new_value)=1) q WHERE n=1 ORDER BY id DESC", ct);
        return rows.Select(r => { var item = JsonSerializer.Deserialize<TechnicalIncident>(r["new_value"]!.ToString()!)!; return item with { Id = Convert.ToInt64(r["entity_id"]), Revision = Convert.ToInt64(r["id"]) }; }).ToArray();
    }
    public async Task SaveIncidentAsync(long? id, TechnicalIncidentChange request, long actor, CancellationToken ct)
    {
        await using var c = await Open(ct); await using var tx = (SqlTransaction)await c.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        if (id.HasValue) {
            var revision = await Scalar(c, tx, "SELECT ISNULL(MAX(id),0) FROM dbo.audit_logs WITH(UPDLOCK,HOLDLOCK) WHERE entity_type='TechnicalIncident' AND entity_id=@id", ct, ("@id", id.Value));
            if (revision == 0) throw new PaymentException(404, "Không tìm thấy sự cố.");
            if (revision != request.Revision) throw new PaymentException(409, "Sự cố đã thay đổi. Tải lại để cập nhật.");
        }
        var payload = JsonSerializer.Serialize(new TechnicalIncident(id ?? 0, 0, request.Title, request.Status, request.Note, DateTimeOffset.UtcNow));
        var newId = await Scalar(c, tx, "INSERT dbo.audit_logs(actor_id,action,entity_type,entity_id,new_value) VALUES(@actor,'technical_incident_updated','TechnicalIncident',@id,@value); SELECT CONVERT(bigint,SCOPE_IDENTITY());", ct, ("@actor", actor), ("@id", id ?? 0), ("@value", payload));
        if (id is null) await Execute(c, tx, "UPDATE dbo.audit_logs SET entity_id=@id WHERE id=@id", ct, ("@id", newId));
        await tx.CommitAsync(ct);
    }
}
