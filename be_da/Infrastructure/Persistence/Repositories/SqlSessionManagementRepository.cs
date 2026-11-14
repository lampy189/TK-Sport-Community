using System.Data;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using be_da.Application.DTOs;
using be_da.Application.Interfaces.External;
using be_da.Application.Interfaces.Repositories;
using be_da.Infrastructure.Persistence;

namespace be_da.Infrastructure.Persistence.Repositories;

public sealed class SqlSessionManagementRepository(IConfiguration configuration, IDataAccessContext? dataScope = null) : ISessionManagementRepository
{
    private readonly string connectionString = configuration.GetConnectionString("Sep490")
        ?? throw new InvalidOperationException("Configure ConnectionStrings:Sep490.");

    public async Task<SessionManagementOptions> GetOptionsAsync(long venueId, CancellationToken ct)
    {
        if (!InScope(venueId)) return new([], [], [], []);
        await using var connection = await OpenAsync(ct);
        var sports = new List<SessionSportOption>();
        await using (var command = new SqlCommand("SELECT id,name FROM dbo.sports ORDER BY name,id;", connection))
        await using (var reader = await command.ExecuteReaderAsync(ct))
            while (await reader.ReadAsync(ct)) sports.Add(new(reader.GetInt64(0), reader.GetString(1)));

        var courts = new List<SessionCourtOption>();
        await using (var command = new SqlCommand("SELECT id,name,sport_id FROM dbo.courts WHERE venue_id=@venue AND ISNULL(is_active,0)=1 ORDER BY name,id;", connection))
        {
            command.Parameters.Add("@venue", SqlDbType.BigInt).Value = venueId;
            await using var reader = await command.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct)) courts.Add(new(reader.GetInt64(0), reader.IsDBNull(1) ? "Sân" : reader.GetString(1), reader.IsDBNull(2) ? null : reader.GetInt64(2)));
        }

        var timeSlots = new List<TimeSlotSummary>();
        await using (var command = new SqlCommand("SELECT id,start_time,end_time,label FROM dbo.time_slots ORDER BY start_time,id;", connection))
        await using (var reader = await command.ExecuteReaderAsync(ct))
            while (await reader.ReadAsync(ct))
            {
                var start = reader.GetTimeSpan(1);
                var end = reader.GetTimeSpan(2);
                timeSlots.Add(new(reader.GetInt64(0), start, end, reader.IsDBNull(3) ? $"{start:hh\\:mm} - {end:hh\\:mm}" : reader.GetString(3)));
            }

        var personnel = new List<SessionPersonnelOption>();
        var hasVenueColumn = await HasUserVenueColumnAsync(connection, ct);
        var scopeFilter = hasVenueColumn ? " AND u.venue_id=@venue" : string.Empty;
        await using (var command = new SqlCommand($"""
            SELECT u.id,u.full_name,r.code
            FROM dbo.users u JOIN dbo.roles r ON r.id=u.role_id
            WHERE u.deleted_at IS NULL AND LOWER(u.status)='active' AND r.code IN ('Referee','Staff'){scopeFilter}
            ORDER BY u.full_name,u.id;
            """, connection))
        {
            if (hasVenueColumn) command.Parameters.Add("@venue", SqlDbType.BigInt).Value = venueId;
            await using var reader = await command.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct)) personnel.Add(new(reader.GetInt64(0), reader.GetString(1), reader.GetString(2)));
        }

        return new(sports, courts, timeSlots, personnel);
    }

    public async Task<IReadOnlyList<ManagedSession>> GetSessionsAsync(long venueId, CancellationToken ct)
    {
        if (!InScope(venueId)) return [];
        await using var connection = await OpenAsync(ct);
        var sessions = new List<ManagedSession>();
        const string sql = """
            SELECT s.id,s.sport_id,sp.name,s.court_id,ISNULL(c.name,N'Sân'),s.time_slot_id,ts.start_time,ts.end_time,
                   s.session_date,s.band,s.capacity,s.price_vnd,s.status,
                   ISNULL(occupancy.places,0)
            FROM dbo.sessions s JOIN dbo.sports sp ON sp.id=s.sport_id
            JOIN dbo.courts c ON c.id=s.court_id JOIN dbo.time_slots ts ON ts.id=s.time_slot_id
            OUTER APPLY (
                SELECT COUNT(*) AS places FROM dbo.tickets t
                LEFT JOIN dbo.social_booking_orders o ON o.id=t.order_id
                WHERE t.session_id=s.id AND (
                    LOWER(t.status) IN ('valid','paid','confirmed','checkedin')
                    OR (LOWER(t.status) IN ('pending','reserved') AND o.expires_at>SYSUTCDATETIME()))
            ) occupancy
            WHERE c.venue_id=@venue
            ORDER BY s.session_date DESC,ts.start_time DESC,s.id DESC;
            """;
        await using (var command = new SqlCommand(sql, connection))
        {
            command.Parameters.Add("@venue", SqlDbType.BigInt).Value = venueId;
            await using var reader = await command.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
                sessions.Add(new(reader.GetInt64(0), reader.GetInt64(1), reader.GetString(2), reader.GetInt64(3), reader.GetString(4),
                    reader.GetInt64(5), TimeOnly.FromTimeSpan(reader.GetTimeSpan(6)), TimeOnly.FromTimeSpan(reader.GetTimeSpan(7)),
                    DateOnly.FromDateTime(reader.GetDateTime(8)), reader.IsDBNull(9) ? null : reader.GetString(9), reader.GetInt16(10),
                    reader.GetInt32(11), reader.GetString(12), Convert.ToInt16(reader.GetInt32(13)), [], []));
        }

        if (sessions.Count == 0) return sessions;
        var ids = sessions.Select(s => s.Id).ToArray();
        var referees = await LoadAssigneesAsync(connection, "session_referees", "referee_id", ids, ct);
        var staff = await LoadAssigneesAsync(connection, "session_staff", "staff_id", ids, ct);
        return sessions.Select(s => s with
        {
            Referees = referees.GetValueOrDefault(s.Id) ?? [],
            Staff = staff.GetValueOrDefault(s.Id) ?? []
        }).ToList();
    }

    public async Task<OperationResult> SaveAsync(long venueId, long actorId, long? sessionId, SaveManagedSessionRequest request, CancellationToken ct)
    {
        if (!InScope(venueId)) return Denied();
        await using var connection = await OpenAsync(ct);
        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        try
        {
            var slot = await ReadSlotAndValidateCourtAsync(connection, transaction, venueId, request, ct);
            if (slot is null) return await RollbackAsync(transaction, NotFound("Sân, môn hoặc khung giờ không thuộc cơ sở đang chọn."), ct);
            if (!IsFuture(request.SessionDate, slot.Value.Start))
                return await RollbackAsync(transaction, Invalid("SESSION_IN_PAST", "Giờ bắt đầu buổi phải ở tương lai."), ct);
            if (await HasCourtConflictAsync(connection, transaction, request.CourtId, request.SessionDate, slot.Value.Start, slot.Value.End, sessionId, ct))
                return await RollbackAsync(transaction, Conflict("COURT_CONFLICT", "Sân đã có buổi khác trong khoảng giờ này."), ct);

            if (sessionId.HasValue)
            {
                var existing = await ReadExistingAsync(connection, transaction, venueId, sessionId.Value, ct);
                if (existing is null) return await RollbackAsync(transaction, NotFound("Không tìm thấy buổi chơi tại cơ sở này."), ct);
                var occupied = await OccupiedAsync(connection, transaction, sessionId.Value, ct);
                if (request.Capacity < occupied)
                    return await RollbackAsync(transaction, Conflict("CAPACITY_BELOW_OCCUPANCY", "Sức chứa không thể thấp hơn số vé đang giữ hoặc đã xác nhận."), ct);
                var criticalChange = existing.Value.SportId != request.SportId || existing.Value.CourtId != request.CourtId
                    || existing.Value.SlotId != request.TimeSlotId || existing.Value.Date != request.SessionDate
                    || existing.Value.Price != request.PriceVnd || !string.Equals(existing.Value.Band, request.Band, StringComparison.Ordinal);
                if (occupied > 0 && criticalChange)
                    return await RollbackAsync(transaction, Conflict("SESSION_HAS_TICKETS", "Không thể đổi môn, sân, giờ, trình độ hoặc giá khi buổi đã có người giữ vé."), ct);
                if (existing.Value.Status is "cancelled" or "completed")
                    return await RollbackAsync(transaction, Conflict("SESSION_NOT_EDITABLE", "Buổi đã đóng hoặc kết thúc, không thể sửa thông tin."), ct);
                var referees = await AssignedIdsAsync(connection, transaction, "session_referees", "referee_id", sessionId.Value, ct);
                var staff = await AssignedIdsAsync(connection, transaction, "session_staff", "staff_id", sessionId.Value, ct);
                if (await HasPersonnelConflictAsync(connection, transaction, "session_referees", "referee_id", sessionId.Value, request.SessionDate, slot.Value.Start, slot.Value.End, referees, ct)
                    || await HasPersonnelConflictAsync(connection, transaction, "session_staff", "staff_id", sessionId.Value, request.SessionDate, slot.Value.Start, slot.Value.End, staff, ct))
                    return await RollbackAsync(transaction, Conflict("PERSONNEL_CONFLICT", "Khung giờ mới trùng lịch của nhân sự đang được phân công."), ct);

                await using var update = new SqlCommand("""
                    UPDATE dbo.sessions SET sport_id=@sport,court_id=@court,time_slot_id=@slot,session_date=@date,band=@band,capacity=@capacity,price_vnd=@price
                    WHERE id=@id;
                    """, connection, transaction);
                AddSessionParameters(update, venueId, request);
                update.Parameters.Add("@id", SqlDbType.BigInt).Value = sessionId.Value;
                await update.ExecuteNonQueryAsync(ct);
                await AuditAsync(connection, transaction, actorId, sessionId.Value, "session_updated", existing.Value, request, ct);
                await transaction.CommitAsync(ct);
                return OperationResult.Success((await FindAsync(connection, sessionId.Value, ct)));
            }

            await using var insert = new SqlCommand("""
                INSERT dbo.sessions(sport_id,court_id,time_slot_id,session_date,band,capacity,price_vnd,status,created_by)
                VALUES(@sport,@court,@slot,@date,@band,@capacity,@price,'Draft',@actor);
                SELECT CONVERT(bigint,SCOPE_IDENTITY());
                """, connection, transaction);
            AddSessionParameters(insert, venueId, request);
            insert.Parameters.Add("@actor", SqlDbType.BigInt).Value = actorId;
            var id = Convert.ToInt64(await insert.ExecuteScalarAsync(ct));
            await AuditAsync(connection, transaction, actorId, id, "session_created", null, request, ct);
            await transaction.CommitAsync(ct);
            return OperationResult.Created(await FindAsync(connection, id, ct));
        }
        catch (SqlException ex) when (ex.Number is 2601 or 2627)
        {
            await transaction.RollbackAsync(CancellationToken.None);
            return Conflict("SESSION_CONFLICT", "Buổi bị trùng tài nguyên. Hãy tải lại lịch và thử lại.");
        }
    }

    public async Task<OperationResult> SetAssignmentsAsync(long venueId, long actorId, long sessionId, SessionAssignmentsRequest request, CancellationToken ct)
    {
        if (!InScope(venueId)) return Denied();
        await using var connection = await OpenAsync(ct);
        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        try
        {
            var slot = await ReadSessionSlotAsync(connection, transaction, venueId, sessionId, ct);
            if (slot is null) return await RollbackAsync(transaction, NotFound("Không tìm thấy buổi chơi tại cơ sở này."), ct);
            if (!IsFuture(slot.Value.Date, slot.Value.Start))
                return await RollbackAsync(transaction, Conflict("SESSION_STARTED", "Không thể thay đổi nhân sự sau khi buổi bắt đầu."), ct);
            var referees = request.RefereeIds ?? [];
            var staff = request.StaffIds ?? [];
            var hasVenueColumn = await HasUserVenueColumnAsync(connection, ct, transaction);
            if (!await ValidatePersonnelAsync(connection, transaction, venueId, "Referee", referees, hasVenueColumn, ct)
                || !await ValidatePersonnelAsync(connection, transaction, venueId, "Staff", staff, hasVenueColumn, ct))
                return await RollbackAsync(transaction, Invalid("PERSONNEL_NOT_ASSIGNED", "Chỉ phân công nhân sự đang hoạt động, đúng vai trò và thuộc cơ sở này."), ct);
            if (await HasPersonnelConflictAsync(connection, transaction, "session_referees", "referee_id", sessionId, slot.Value.Date, slot.Value.Start, slot.Value.End, referees, ct)
                || await HasPersonnelConflictAsync(connection, transaction, "session_staff", "staff_id", sessionId, slot.Value.Date, slot.Value.Start, slot.Value.End, staff, ct))
                return await RollbackAsync(transaction, Conflict("PERSONNEL_CONFLICT", "Nhân sự đã được phân công vào buổi khác trùng giờ."), ct);

            var before = new { referees = await AssignedIdsAsync(connection, transaction, "session_referees", "referee_id", sessionId, ct), staff = await AssignedIdsAsync(connection, transaction, "session_staff", "staff_id", sessionId, ct) };
            await ExecuteAsync(connection, transaction, "DELETE dbo.session_referees WHERE session_id=@id; DELETE dbo.session_staff WHERE session_id=@id;", ct, ("@id", sessionId));
            foreach (var id in referees)
                await ExecuteAsync(connection, transaction, "INSERT dbo.session_referees(session_id,referee_id,assigned_by) VALUES(@session,@user,@actor);", ct, ("@session", sessionId), ("@user", id), ("@actor", actorId));
            foreach (var id in staff)
                await ExecuteAsync(connection, transaction, "INSERT dbo.session_staff(session_id,staff_id) VALUES(@session,@user);", ct, ("@session", sessionId), ("@user", id));
            await AuditAsync(connection, transaction, actorId, sessionId, "session_assignments_updated", before, new { referees, staff }, ct);
            await transaction.CommitAsync(ct);
            return OperationResult.Success(await FindAsync(connection, sessionId, ct));
        }
        catch (SqlException ex) when (ex.Number is 2601 or 2627)
        {
            await transaction.RollbackAsync(CancellationToken.None);
            return Conflict("PERSONNEL_CONFLICT", "Phân công trùng. Hãy tải lại lịch và thử lại.");
        }
    }

    public async Task<OperationResult> SetStatusAsync(long venueId, long actorId, long sessionId, bool open, CancellationToken ct)
    {
        if (!InScope(venueId)) return Denied();
        await using var connection = await OpenAsync(ct);
        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var slot = await ReadSessionSlotAsync(connection, transaction, venueId, sessionId, ct);
        if (slot is null) return await RollbackAsync(transaction, NotFound("Không tìm thấy buổi chơi tại cơ sở này."), ct);
        if (!open)
        {
            var status = await ScalarStringAsync(connection, transaction, "SELECT status FROM dbo.sessions WITH(UPDLOCK,HOLDLOCK) WHERE id=@id;", ct, ("@id", sessionId));
            if (!string.Equals(status, "Open", StringComparison.OrdinalIgnoreCase))
                return await RollbackAsync(transaction, string.Equals(status, "Closed", StringComparison.OrdinalIgnoreCase) ? OperationResult.Success(await FindAsync(connection, sessionId, ct)) : Conflict("SESSION_NOT_OPEN", "Chỉ đóng đăng ký cho buổi đang mở."), ct);
            await ExecuteAsync(connection, transaction, "UPDATE dbo.sessions SET status='Closed' WHERE id=@id;", ct, ("@id", sessionId));
            await AuditAsync(connection, transaction, actorId, sessionId, "session_registration_closed", new { status }, new { status = "Closed" }, ct);
            await transaction.CommitAsync(ct);
            return OperationResult.Success(await FindAsync(connection, sessionId, ct));
        }

        var current = await ScalarStringAsync(connection, transaction, "SELECT status FROM dbo.sessions WITH(UPDLOCK,HOLDLOCK) WHERE id=@id;", ct, ("@id", sessionId));
        if (string.Equals(current, "Open", StringComparison.OrdinalIgnoreCase))
            return await RollbackAsync(transaction, OperationResult.Success(await FindAsync(connection, sessionId, ct)), ct);
        if (current is not ("Draft" or "Closed"))
            return await RollbackAsync(transaction, Conflict("SESSION_NOT_OPENABLE", "Trạng thái hiện tại không thể mở đăng ký."), ct);
        if (!IsFuture(slot.Value.Date, slot.Value.Start))
            return await RollbackAsync(transaction, Invalid("SESSION_IN_PAST", "Chỉ mở đăng ký cho buổi có giờ bắt đầu trong tương lai."), ct);
        var refereeIds = await AssignedIdsAsync(connection, transaction, "session_referees", "referee_id", sessionId, ct);
        var staffIds = await AssignedIdsAsync(connection, transaction, "session_staff", "staff_id", sessionId, ct);
        if (refereeIds.Length == 0)
            return await RollbackAsync(transaction, Invalid("REFEREE_REQUIRED", "Phải phân công ít nhất một trọng tài trước khi mở đăng ký."), ct);
        var hasVenueColumn = await HasUserVenueColumnAsync(connection, ct, transaction);
        if (!await ValidatePersonnelAsync(connection, transaction, venueId, "Referee", refereeIds, hasVenueColumn, ct)
            || !await ValidatePersonnelAsync(connection, transaction, venueId, "Staff", staffIds, hasVenueColumn, ct))
            return await RollbackAsync(transaction, Conflict("PERSONNEL_NOT_ASSIGNED", "Nhân sự được phân công không còn thuộc cơ sở hoặc không còn hoạt động."), ct);
        if (await HasCourtConflictAsync(connection, transaction, slot.Value.CourtId, slot.Value.Date, slot.Value.Start, slot.Value.End, sessionId, ct)
            || await HasPersonnelConflictAsync(connection, transaction, "session_referees", "referee_id", sessionId, slot.Value.Date, slot.Value.Start, slot.Value.End, refereeIds, ct)
            || await HasPersonnelConflictAsync(connection, transaction, "session_staff", "staff_id", sessionId, slot.Value.Date, slot.Value.Start, slot.Value.End, staffIds, ct))
            return await RollbackAsync(transaction, Conflict("RESOURCE_CONFLICT", "Sân hoặc nhân sự đã có lịch trùng trong khoảng giờ này."), ct);

        await ExecuteAsync(connection, transaction, "UPDATE dbo.sessions SET status='Open' WHERE id=@id;", ct, ("@id", sessionId));
        await AuditAsync(connection, transaction, actorId, sessionId, "session_registration_opened", new { status = current }, new { status = "Open" }, ct);
        await transaction.CommitAsync(ct);
        return OperationResult.Success(await FindAsync(connection, sessionId, ct));
    }

    private async Task<Dictionary<long, IReadOnlyList<SessionAssignee>>> LoadAssigneesAsync(SqlConnection connection, string table, string userColumn, long[] ids, CancellationToken ct)
    {
        var result = new Dictionary<long, IReadOnlyList<SessionAssignee>>();
        var idList = string.Join(",", ids);
        var sql = $"SELECT a.session_id,u.id,u.full_name FROM dbo.{table} a JOIN dbo.users u ON u.id=a.{userColumn} WHERE a.session_id IN ({idList}) ORDER BY u.full_name,u.id;";
        var grouped = new Dictionary<long, List<SessionAssignee>>();
        await using var command = new SqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            var sessionId = reader.GetInt64(0);
            if (!grouped.TryGetValue(sessionId, out var people)) grouped[sessionId] = people = [];
            people.Add(new(reader.GetInt64(1), reader.GetString(2)));
        }
        foreach (var item in grouped) result[item.Key] = item.Value;
        return result;
    }

    private async Task<ManagedSession?> FindAsync(SqlConnection connection, long id, CancellationToken ct)
    {
        var rows = await ReadSessionsByIdsAsync(connection, [id], ct);
        return rows.SingleOrDefault();
    }

    private async Task<List<ManagedSession>> ReadSessionsByIdsAsync(SqlConnection connection, long[] ids, CancellationToken ct)
    {
        if (ids.Length == 0) return [];
        var sessions = new List<ManagedSession>();
        var idList = string.Join(",", ids);
        var sql = $"""
            SELECT s.id,s.sport_id,sp.name,s.court_id,ISNULL(c.name,N'Sân'),s.time_slot_id,ts.start_time,ts.end_time,s.session_date,s.band,s.capacity,s.price_vnd,s.status,
                   (SELECT COUNT(*) FROM dbo.tickets t LEFT JOIN dbo.social_booking_orders o ON o.id=t.order_id WHERE t.session_id=s.id AND (LOWER(t.status) IN ('valid','paid','confirmed','checkedin') OR (LOWER(t.status) IN ('pending','reserved') AND o.expires_at>SYSUTCDATETIME())))
            FROM dbo.sessions s JOIN dbo.sports sp ON sp.id=s.sport_id JOIN dbo.courts c ON c.id=s.court_id JOIN dbo.time_slots ts ON ts.id=s.time_slot_id
            WHERE s.id IN ({idList}) ORDER BY s.session_date,ts.start_time;
            """;
        await using (var command = new SqlCommand(sql, connection))
        await using (var reader = await command.ExecuteReaderAsync(ct))
            while (await reader.ReadAsync(ct))
                sessions.Add(new(reader.GetInt64(0), reader.GetInt64(1), reader.GetString(2), reader.GetInt64(3), reader.GetString(4), reader.GetInt64(5),
                    TimeOnly.FromTimeSpan(reader.GetTimeSpan(6)), TimeOnly.FromTimeSpan(reader.GetTimeSpan(7)), DateOnly.FromDateTime(reader.GetDateTime(8)),
                    reader.IsDBNull(9) ? null : reader.GetString(9), reader.GetInt16(10), reader.GetInt32(11), reader.GetString(12), Convert.ToInt16(reader.GetInt32(13)), [], []));
        var referees = await LoadAssigneesAsync(connection, "session_referees", "referee_id", ids, ct);
        var staff = await LoadAssigneesAsync(connection, "session_staff", "staff_id", ids, ct);
        return sessions.Select(s => s with { Referees = referees.GetValueOrDefault(s.Id) ?? [], Staff = staff.GetValueOrDefault(s.Id) ?? [] }).ToList();
    }

    private async Task<(TimeSpan Start, TimeSpan End)?> ReadSlotAndValidateCourtAsync(SqlConnection connection, SqlTransaction transaction, long venueId, SaveManagedSessionRequest request, CancellationToken ct)
    {
        await using var command = new SqlCommand("""
            SELECT ts.start_time,ts.end_time FROM dbo.courts c WITH(UPDLOCK,HOLDLOCK)
            JOIN dbo.time_slots ts WITH(UPDLOCK,HOLDLOCK) ON ts.id=@slot
            WHERE c.id=@court AND c.venue_id=@venue AND ISNULL(c.is_active,0)=1 AND (c.sport_id IS NULL OR c.sport_id=@sport)
              AND EXISTS(SELECT 1 FROM dbo.sports WHERE id=@sport) AND ts.start_time<ts.end_time;
            """, connection, transaction);
        command.Parameters.Add("@slot", SqlDbType.BigInt).Value = request.TimeSlotId;
        command.Parameters.Add("@court", SqlDbType.BigInt).Value = request.CourtId;
        command.Parameters.Add("@venue", SqlDbType.BigInt).Value = venueId;
        command.Parameters.Add("@sport", SqlDbType.BigInt).Value = request.SportId;
        await using var reader = await command.ExecuteReaderAsync(ct);
        return await reader.ReadAsync(ct) ? (reader.GetTimeSpan(0), reader.GetTimeSpan(1)) : null;
    }

    private async Task<(long SportId, long CourtId, long SlotId, DateOnly Date, string? Band, int Price, string Status)?> ReadExistingAsync(SqlConnection connection, SqlTransaction transaction, long venueId, long id, CancellationToken ct)
    {
        await using var command = new SqlCommand("""
            SELECT s.sport_id,s.court_id,s.time_slot_id,s.session_date,s.band,s.price_vnd,s.status
            FROM dbo.sessions s WITH(UPDLOCK,HOLDLOCK) JOIN dbo.courts c ON c.id=s.court_id
            WHERE s.id=@id AND c.venue_id=@venue;
            """, connection, transaction);
        command.Parameters.Add("@id", SqlDbType.BigInt).Value = id;
        command.Parameters.Add("@venue", SqlDbType.BigInt).Value = venueId;
        await using var reader = await command.ExecuteReaderAsync(ct);
        return await reader.ReadAsync(ct) ? (reader.GetInt64(0), reader.GetInt64(1), reader.GetInt64(2), DateOnly.FromDateTime(reader.GetDateTime(3)), reader.IsDBNull(4) ? null : reader.GetString(4), reader.GetInt32(5), reader.GetString(6)) : null;
    }

    private async Task<(long CourtId, DateOnly Date, TimeSpan Start, TimeSpan End)?> ReadSessionSlotAsync(SqlConnection connection, SqlTransaction? transaction, long venueId, long id, CancellationToken ct)
    {
        await using var command = new SqlCommand("""
            SELECT s.court_id,s.session_date,ts.start_time,ts.end_time FROM dbo.sessions s
            JOIN dbo.courts c ON c.id=s.court_id JOIN dbo.time_slots ts ON ts.id=s.time_slot_id
            WHERE s.id=@id AND c.venue_id=@venue;
            """, connection, transaction);
        command.Parameters.Add("@id", SqlDbType.BigInt).Value = id;
        command.Parameters.Add("@venue", SqlDbType.BigInt).Value = venueId;
        await using var reader = await command.ExecuteReaderAsync(ct);
        return await reader.ReadAsync(ct) ? (reader.GetInt64(0), DateOnly.FromDateTime(reader.GetDateTime(1)), reader.GetTimeSpan(2), reader.GetTimeSpan(3)) : null;
    }

    private static async Task<bool> HasCourtConflictAsync(SqlConnection connection, SqlTransaction? transaction, long courtId, DateOnly date, TimeSpan start, TimeSpan end, long? excludeId, CancellationToken ct)
    {
        const string sql = """
            SELECT COUNT(*) FROM dbo.sessions s WITH(UPDLOCK,HOLDLOCK) JOIN dbo.time_slots ts ON ts.id=s.time_slot_id
            WHERE s.court_id=@court AND s.session_date=@date AND (@exclude IS NULL OR s.id<>@exclude)
              AND LOWER(s.status) NOT IN ('cancelled','completed') AND ts.start_time<@end AND ts.end_time>@start;
            """;
        return await ScalarIntAsync(connection, transaction, sql, ct, ("@court", courtId), ("@date", date.ToDateTime(TimeOnly.MinValue)), ("@exclude", excludeId), ("@start", start), ("@end", end)) > 0;
    }

    private static async Task<bool> HasPersonnelConflictAsync(SqlConnection connection, SqlTransaction transaction, string table, string userColumn, long sessionId, DateOnly date, TimeSpan start, TimeSpan end, long[] userIds, CancellationToken ct)
    {
        foreach (var userId in userIds)
        {
            var sql = $"""
                SELECT COUNT(*) FROM dbo.{table} a WITH(UPDLOCK,HOLDLOCK) JOIN dbo.sessions s ON s.id=a.session_id
                JOIN dbo.time_slots ts ON ts.id=s.time_slot_id
                WHERE a.{userColumn}=@user AND s.session_date=@date AND s.id<>@session
                  AND LOWER(s.status) NOT IN ('cancelled','completed') AND ts.start_time<@end AND ts.end_time>@start;
                """;
            if (await ScalarIntAsync(connection, transaction, sql, ct, ("@user", userId), ("@date", date.ToDateTime(TimeOnly.MinValue)), ("@session", sessionId), ("@start", start), ("@end", end)) > 0) return true;
        }
        return false;
    }

    private static async Task<bool> ValidatePersonnelAsync(SqlConnection connection, SqlTransaction transaction, long venueId, string role, long[] userIds, bool hasVenueColumn, CancellationToken ct)
    {
        if (userIds.Length == 0) return true;
        var ids = string.Join(",", userIds);
        var venueFilter = hasVenueColumn ? " AND u.venue_id=@venue" : string.Empty;
        var sql = $"SELECT COUNT(*) FROM dbo.users u WITH(UPDLOCK,HOLDLOCK) JOIN dbo.roles r ON r.id=u.role_id WHERE u.id IN ({ids}) AND r.code=@role AND LOWER(u.status)='active' AND u.deleted_at IS NULL{venueFilter};";
        await using var command = new SqlCommand(sql, connection, transaction);
        command.Parameters.Add("@role", SqlDbType.VarChar, 100).Value = role;
        if (hasVenueColumn) command.Parameters.Add("@venue", SqlDbType.BigInt).Value = venueId;
        return Convert.ToInt32(await command.ExecuteScalarAsync(ct)) == userIds.Length;
    }

    private static async Task<long[]> AssignedIdsAsync(SqlConnection connection, SqlTransaction transaction, string table, string userColumn, long sessionId, CancellationToken ct)
    {
        var ids = new List<long>();
        await using var command = new SqlCommand($"SELECT {userColumn} FROM dbo.{table} WITH(UPDLOCK,HOLDLOCK) WHERE session_id=@id ORDER BY {userColumn};", connection, transaction);
        command.Parameters.Add("@id", SqlDbType.BigInt).Value = sessionId;
        await using var reader = await command.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct)) ids.Add(reader.GetInt64(0));
        return ids.ToArray();
    }

    private static async Task<int> OccupiedAsync(SqlConnection connection, SqlTransaction transaction, long id, CancellationToken ct) => await ScalarIntAsync(connection, transaction, """
        SELECT COUNT(*) FROM dbo.tickets t WITH(UPDLOCK,HOLDLOCK) LEFT JOIN dbo.social_booking_orders o ON o.id=t.order_id
        WHERE t.session_id=@id AND (LOWER(t.status) IN ('valid','paid','confirmed','checkedin') OR (LOWER(t.status) IN ('pending','reserved') AND o.expires_at>SYSUTCDATETIME()));
        """, ct, ("@id", id));

    private static void AddSessionParameters(SqlCommand command, long venueId, SaveManagedSessionRequest request)
    {
        command.Parameters.Add("@sport", SqlDbType.BigInt).Value = request.SportId;
        command.Parameters.Add("@court", SqlDbType.BigInt).Value = request.CourtId;
        command.Parameters.Add("@slot", SqlDbType.BigInt).Value = request.TimeSlotId;
        command.Parameters.Add("@date", SqlDbType.Date).Value = request.SessionDate.ToDateTime(TimeOnly.MinValue);
        command.Parameters.Add("@band", SqlDbType.VarChar, 100).Value = (object? )request.Band ?? DBNull.Value;
        command.Parameters.Add("@capacity", SqlDbType.SmallInt).Value = request.Capacity;
        command.Parameters.Add("@price", SqlDbType.Int).Value = request.PriceVnd;
    }

    private static async Task AuditAsync(SqlConnection connection, SqlTransaction transaction, long actorId, long sessionId, string action, object? before, object after, CancellationToken ct)
    {
        await using var command = new SqlCommand("INSERT dbo.audit_logs(actor_id,action,entity_type,entity_id,old_value,new_value) VALUES(@actor,@action,'Session',@id,@old,@new);", connection, transaction);
        command.Parameters.Add("@actor", SqlDbType.BigInt).Value = actorId;
        command.Parameters.Add("@action", SqlDbType.VarChar, 100).Value = action;
        command.Parameters.Add("@id", SqlDbType.BigInt).Value = sessionId;
        command.Parameters.Add("@old", SqlDbType.NVarChar, -1).Value = before is null ? DBNull.Value : JsonSerializer.Serialize(before);
        command.Parameters.Add("@new", SqlDbType.NVarChar, -1).Value = JsonSerializer.Serialize(after);
        await command.ExecuteNonQueryAsync(ct);
    }

    private async Task<bool> HasUserVenueColumnAsync(SqlConnection connection, CancellationToken ct, SqlTransaction? transaction = null)
    {
        await using var command = new SqlCommand("SELECT CASE WHEN COL_LENGTH('dbo.users','venue_id') IS NULL THEN 0 ELSE 1 END;", connection, transaction);
        return Convert.ToInt32(await command.ExecuteScalarAsync(ct)) == 1;
    }

    private async Task<SqlConnection> OpenAsync(CancellationToken ct)
    {
        var connection = new SqlConnection(connectionString);
        await SportCenterConnection.OpenAsync(connection, ct, dataScope);
        return connection;
    }

    private bool InScope(long venueId) => venueId > 0 && (dataScope?.RestrictedVenueId is not long restricted || restricted == venueId)
        && (dataScope?.VenueId is not long assigned || assigned == venueId);

    private static bool IsFuture(DateOnly date, TimeSpan start) => date.ToDateTime(TimeOnly.FromTimeSpan(start)) > DateTime.UtcNow.AddHours(7);

    private static async Task<int> ScalarIntAsync(SqlConnection connection, SqlTransaction? transaction, string sql, CancellationToken ct, params (string Name, object? Value)[] parameters)
    {
        await using var command = new SqlCommand(sql, connection, transaction);
        AddParameters(command, parameters);
        return Convert.ToInt32(await command.ExecuteScalarAsync(ct));
    }

    private static async Task<string?> ScalarStringAsync(SqlConnection connection, SqlTransaction? transaction, string sql, CancellationToken ct, params (string Name, object? Value)[] parameters)
    {
        await using var command = new SqlCommand(sql, connection, transaction);
        AddParameters(command, parameters);
        return await command.ExecuteScalarAsync(ct) as string;
    }

    private static async Task ExecuteAsync(SqlConnection connection, SqlTransaction transaction, string sql, CancellationToken ct, params (string Name, object? Value)[] parameters)
    {
        await using var command = new SqlCommand(sql, connection, transaction);
        AddParameters(command, parameters);
        await command.ExecuteNonQueryAsync(ct);
    }

    private static void AddParameters(SqlCommand command, (string Name, object? Value)[] parameters)
    {
        foreach (var (name, value) in parameters) command.Parameters.AddWithValue(name, value ?? DBNull.Value);
    }

    private static async Task<OperationResult> RollbackAsync(SqlTransaction transaction, OperationResult result, CancellationToken ct)
    {
        await transaction.RollbackAsync(ct);
        return result;
    }

    private static OperationResult Invalid(string code, string message) => OperationResult.Invalid(new { code, message });
    private static OperationResult NotFound(string message) => OperationResult.Missing(new { code = "SESSION_NOT_FOUND", message });
    private static OperationResult Conflict(string code, string message) => OperationResult.Conflict(new { code, message });
    private static OperationResult Denied() => OperationResult.Denied(new { code = "CENTER_ACCESS_DENIED", message = "Buổi chơi nằm ngoài cơ sở được phân công." });
}
