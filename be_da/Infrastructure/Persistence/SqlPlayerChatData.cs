using System.Data;
using System.Globalization;
using be_da.Application.Chatbot;
using be_da.Authorization;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace be_da.Infrastructure.Persistence;

public sealed class SqlPlayerChatData(IConfiguration configuration, IOptions<ChatbotOptions> options, TimeProvider clock) : IPlayerChatData
{
    public async Task<ChatData> ReadAsync(ChatLookup lookup, long userId, CancellationToken ct)
    {
        if (userId < 0 || (userId == 0 && lookup.Scope is not ("sessions" or "policies")))
            throw new ArgumentOutOfRangeException(nameof(userId));
        var now = clock.GetUtcNow();
        var today = DateOnly.FromDateTime(now.ToOffset(TimeSpan.FromHours(7)).DateTime);
        var from = lookup.OnDate ?? today;
        var to = lookup.OnDate ?? today.AddDays(6);
        var codes = options.Value.PublicPolicyCodes.Where(c => !string.IsNullOrWhiteSpace(c)).Distinct().Take(100).ToArray();
        if (lookup.Scope == "policies" && codes.Length == 0)
            return new([], false, now, "Chưa cấu hình chính sách SQL được công khai cho Player.");
        var sql = lookup.Scope switch
        {
            "sessions" => SessionsSql,
            "tickets" => TicketsSql,
            "payments" => PaymentsSql,
            "refunds" => RefundsSql,
            "policies" => PoliciesSql.Replace("/*codes*/", string.Join(",", codes.Select((_, i) => "@code" + i))),
            _ => throw new ArgumentException("Unsupported chatbot lookup.")
        };
        var connectionString = configuration.GetConnectionString("Sep490")
            ?? throw new InvalidOperationException("Configure ConnectionStrings:Sep490.");
        await using var connection = new SqlConnection(connectionString);
        await SportCenterConnection.OpenAsync(connection, ct);
        await using var command = new SqlCommand(sql, connection) { CommandTimeout = 10 };
        command.Parameters.Add("@userId", SqlDbType.BigInt).Value = userId;
        command.Parameters.Add("@recordId", SqlDbType.BigInt).Value = (object?)lookup.RecordId ?? DBNull.Value;
        command.Parameters.Add("@sport", SqlDbType.VarChar, 100).Value = (object?)lookup.Sport ?? DBNull.Value;
        command.Parameters.Add("@date", SqlDbType.Date).Value = lookup.OnDate.HasValue ? lookup.OnDate.Value.ToDateTime(TimeOnly.MinValue) : DBNull.Value;
        command.Parameters.Add("@from", SqlDbType.Date).Value = from.ToDateTime(TimeOnly.MinValue);
        command.Parameters.Add("@to", SqlDbType.Date).Value = to.ToDateTime(TimeOnly.MinValue);
        command.Parameters.Add("@now", SqlDbType.DateTimeOffset).Value = now;
        command.Parameters.Add("@center", SqlDbType.BigInt).Value = (object?)CenterSelection.RestrictedVenue ?? DBNull.Value;
        command.Parameters.Add("@after", SqlDbType.Time).Value = lookup.StartFrom.HasValue ? lookup.StartFrom.Value.ToTimeSpan() : DBNull.Value;
        command.Parameters.Add("@before", SqlDbType.Time).Value = lookup.StartBefore.HasValue ? lookup.StartBefore.Value.ToTimeSpan() : DBNull.Value;
        for (var i = 0; i < codes.Length; i++) command.Parameters.Add("@code" + i, SqlDbType.VarChar, 100).Value = codes[i];
        var documents = new List<KnowledgeArticle>();
        await using var reader = await command.ExecuteReaderAsync(ct);
        var rowCount = 0;
        while (await reader.ReadAsync(ct))
        {
            rowCount++;
            if (lookup.Scope != "policies" && rowCount > 20) break;
            var id = reader.GetInt64(0);
            var title = reader.GetString(1);
            var text = reader.GetString(2);
            if (lookup.Scope == "policies")
            {
                // Rebuild from currently approved/effective SQL rows each request: no stale index after revocation.
                for (var offset = 0; offset < text.Length; offset += 1100)
                {
                    var chunk = text.Substring(offset, Math.Min(1200, text.Length - offset));
                    documents.Add(new($"sql-policy-{id}-{offset}", title, $"SQL · policies #{id} · đoạn {offset / 1100 + 1}", title, chunk));
                }
            }
            else
            {
                var table = lookup.Scope == "payments" ? "social_booking_orders" : lookup.Scope;
                documents.Add(new($"sql-{lookup.Scope}-{id}", title, $"SQL · {table} #{id}", title, text));
            }
        }
        var description = lookup.Scope switch
        {
            "sessions" => lookup.RecordId.HasValue ? $"Buổi chơi công khai #{lookup.RecordId}." : $"Buổi chơi công khai từ {from:dd/MM/yyyy} đến {to:dd/MM/yyyy} (giờ Việt Nam).",
            "tickets" => "Vé thuộc người chơi đang đăng nhập; mới nhất trước. Không bao gồm vé đứng tên bạn bè.",
            "payments" => "Đơn social do tài khoản đang đăng nhập thanh toán; mới nhất trước. Không bao gồm đơn hàng hóa.",
            "refunds" => "Hoàn tiền có tài khoản đang đăng nhập là người nhận; mới nhất trước.",
            _ => "Chính sách trong danh sách công khai, có người duyệt và đang hiệu lực."
        };
        if (lookup.Scope != "sessions" && lookup.OnDate.HasValue) description += $" Ngày buổi chơi: {lookup.OnDate:dd/MM/yyyy}.";
        if (lookup.Sport is not null) description += $" Môn: {lookup.Sport}.";
        if (lookup.StartFrom.HasValue) description += $" Giờ bắt đầu từ {lookup.StartFrom:HH:mm}.";
        if (lookup.StartBefore.HasValue) description += $" Giờ bắt đầu trước {lookup.StartBefore:HH:mm}.";
        return new(documents, lookup.Scope != "policies" && rowCount > 20, clock.GetUtcNow(), description);
    }

    private const string SessionsSql = """
        SELECT TOP (21) s.id, CONCAT(N'Buổi #',s.id,N' · ',sp.name),
          CONCAT(N'Ngày ',CONVERT(varchar(10),s.session_date,103),N'; giờ ',CONVERT(varchar(5),ts.start_time,108),N'–',CONVERT(varchar(5),ts.end_time,108),
            N'; sân ',c.name,N'; địa điểm ',v.name,N'; nhóm ',s.band,N'; giá ',s.price_vnd,N' VND; sức chứa ',s.capacity,
            N'; chỗ còn lại ',CASE WHEN s.capacity>occ.n THEN s.capacity-occ.n ELSE 0 END,N'; trạng thái ',s.status,
            N'. Số chỗ có thể thay đổi; hệ thống kiểm tra điều kiện khi đăng ký.')
        FROM dbo.sessions s JOIN dbo.sports sp ON sp.id=s.sport_id
        JOIN dbo.courts c ON c.id=s.court_id JOIN dbo.venues v ON v.id=c.venue_id
        JOIN dbo.time_slots ts ON ts.id=s.time_slot_id
        OUTER APPLY (SELECT COUNT(*) n FROM dbo.tickets t LEFT JOIN dbo.social_booking_orders o ON o.id=t.order_id
            WHERE t.session_id=s.id AND (LOWER(t.status) IN ('valid','paid','confirmed','checkedin')
              OR (LOWER(t.status) IN ('pending','reserved') AND o.expires_at>@now))) occ
        WHERE s.status IN ('Open','Published') AND (@sport IS NULL OR sp.code=@sport)
          AND (@center IS NULL OR c.venue_id=@center)
          AND (@after IS NULL OR ts.start_time>=@after) AND (@before IS NULL OR ts.start_time<@before)
          AND (@recordId IS NULL OR s.id=@recordId)
          AND ((@recordId IS NOT NULL AND @date IS NULL) OR s.session_date BETWEEN @from AND @to)
        ORDER BY s.session_date,ts.start_time,s.id;
        """;

    private const string TicketsSql = """
        SELECT TOP (21) t.id, CONCAT(N'Vé #',t.id,N' · ',sp.name),
          CONCAT(N'Buổi #',s.id,N'; ngày ',CONVERT(varchar(10),s.session_date,103),N'; giờ ',CONVERT(varchar(5),ts.start_time,108),
            N'; trạng thái vé ',t.status,N'; tiền vé ',t.amount_vnd,N' VND.',
            CASE WHEN LOWER(t.status) IN ('pending','reserved') AND (o.expires_at IS NULL OR o.expires_at<=@now)
              THEN N' Hạn giữ chỗ đã hết hoặc chưa xác định; không coi đây là vé được xác nhận.' ELSE N'' END)
        FROM dbo.tickets t JOIN dbo.players p ON p.id=t.player_id
        JOIN dbo.social_booking_orders o ON o.id=t.order_id JOIN dbo.sessions s ON s.id=t.session_id
        JOIN dbo.sports sp ON sp.id=s.sport_id JOIN dbo.time_slots ts ON ts.id=s.time_slot_id
        WHERE p.user_id=@userId AND (@recordId IS NULL OR t.id=@recordId)
          AND (@date IS NULL OR s.session_date=@date) AND (@sport IS NULL OR sp.code=@sport)
        ORDER BY t.id DESC;
        """;

    private const string PaymentsSql = """
        SELECT TOP (21) o.id, CONCAT(N'Đơn social #',o.id),
          CONCAT(N'Tổng đơn ',o.total_amount,N' VND; trạng thái đơn ',o.status,
            N'; hạn thanh toán (UTC) ',COALESCE(CONVERT(varchar(33),SWITCHOFFSET(o.expires_at,'+00:00'),127),N'chưa có'),
            N'; giao dịch gần nhất: ',COALESCE(pay.status,N'chưa ghi nhận'),
            N'; số tiền giao dịch ',COALESCE(CONVERT(varchar(20),pay.amount_vnd),N'chưa có'),N' VND.',
            N' Trạng thái giao dịch không đồng nghĩa đã hoàn tiền. Đây chỉ là dữ liệu ghi nhận, không phải thao tác thanh toán.')
        FROM dbo.social_booking_orders o
        OUTER APPLY (SELECT TOP (1) p.status,p.amount_vnd FROM dbo.payments p
            WHERE p.order_id=o.id AND p.goods_order_id IS NULL ORDER BY p.id DESC) pay
        WHERE o.payer_id=@userId AND (@recordId IS NULL OR o.id=@recordId)
          AND (@date IS NULL OR EXISTS(SELECT 1 FROM dbo.tickets t JOIN dbo.sessions s ON s.id=t.session_id
              WHERE t.order_id=o.id AND s.session_date=@date))
        ORDER BY o.id DESC;
        """;

    private const string RefundsSql = """
        SELECT TOP (21) r.id, CONCAT(N'Hoàn tiền #',r.id),
          CONCAT(N'Vé #',r.ticket_id,N'; buổi #',s.id,N'; trạng thái ',COALESCE(r.status,N'chưa xác định'),
            N'; số tiền ',COALESCE(CONVERT(varchar(20),r.amount_vnd),N'chưa xác định'),
            N' VND. Trạng thái được đọc từ hệ thống; không phải kiểm tra số dư ngân hàng.')
        FROM dbo.refunds r JOIN dbo.tickets t ON t.id=r.ticket_id JOIN dbo.sessions s ON s.id=t.session_id
        JOIN dbo.sports sp ON sp.id=s.sport_id
        WHERE r.recipient_id=@userId AND (@recordId IS NULL OR r.id=@recordId)
          AND (@date IS NULL OR s.session_date=@date) AND (@sport IS NULL OR sp.code=@sport)
        ORDER BY r.id DESC;
        """;

    private const string PoliciesSql = """
        SELECT p.id, CONCAT(p.code,N' · ',p.name), p.value
        FROM dbo.policies p
        WHERE p.code IN (/*codes*/) AND p.approved_by IS NOT NULL
          AND p.effective_from IS NOT NULL AND p.effective_from<=@now
          AND (p.effective_to IS NULL OR p.effective_to>@now)
          AND p.value IS NOT NULL AND LEN(p.value) BETWEEN 1 AND 16000
        ORDER BY p.id;
        """;
}
