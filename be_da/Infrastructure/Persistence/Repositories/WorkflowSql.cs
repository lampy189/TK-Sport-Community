using System.Data;
using Microsoft.Data.SqlClient;
using TKSC.Domain.Exceptions;
namespace be_da.Infrastructure.Persistence.Repositories;

internal static class WorkflowSql
{
    internal static SqlCommand Command(SqlConnection connection, SqlTransaction? tx, string sql, params (string Key, object? Value)[] parameters)
    {
        var command = new SqlCommand(sql, connection, tx);
        foreach (var (key, value) in parameters) command.Parameters.AddWithValue(key, value ?? DBNull.Value);
        return command;
    }
    internal static async Task<List<Dictionary<string, object?>>> Rows(SqlConnection c, SqlTransaction? tx, string sql, CancellationToken ct, params (string, object?)[] p)
    {
        await using var cmd = Command(c, tx, sql, p);
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        var rows = new List<Dictionary<string, object?>>();
        while (await reader.ReadAsync(ct))
        {
            var row = new Dictionary<string, object?>();
            for (var i = 0; i < reader.FieldCount; i++) row[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
            rows.Add(row);
        }
        return rows;
    }
    internal static async Task<int> Execute(SqlConnection c, SqlTransaction? tx, string sql, CancellationToken ct, params (string, object?)[] p)
    {
        await using var cmd = Command(c, tx, sql, p);
        return await cmd.ExecuteNonQueryAsync(ct);
    }
    internal static async Task<long> Scalar(SqlConnection c, SqlTransaction? tx, string sql, CancellationToken ct, params (string, object?)[] p)
    {
        await using var cmd = Command(c, tx, sql, p);
        var value = await cmd.ExecuteScalarAsync(ct);
        return value is null or DBNull ? 0 : Convert.ToInt64(value);
    }
    internal static async Task RequireInvitations(SqlConnection c, SqlTransaction? tx, CancellationToken ct)
    {
        if (await Scalar(c, tx, "SELECT CASE WHEN COL_LENGTH('dbo.invitations','payer_user_id') IS NOT NULL AND COL_LENGTH('dbo.invitations','expires_at') IS NOT NULL AND COL_LENGTH('dbo.invitations','responded_at') IS NOT NULL THEN 1 ELSE 0 END", ct) == 0)
            throw new PaymentException(503, "Lời mời chưa sẵn sàng. Vui lòng liên hệ quản trị để hoàn tất cập nhật dữ liệu.");
    }
    // Used for both invitation preview and atomic acceptance. Existing ticket/order rows are never changed here.
    internal static async Task<string?> Eligibility(SqlConnection c, SqlTransaction? tx, long session, long player, CancellationToken ct)
    {
        const string sql = """
            SELECT CASE
              WHEN u.id IS NULL OR u.deleted_at IS NOT NULL OR LOWER(COALESCE(u.status,''))<>'active' OR r.code<>'Player' THEN N'Tài khoản không hoạt động.'
              WHEN ISNULL(v.is_active,0)<>1 OR ISNULL(cr.is_active,0)<>1 OR LOWER(COALESCE(s.status,'')) NOT IN ('open','published') THEN N'Buổi chơi chưa mở đăng ký.'
              WHEN TODATETIMEOFFSET(DATEADD(second,DATEDIFF(second,CAST('00:00' AS time),ts.start_time),CAST(s.session_date AS datetime2)),'+07:00')<=SYSDATETIMEOFFSET() THEN N'Buổi chơi đã bắt đầu.'
              WHEN EXISTS(SELECT 1 FROM dbo.tickets t JOIN dbo.social_booking_orders o ON o.id=t.order_id
                WHERE t.player_id=@player AND t.session_id=@session AND (LOWER(t.status) IN ('valid','paid','confirmed','checkedin') OR (LOWER(t.status) IN ('pending','reserved') AND o.expires_at>SYSDATETIMEOFFSET()))) THEN N'Đã có vé hoặc đang giữ chỗ.'
              WHEN EXISTS(SELECT 1 FROM dbo.tickets t JOIN dbo.social_booking_orders o ON o.id=t.order_id
                JOIN dbo.sessions other ON other.id=t.session_id JOIN dbo.time_slots ot ON ot.id=other.time_slot_id
                WHERE t.player_id=@player AND other.session_date=s.session_date AND ot.start_time<ts.end_time AND ot.end_time>ts.start_time
                AND (LOWER(t.status) IN ('valid','paid','confirmed','checkedin') OR (LOWER(t.status) IN ('pending','reserved') AND o.expires_at>SYSDATETIMEOFFSET()))) THEN N'Trùng lịch với vé đang có.'
              WHEN (SELECT COUNT(*) FROM dbo.tickets t JOIN dbo.social_booking_orders o ON o.id=t.order_id WHERE t.session_id=s.id AND
                (LOWER(t.status) IN ('valid','paid','confirmed','checkedin') OR (LOWER(t.status) IN ('pending','reserved') AND o.expires_at>SYSDATETIMEOFFSET())))>=s.capacity THEN N'Buổi chơi đã đủ người.'
              WHEN NULLIF(s.band,'') IS NOT NULL AND DATEDIFF(minute,ts.start_time,ts.end_time)>60
                AND NOT EXISTS(SELECT 1 FROM dbo.session_exceptions x WHERE x.session_id=s.id AND x.player_id=@player AND x.approved_by IS NOT NULL)
                AND (psp.current_score IS NULL OR LOWER(COALESCE(psp.rating_status,'')) IN ('pendingsurvey','unrated','')
                  OR COALESCE(NULLIF(psp.current_band,''),CASE WHEN psp.current_score<20 THEN 'B1' WHEN psp.current_score<40 THEN 'B2' WHEN psp.current_score<60 THEN 'B3' WHEN psp.current_score<80 THEN 'B4' ELSE 'B5' END)<>s.band)
                THEN N'Chưa đủ điều kiện trình độ cho môn này.'
              END
            FROM dbo.sessions s JOIN dbo.time_slots ts ON ts.id=s.time_slot_id JOIN dbo.courts cr ON cr.id=s.court_id JOIN dbo.venues v ON v.id=cr.venue_id
            LEFT JOIN dbo.players p ON p.id=@player LEFT JOIN dbo.users u ON u.id=p.user_id LEFT JOIN dbo.roles r ON r.id=u.role_id
            LEFT JOIN dbo.player_sport_profiles psp ON psp.player_id=p.id AND psp.sport_id=s.sport_id WHERE s.id=@session;
            """;
        await using var cmd = Command(c, tx, sql, ("@session", session), ("@player", player));
        var result = await cmd.ExecuteScalarAsync(ct);
        return result is null ? "Không tìm thấy buổi chơi." : result is DBNull ? null : (string)result;
    }
}
