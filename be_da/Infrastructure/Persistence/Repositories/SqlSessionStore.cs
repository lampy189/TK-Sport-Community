using be_da.Application.DTOs;
using System.Data;
using Microsoft.Data.SqlClient;
using be_da.Application.Interfaces.Repositories;
using be_da.Domain.Entities;

namespace be_da.Infrastructure.Persistence.Repositories;
public sealed class SqlSessionStore(string connectionString, be_da.Application.Interfaces.External.IDataAccessContext? dataScope = null) : ISessionStore
{
    public async Task<IReadOnlyList<TimeSlotSummary>> GetTimeSlotsAsync(CancellationToken cancellationToken)
    {
        await using var connection = new SqlConnection(connectionString);
        await be_da.Infrastructure.Persistence.SportCenterConnection.OpenAsync(connection, cancellationToken, dataScope);
        const string sql = """
            SELECT id, start_time, end_time, label
            FROM dbo.time_slots
            ORDER BY start_time ASC, id ASC;
            """;
        await using var command = new SqlCommand(sql, connection);
        var slots = new List<TimeSlotSummary>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var id = reader.GetInt64(0);
            var startTime = reader.GetTimeSpan(1);
            var endTime = reader.GetTimeSpan(2);
            var label = reader.IsDBNull(3) ? $"{startTime:hh\\:mm} - {endTime:hh\\:mm}" : reader.GetString(3);
            slots.Add(new TimeSlotSummary(id, startTime, endTime, label));
        }

        return slots;
    }

    public async Task<IReadOnlyList<VenueSummary>> GetVenuesAsync(CancellationToken cancellationToken)
    {
        await using var connection = new SqlConnection(connectionString);
        await be_da.Infrastructure.Persistence.SportCenterConnection.OpenAsync(connection, cancellationToken, dataScope);
        const string sql = """
            SELECT id, name, address
            FROM dbo.venues
            WHERE is_active = 1
            ORDER BY id ASC;
            """;
        await using var command = new SqlCommand(sql, connection);
        var venues = new List<VenueSummary>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            venues.Add(new VenueSummary(Id: reader.GetInt64(0), Name: reader.GetString(1), Address: reader.IsDBNull(2) ? null : reader.GetString(2)));
        }

        return venues;
    }

    public async Task<IReadOnlyList<SessionSummary>> GetSessionsAsync(SessionFilter filter, long? currentUserId, CancellationToken cancellationToken)
    {
        await using var connection = new SqlConnection(connectionString);
        await be_da.Infrastructure.Persistence.SportCenterConnection.OpenAsync(connection, cancellationToken, dataScope);
        const string sql = """
            SELECT 
                s.id, s.sport_id, sp.code AS sport_code, sp.name AS sport_name,
                s.court_id, c.name AS court_name, v.name AS venue_name,
                s.time_slot_id, ts.start_time, ts.end_time, ts.label AS time_label,
                s.session_date, s.band, s.capacity, s.price_vnd, s.status, s.created_by,
                ISNULL(occ.occupied_count, 0) AS occupied_places,
                v.id AS venue_id, v.address AS venue_address
            FROM dbo.sessions s
            JOIN dbo.sports sp ON sp.id = s.sport_id
            JOIN dbo.courts c ON c.id = s.court_id
            JOIN dbo.venues v ON v.id = c.venue_id
            JOIN dbo.time_slots ts ON ts.id = s.time_slot_id
            OUTER APPLY (
                SELECT COUNT(*) AS occupied_count
                FROM dbo.tickets t
                LEFT JOIN dbo.social_booking_orders o ON o.id = t.order_id
                WHERE t.session_id = s.id
                  AND (
                      LOWER(t.status) IN ('valid', 'paid', 'confirmed', 'checkedin')
                      OR (LOWER(t.status) IN ('pending', 'reserved') AND o.expires_at > SYSUTCDATETIME())
                  )
            ) occ
            WHERE (@status IS NOT NULL AND s.status = @status OR @status IS NULL AND s.status IN ('Open', 'Published'))
              AND c.is_active=1 AND v.is_active=1
              AND (@sportId IS NULL OR s.sport_id = @sportId)
              AND (@sport IS NULL OR sp.code = @sport OR sp.name = @sport)
              AND (@sessionDate IS NULL OR s.session_date = @sessionDate)
              AND (@fromDate IS NULL OR s.session_date >= @fromDate)
              AND (@toDate IS NULL OR s.session_date <= @toDate)
              AND (@timeSlotId IS NULL OR s.time_slot_id = @timeSlotId)
              AND (@venueId IS NULL OR c.venue_id = @venueId)
              AND (@venue IS NULL OR v.name = @venue)
              AND (
                  @sessionType IS NULL
                  OR (LOWER(@sessionType) IN ('hourlycourt', 'hourly', '1h') AND DATEDIFF(MINUTE, ts.start_time, ts.end_time) <= 60)
                  OR (LOWER(@sessionType) IN ('social', '2h') AND DATEDIFF(MINUTE, ts.start_time, ts.end_time) > 60)
              )
            ORDER BY s.session_date ASC, ts.start_time ASC;
            """;
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add("@status", SqlDbType.VarChar, 100).Value = (object? )filter.Status ?? DBNull.Value;
        command.Parameters.Add("@sportId", SqlDbType.BigInt).Value = (object? )filter.SportId ?? DBNull.Value;
        command.Parameters.Add("@sport", SqlDbType.NVarChar, 100).Value = (object? )filter.Sport ?? DBNull.Value;
        command.Parameters.Add("@sessionDate", SqlDbType.Date).Value = filter.Date.HasValue ? filter.Date.Value.ToDateTime(TimeOnly.MinValue) : DBNull.Value;
        command.Parameters.Add("@fromDate", SqlDbType.Date).Value = filter.FromDate.HasValue ? filter.FromDate.Value.ToDateTime(TimeOnly.MinValue) : DBNull.Value;
        command.Parameters.Add("@toDate", SqlDbType.Date).Value = filter.ToDate.HasValue ? filter.ToDate.Value.ToDateTime(TimeOnly.MinValue) : DBNull.Value;
        command.Parameters.Add("@timeSlotId", SqlDbType.BigInt).Value = (object? )filter.TimeSlotId ?? DBNull.Value;
        command.Parameters.Add("@venueId", SqlDbType.BigInt).Value = (object? )filter.VenueId ?? DBNull.Value;
        command.Parameters.Add("@venue", SqlDbType.NVarChar, 255).Value = (object? )filter.Venue ?? DBNull.Value;
        command.Parameters.Add("@sessionType", SqlDbType.VarChar, 50).Value = (object? )filter.SessionType ?? DBNull.Value;
        var rawSessions = new List<SessionSummary>();
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                rawSessions.Add(ReadSessionSummary(reader));
            }
        }

        if (!currentUserId.HasValue || rawSessions.Count == 0)
        {
            return rawSessions;
        }

        // Evaluate user's eligibility suggestion
        var(profiles, exceptions) = await LoadPlayerProfilesAndExceptionsAsync(connection, currentUserId.Value, cancellationToken);
        var result = new List<SessionSummary>(rawSessions.Count);
        foreach (var session in rawSessions)
        {
            var(eligible, reason) = CheckEligibility(session, profiles, exceptions);
            result.Add(session with { IsEligibleSuggestion = eligible, IneligibilityReason = reason });
        }

        return result;
    }

    public async Task<SessionDetails?> GetSessionByIdAsync(long id, long? currentUserId, CancellationToken cancellationToken)
    {
        await using var connection = new SqlConnection(connectionString);
        await be_da.Infrastructure.Persistence.SportCenterConnection.OpenAsync(connection, cancellationToken, dataScope);
        const string sessionSql = """
            SELECT 
                s.id, s.sport_id, sp.code AS sport_code, sp.name AS sport_name,
                s.court_id, c.name AS court_name, v.name AS venue_name,
                s.time_slot_id, ts.start_time, ts.end_time, ts.label AS time_label,
                s.session_date, s.band, s.capacity, s.price_vnd, s.status, s.created_by,
                ISNULL(occ.occupied_count, 0) AS occupied_places,
                v.id AS venue_id, v.address AS venue_address
            FROM dbo.sessions s
            JOIN dbo.sports sp ON sp.id = s.sport_id
            JOIN dbo.courts c ON c.id = s.court_id
            JOIN dbo.venues v ON v.id = c.venue_id
            JOIN dbo.time_slots ts ON ts.id = s.time_slot_id
            OUTER APPLY (
                SELECT COUNT(*) AS occupied_count
                FROM dbo.tickets t
                LEFT JOIN dbo.social_booking_orders o ON o.id = t.order_id
                WHERE t.session_id = s.id
                  AND (
                      LOWER(t.status) IN ('valid', 'paid', 'confirmed', 'checkedin')
                      OR (LOWER(t.status) IN ('pending','reserved') AND o.expires_at > SYSUTCDATETIME())
                  )
            ) occ
            WHERE s.id = @id;
            """;
        SessionSummary? summary = null;
        await using (var command = new SqlCommand(sessionSql, connection))
        {
            command.Parameters.Add("@id", SqlDbType.BigInt).Value = id;
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            if (await reader.ReadAsync(cancellationToken))
            {
                summary = ReadSessionSummary(reader);
            }
        }

        if (summary == null)
            return null;
        // Fetch public participants
        const string participantsSql = """
            SELECT 
                t.id AS ticket_id,
                p.id AS player_id,
                u.full_name,
                p.avatar_url,
                psp.current_band,
                t.status AS ticket_status,
                psp.current_score,
                t.ticket_code,
                CASE WHEN EXISTS (
                    SELECT 1 FROM dbo.session_exceptions se
                    WHERE se.session_id = t.session_id
                      AND se.player_id = p.id
                      AND se.approved_by IS NOT NULL
                ) THEN 1 ELSE 0 END AS has_approved_exception
            FROM dbo.tickets t
            JOIN dbo.players p ON p.id = t.player_id
            JOIN dbo.users u ON u.id = p.user_id
            LEFT JOIN dbo.social_booking_orders o ON o.id = t.order_id
            LEFT JOIN dbo.player_sport_profiles psp ON psp.player_id = p.id AND psp.sport_id = @sportId
            WHERE t.session_id = @sessionId
              AND (
                  LOWER(t.status) IN ('valid', 'paid', 'confirmed', 'checkedin')
                  OR (LOWER(t.status) = 'pending' AND o.expires_at > SYSUTCDATETIME())
              )
            ORDER BY t.id ASC;
            """;
        var participants = new List<SessionParticipant>();
        await using (var command = new SqlCommand(participantsSql, connection))
        {
            command.Parameters.Add("@sessionId", SqlDbType.BigInt).Value = id;
            command.Parameters.Add("@sportId", SqlDbType.BigInt).Value = summary.SportId;
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                var rawBand = reader.IsDBNull(4) ? null : reader.GetString(4);
                short? rawScore = reader.IsDBNull(6) ? null : reader.GetInt16(6);
                var ticketCode = reader.IsDBNull(7) ? null : reader.GetString(7);
                var hasApprovedException = !reader.IsDBNull(8) && reader.GetInt32(8) == 1;
                var normalizedBand = NormalizeBand(rawBand, rawScore);
                var displayBand = normalizedBand;
                if (!string.IsNullOrWhiteSpace(summary.Band))
                {
                    var sessionBandTrimmed = summary.Band.Trim();
                    var isSeedDemoTicket = !string.IsNullOrWhiteSpace(ticketCode) && (ticketCode.StartsWith("TKT-W4", StringComparison.OrdinalIgnoreCase) || ticketCode.StartsWith("SEED-", StringComparison.OrdinalIgnoreCase));
                    if (isSeedDemoTicket && !hasApprovedException)
                    {
                        displayBand = sessionBandTrimmed;
                    }
                    else if (hasApprovedException && !string.IsNullOrWhiteSpace(normalizedBand) && !string.Equals(normalizedBand, sessionBandTrimmed, StringComparison.OrdinalIgnoreCase))
                    {
                        displayBand = $"{normalizedBand} · Ngoại lệ";
                    }
                }

                participants.Add(new SessionParticipant(TicketId: reader.GetInt64(0), PlayerId: reader.GetInt64(1), FullName: reader.GetString(2), AvatarUrl: reader.IsDBNull(3) ? null : reader.GetString(3), CurrentBand: displayBand, TicketStatus: reader.GetString(5)));
            }
        }

        if (currentUserId.HasValue)
        {
            var(profiles, exceptions) = await LoadPlayerProfilesAndExceptionsAsync(connection, currentUserId.Value, cancellationToken);
            var(eligible, reason) = CheckEligibility(summary, profiles, exceptions);
            summary = summary with
            {
                IsEligibleSuggestion = eligible,
                IneligibilityReason = reason
            };
        }

        return new SessionDetails(summary, participants);
    }

    public Task<RegisterSessionResult> RegisterSessionAsync(long userId, long sessionId, CancellationToken cancellationToken) => RegisterCoreAsync(userId, sessionId, null, cancellationToken);
    public Task<RegisterSessionResult> RegisterLateEntryAsync(long userId, long sessionId, long expectedRevision, CancellationToken cancellationToken) => RegisterCoreAsync(userId, sessionId, expectedRevision, cancellationToken);
    public async Task<RegisterSessionResult> AcceptInvitationAsync(long userId, long invitationId, CancellationToken ct)
    {
        await using var c = new SqlConnection(connectionString);
        await be_da.Infrastructure.Persistence.SportCenterConnection.OpenAsync(c,ct,dataScope);
        await WorkflowSql.RequireInvitations(c,null,ct);
        var session = await WorkflowSql.Scalar(c,null,"SELECT i.session_id FROM dbo.invitations i JOIN dbo.players p ON p.id=i.invitee_id WHERE i.id=@id AND p.user_id=@user",ct,("@id",invitationId),("@user",userId));
        return session==0 ? new(false,"INVITATION_NOT_FOUND","Không tìm thấy lời mời.") : await RegisterCoreAsync(userId,session,null,ct,invitationId);
    }
    private async Task<RegisterSessionResult> RegisterCoreAsync(long userId, long sessionId, long? lateRevision, CancellationToken cancellationToken, long? invitationId = null)
    {
        await using var connection = new SqlConnection(connectionString);
        await be_da.Infrastructure.Persistence.SportCenterConnection.OpenAsync(connection, cancellationToken, dataScope);
        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        try
        {
            var payerId = userId;
            // Find player ID for user
            long playerId;
            await using (var playerCmd = new SqlCommand("SELECT id FROM dbo.players WHERE user_id=@userId;", connection, transaction))
            {
                playerCmd.Parameters.Add("@userId", SqlDbType.BigInt).Value = userId;
                var res = await playerCmd.ExecuteScalarAsync(cancellationToken);
                if (res == null)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return new RegisterSessionResult(false, "SKILL_INELIGIBLE", "Người chơi chưa làm khảo sát kỹ năng.", "chưa làm survey");
                }

                playerId = Convert.ToInt64(res);
            }

            // Lock and inspect session
            const string sessionLockSql = """
                SELECT s.id, s.sport_id, s.band, s.capacity, s.price_vnd, s.status, s.session_date, ts.start_time, ts.end_time
                FROM dbo.sessions s WITH (UPDLOCK, HOLDLOCK)
                JOIN dbo.time_slots ts ON ts.id = s.time_slot_id
                JOIN dbo.courts c ON c.id=s.court_id JOIN dbo.venues v ON v.id=c.venue_id
                  WHERE s.id = @sessionId AND v.is_active=1 AND c.is_active=1
                    AND (@selectedCenter IS NULL OR c.venue_id=@selectedCenter);
                """;
            long sportId;
            string? sessionBand;
            short capacity;
            int priceVnd;
            string sessionStatus;
            DateOnly sessionDate;
            TimeSpan startTime;
            TimeSpan endTime;
            await using (var sessionCmd = new SqlCommand(sessionLockSql, connection, transaction))
            {
                sessionCmd.Parameters.Add("@sessionId", SqlDbType.BigInt).Value = sessionId;
                sessionCmd.Parameters.Add("@late", SqlDbType.Bit).Value = lateRevision.HasValue;
                sessionCmd.Parameters.Add("@selectedCenter", SqlDbType.BigInt).Value = (object? )dataScope?.RestrictedVenueId ?? DBNull.Value;
                await using var reader = await sessionCmd.ExecuteReaderAsync(cancellationToken);
                if (!await reader.ReadAsync(cancellationToken))
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return new RegisterSessionResult(false, "SESSION_NOT_FOUND", "Buổi chơi không tồn tại.");
                }

                sportId = reader.GetInt64(1);
                sessionBand = reader.IsDBNull(2) ? null : reader.GetString(2);
                capacity = reader.GetInt16(3);
                priceVnd = reader.GetInt32(4);
                sessionStatus = reader.GetString(5);
                sessionDate = DateOnly.FromDateTime(reader.GetDateTime(6));
                startTime = reader.GetTimeSpan(7);
                endTime = reader.GetTimeSpan(8);
            }

            if (invitationId.HasValue)
            {
                await WorkflowSql.RequireInvitations(connection,transaction,cancellationToken);
                await using var invite = WorkflowSql.Command(connection,transaction,"""
                    SELECT i.status,i.expires_at,i.payer_user_id,i.order_id,i.ticket_id,o.order_code,t.ticket_code,t.amount_vnd,o.expires_at
                    FROM dbo.invitations i WITH(UPDLOCK,HOLDLOCK) JOIN dbo.players p ON p.id=i.invitee_id
                    LEFT JOIN dbo.social_booking_orders o ON o.id=i.order_id LEFT JOIN dbo.tickets t ON t.id=i.ticket_id
                    WHERE i.id=@id AND p.user_id=@user AND i.session_id=@session;
                    """,("@id",invitationId.Value),("@user",userId),("@session",sessionId));
                await using var row=await invite.ExecuteReaderAsync(cancellationToken);
                if(!await row.ReadAsync(cancellationToken)) return new(false,"INVITATION_NOT_FOUND","Không tìm thấy lời mời.");
                if(string.Equals(row.GetString(0),"accepted",StringComparison.OrdinalIgnoreCase) && !row.IsDBNull(3) && !row.IsDBNull(4))
                    return new(true,null,"Lời mời đã được chấp nhận.",null,row.GetInt64(3),row.GetString(5),row.GetInt64(4),row.GetString(6),row.GetInt32(7),row.GetFieldValue<DateTimeOffset>(8));
                if(!string.Equals(row.GetString(0),"pending",StringComparison.OrdinalIgnoreCase) || row.IsDBNull(1) || row.GetFieldValue<DateTimeOffset>(1)<=DateTimeOffset.UtcNow || row.IsDBNull(2))
                    return new(false,"INVITATION_EXPIRED","Lời mời đã xử lý hoặc hết hạn.");
                payerId=row.GetInt64(2);
            }

            var durationMinutes = (endTime - startTime).TotalMinutes;
            var isHourlyCourt = durationMinutes > 0 && durationMinutes <= 60;
            if (!string.Equals(sessionStatus, "Open", StringComparison.OrdinalIgnoreCase) && !string.Equals(sessionStatus, "Published", StringComparison.OrdinalIgnoreCase) && !(lateRevision.HasValue && string.Equals(sessionStatus, "ongoing", StringComparison.OrdinalIgnoreCase)))
            {
                await transaction.RollbackAsync(cancellationToken);
                return new RegisterSessionResult(false, "SESSION_NOT_OPEN", "Buổi chơi chưa mở hoặc đã kết thúc đăng ký.");
            }

            var nowVn = DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(7)).DateTime;
            var sessionStartVn = sessionDate.ToDateTime(TimeOnly.FromTimeSpan(startTime));
            var now = DateTimeOffset.UtcNow;
            var endsAt = TKSC.Domain.Payments.LateEntryRules.End(sessionDate, TimeOnly.FromTimeSpan(endTime));
            if(!lateRevision.HasValue)
            {
                var reason=await WorkflowSql.Eligibility(connection,transaction,sessionId,playerId,cancellationToken);
                if(reason is not null) return new(false,reason.Contains("giữ chỗ",StringComparison.Ordinal)?"ALREADY_REGISTERED":reason.Contains("đủ người",StringComparison.Ordinal)?"SESSION_FULL":reason.Contains("trình độ",StringComparison.Ordinal)?"SKILL_INELIGIBLE":"REGISTRATION_INELIGIBLE",reason);
            }
            if(await WorkflowSql.Scalar(connection,transaction,"SELECT COUNT(*) FROM dbo.users u JOIN dbo.roles r ON r.id=u.role_id WHERE u.id=@payer AND u.deleted_at IS NULL AND LOWER(u.status)='active' AND r.code='Player'",cancellationToken,("@payer",payerId))!=1)
                return new(false,"PAYER_UNAVAILABLE","Người thanh toán không còn hoạt động.");
            if (lateRevision.HasValue)
            {
                await LateEntrySql.RequireSchema(connection, transaction, cancellationToken);
                if (!TKSC.Domain.Payments.LateEntryRules.Running(TKSC.Domain.Payments.LateEntryRules.End(sessionDate, TimeOnly.FromTimeSpan(startTime)), endsAt, sessionStatus, now))
                    return new(false, "LATE_ENTRY_ENDED", "Buổi chưa bắt đầu hoặc đã kết thúc, không thể mua suất giờ lẻ.");
                await using (var retry = new SqlCommand("""
                    SELECT TOP(1) o.id,o.order_code,t.id,t.ticket_code,t.amount_vnd,o.expires_at
                    FROM dbo.tickets t JOIN dbo.late_entry_tickets lt ON lt.ticket_id=t.id
                    JOIN dbo.social_booking_orders o ON o.id=t.order_id
                    WHERE t.session_id=@session AND t.player_id=@player AND o.payer_id=@user
                      AND LOWER(t.status) IN ('pending','reserved') AND LOWER(o.status)='pending' AND o.expires_at>@now
                    ORDER BY t.id DESC
                    """, connection, transaction))
                {
                    retry.Parameters.AddWithValue("@session", sessionId);
                    retry.Parameters.AddWithValue("@player", playerId);
                    retry.Parameters.AddWithValue("@user", userId);
                    retry.Parameters.AddWithValue("@now", now);
                    await using var previous = await retry.ExecuteReaderAsync(cancellationToken);
                    if (await previous.ReadAsync(cancellationToken))
                        return new(true, null, "Tiếp tục thanh toán chỗ đang giữ.", null, previous.GetInt64(0), previous.GetString(1), previous.GetInt64(2), previous.GetString(3), previous.GetInt32(4), previous.GetFieldValue<DateTimeOffset>(5));
                }

                var quote = TKSC.Domain.Payments.LateEntryRules.Quote(TKSC.Domain.Payments.LateEntryRules.End(sessionDate, TimeOnly.FromTimeSpan(startTime)), endsAt, sessionStatus, now, priceVnd);
                if (quote is null)
                    return new(false, "LATE_ENTRY_CLOSED", "Buổi không còn đủ điều kiện bán vé giờ lẻ.");
                if (quote.Revision != lateRevision.Value)
                    return new(false, "LATE_ENTRY_CHANGED", "Giá theo thời gian vừa thay đổi. Tải lại và xác nhận giá mới.");
                priceVnd = quote.PriceVnd;
                // A read never writes an offer. Materialize its automatic quote atomically
                // with the first hold, under the session lock, for the ticket's FK/history.
                await using (var offerCmd = new SqlCommand("""
                    IF EXISTS(SELECT 1 FROM dbo.late_entry_offers WHERE session_id=@session)
                      UPDATE dbo.late_entry_offers SET max_tickets=@capacity,price_vnd=@price,is_open=1,
                        revision=@revision,updated_by=@actor,updated_at=@now WHERE session_id=@session;
                    ELSE INSERT dbo.late_entry_offers(session_id,max_tickets,price_vnd,is_open,revision,updated_by,updated_at)
                      VALUES(@session,@capacity,@price,1,@revision,@actor,@now);
                    """, connection, transaction))
                {
                    offerCmd.Parameters.AddWithValue("@session", sessionId);
                    offerCmd.Parameters.AddWithValue("@capacity", Math.Min((int)capacity, 8));
                    offerCmd.Parameters.AddWithValue("@price", priceVnd);
                    offerCmd.Parameters.AddWithValue("@revision", quote.Revision);
                    offerCmd.Parameters.AddWithValue("@actor", userId);
                    offerCmd.Parameters.AddWithValue("@now", now);
                    await offerCmd.ExecuteNonQueryAsync(cancellationToken);
                }

                if (!await LateEntrySql.Eligible(connection, transaction, sessionId, playerId, now, cancellationToken))
                    return new(false, "SKILL_INELIGIBLE", "Chưa đủ điều kiện trình độ/tài khoản hoặc trùng lịch với vé khác.");
            }

            if (!lateRevision.HasValue && sessionStartVn <= nowVn)
            {
                await transaction.RollbackAsync(cancellationToken);
                return new RegisterSessionResult(false, "SESSION_PAST_START", "Buổi chơi đã qua thời điểm bắt đầu và đã khóa đăng ký.");
            }

            // Bước A: Check Sport-Specific Skill Eligibility
            bool hasApprovedException = false;
            await using (var excCmd = new SqlCommand("""
                SELECT COUNT(*) FROM dbo.session_exceptions WITH (UPDLOCK, HOLDLOCK)
                WHERE session_id = @sessionId AND player_id = @playerId AND approved_by IS NOT NULL;
                """, connection, transaction))
            {
                excCmd.Parameters.Add("@sessionId", SqlDbType.BigInt).Value = sessionId;
                excCmd.Parameters.Add("@playerId", SqlDbType.BigInt).Value = playerId;
                hasApprovedException = Convert.ToInt32(await excCmd.ExecuteScalarAsync(cancellationToken)) > 0;
            }

            if (!hasApprovedException && (!isHourlyCourt || lateRevision.HasValue) && !string.IsNullOrWhiteSpace(sessionBand))
            {
                // Must have sport profile matching required band
                string? playerBand = null;
                short? currentScore = null;
                string? ratingStatus = null;
                bool profileFound = false;
                await using (var profileCmd = new SqlCommand("""
                    SELECT current_score, current_band, rating_status
                    FROM dbo.player_sport_profiles WITH (UPDLOCK, HOLDLOCK)
                    WHERE player_id = @playerId AND sport_id = @sportId;
                    """, connection, transaction))
                {
                    profileCmd.Parameters.Add("@playerId", SqlDbType.BigInt).Value = playerId;
                    profileCmd.Parameters.Add("@sportId", SqlDbType.BigInt).Value = sportId;
                    await using var reader = await profileCmd.ExecuteReaderAsync(cancellationToken);
                    if (await reader.ReadAsync(cancellationToken))
                    {
                        profileFound = true;
                        currentScore = reader.IsDBNull(0) ? null : reader.GetInt16(0);
                        playerBand = NormalizeBand(reader.IsDBNull(1) ? null : reader.GetString(1), currentScore);
                        ratingStatus = reader.IsDBNull(2) ? null : reader.GetString(2);
                    }
                }

                if (!profileFound)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return new RegisterSessionResult(false, "SKILL_INELIGIBLE", "Người chơi chưa có hồ sơ kỹ năng cho môn này.", "chưa có hồ sơ môn này");
                }

                if (string.IsNullOrWhiteSpace(playerBand) || currentScore == null || string.Equals(ratingStatus, "PendingSurvey", StringComparison.OrdinalIgnoreCase) || string.Equals(ratingStatus, "Unrated", StringComparison.OrdinalIgnoreCase))
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return new RegisterSessionResult(false, "SKILL_INELIGIBLE", "Người chơi chưa làm khảo sát kỹ năng.", "chưa làm survey");
                }

                if (!string.Equals(playerBand.Trim(), sessionBand.Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return new RegisterSessionResult(false, "SKILL_INELIGIBLE", $"Band kỹ năng ({playerBand}) không khớp với yêu cầu của buổi chơi ({sessionBand}).", "lệch band kỹ năng");
                }
            }

            // Check if player already registered
            await using (var checkCmd = new SqlCommand("""
                SELECT COUNT(*)
                FROM dbo.tickets t WITH (UPDLOCK, HOLDLOCK)
                LEFT JOIN dbo.social_booking_orders o WITH (UPDLOCK, HOLDLOCK) ON o.id = t.order_id
                WHERE t.session_id = @sessionId AND t.player_id = @playerId
                  AND (
                      LOWER(t.status) IN ('valid', 'paid', 'confirmed', 'checkedin')
                      OR (LOWER(t.status) IN ('pending','reserved') AND o.expires_at > SYSUTCDATETIME())
                  );
                """, connection, transaction))
            {
                checkCmd.Parameters.Add("@sessionId", SqlDbType.BigInt).Value = sessionId;
                checkCmd.Parameters.Add("@playerId", SqlDbType.BigInt).Value = playerId;
                if (Convert.ToInt32(await checkCmd.ExecuteScalarAsync(cancellationToken)) > 0)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return new RegisterSessionResult(false, "ALREADY_REGISTERED", "Bạn đã có vé hoặc đang giữ chỗ cho buổi chơi này.");
                }
            }

            // Bước B: Check Available Slots
            int occupiedCount;
            await using (var occCmd = new SqlCommand("""
                SELECT COUNT(*)
                FROM dbo.tickets t WITH (UPDLOCK, HOLDLOCK)
                LEFT JOIN dbo.social_booking_orders o WITH (UPDLOCK, HOLDLOCK) ON o.id = t.order_id
                WHERE t.session_id = @sessionId
                  AND (
                      LOWER(t.status) IN ('valid', 'paid', 'confirmed', 'checkedin')
                      OR (LOWER(t.status) = 'pending' AND o.expires_at > SYSUTCDATETIME())
                  );
                """, connection, transaction))
            {
                occCmd.Parameters.Add("@sessionId", SqlDbType.BigInt).Value = sessionId;
                occupiedCount = Convert.ToInt32(await occCmd.ExecuteScalarAsync(cancellationToken));
            }

            if (occupiedCount >= Math.Min(capacity, (short)8))
            {
                await transaction.RollbackAsync(cancellationToken);
                return new RegisterSessionResult(false, "SESSION_FULL", "Buổi chơi đã đủ người.");
            }

            // Bước C: Record Pending Registration (Timed place hold)
            var sessionStart = TKSC.Domain.Payments.LateEntryRules.End(sessionDate, TimeOnly.FromTimeSpan(startTime));
            var expiresAt = lateRevision.HasValue ? TKSC.Domain.Payments.LateEntryRules.HoldExpiry(now, endsAt) : (now.AddMinutes(15)<sessionStart ? now.AddMinutes(15) : sessionStart);
            var orderCode = $"SBO-{DateTimeOffset.UtcNow:yyMMddHHmmss}-{Random.Shared.Next(1000, 9999)}";
            long orderId;
            const string insertOrderSql = """
                INSERT INTO dbo.social_booking_orders (order_code, payer_id, total_amount, status, expires_at)
                VALUES (@orderCode, @payerId, @totalAmount, 'Pending', @expiresAt);
                SELECT CONVERT(bigint, SCOPE_IDENTITY());
                """;
            await using (var insertOrderCmd = new SqlCommand(insertOrderSql, connection, transaction))
            {
                insertOrderCmd.Parameters.Add("@orderCode", SqlDbType.VarChar, 100).Value = orderCode;
                insertOrderCmd.Parameters.Add("@payerId", SqlDbType.BigInt).Value = payerId;
                insertOrderCmd.Parameters.Add("@totalAmount", SqlDbType.Int).Value = priceVnd;
                insertOrderCmd.Parameters.Add("@expiresAt", SqlDbType.DateTimeOffset, 0).Value = expiresAt;
                orderId = (long)(await insertOrderCmd.ExecuteScalarAsync(cancellationToken))!;
            }

            var ticketCode = lateRevision.HasValue ? $"LATE-{Guid.NewGuid():N}" : $"TKT-{orderId}-{DateTimeOffset.UtcNow:yyMMddHHmmss}-{Random.Shared.Next(100, 999)}";
            long ticketId;
            const string insertTicketSql = """
                INSERT INTO dbo.tickets (order_id, session_id, player_id, ticket_code, status, amount_vnd)
                VALUES (@orderId, @sessionId, @playerId, @ticketCode, 'Pending', @amountVnd);
                SELECT CONVERT(bigint, SCOPE_IDENTITY());
                """;
            await using (var insertTicketCmd = new SqlCommand(insertTicketSql, connection, transaction))
            {
                insertTicketCmd.Parameters.Add("@orderId", SqlDbType.BigInt).Value = orderId;
                insertTicketCmd.Parameters.Add("@sessionId", SqlDbType.BigInt).Value = sessionId;
                insertTicketCmd.Parameters.Add("@playerId", SqlDbType.BigInt).Value = playerId;
                insertTicketCmd.Parameters.Add("@ticketCode", SqlDbType.VarChar, 100).Value = ticketCode;
                insertTicketCmd.Parameters.Add("@amountVnd", SqlDbType.Int).Value = priceVnd;
                ticketId = (long)(await insertTicketCmd.ExecuteScalarAsync(cancellationToken))!;
            }

            if (lateRevision.HasValue)
            {
                await using var late = new SqlCommand("INSERT dbo.late_entry_tickets(ticket_id,session_id,offer_revision,starts_at,ends_at,purchased_at) VALUES(@ticket,@session,@revision,@start,@end,@now)", connection, transaction);
                late.Parameters.AddWithValue("@ticket", ticketId);
                late.Parameters.AddWithValue("@session", sessionId);
                late.Parameters.AddWithValue("@revision", lateRevision.Value);
                late.Parameters.AddWithValue("@start", TKSC.Domain.Payments.LateEntryRules.End(sessionDate, TimeOnly.FromTimeSpan(startTime)));
                late.Parameters.AddWithValue("@end", endsAt);
                late.Parameters.AddWithValue("@now", now);
                await late.ExecuteNonQueryAsync(cancellationToken);
            }

            // Durable web/email events commit atomically with the held ticket.
            await using (var notify = new SqlCommand("""
                INSERT INTO dbo.notifications (user_id, channel, title, reference_type, reference_id, status)
                VALUES (@userId, 'web', @title, 'ticket_reserved', @ticketId, 'unread'),
                       (@userId, 'email', @title, 'ticket_reserved', @ticketId, 'pending');
                """, connection, transaction))
            {
                notify.Parameters.Add("@userId", SqlDbType.BigInt).Value = userId;
                notify.Parameters.Add("@ticketId", SqlDbType.BigInt).Value = ticketId;
                notify.Parameters.Add("@title", SqlDbType.NVarChar, 255).Value = TKSC.Application.Services.Notifications.TicketNotifications.Title("ticket_reserved", ticketId);
                await notify.ExecuteNonQueryAsync(cancellationToken);
            }

            if(invitationId.HasValue)
            {
                await WorkflowSql.Execute(connection,transaction,"""
                    UPDATE dbo.invitations SET status='accepted',responded_at=SYSDATETIMEOFFSET(),order_id=@order,ticket_id=@ticket WHERE id=@id;
                    INSERT dbo.notifications(user_id,channel,title,reference_type,reference_id,status)
                    SELECT p.user_id,'web',N'Lời mời đã được chấp nhận','session_invitation',i.id,'unread'
                    FROM dbo.invitations i JOIN dbo.players p ON p.id=i.inviter_id WHERE i.id=@id;
                    IF @payer<>@recipient INSERT dbo.notifications(user_id,channel,title,reference_type,reference_id,status)
                    VALUES(@payer,'web',N'Bạn có vé của bạn bè đang chờ thanh toán','session_invitation',@id,'unread');
                    """,cancellationToken,("@id",invitationId.Value),("@order",orderId),("@ticket",ticketId),("@payer",payerId),("@recipient",userId));
            }
            await transaction.CommitAsync(cancellationToken);
            return new RegisterSessionResult(Success: true, ErrorCode: null, Message: "Giữ chỗ thành công. Vui lòng thanh toán trước khi hết hạn.", Reason: null, OrderId: orderId, OrderCode: orderCode, TicketId: ticketId, TicketCode: ticketCode, AmountVnd: priceVnd, ExpiresAt: expiresAt);
        }
        catch (SqlException ex)when (ex.Number is 2601 or 2627)
        {
            await transaction.RollbackAsync(cancellationToken);
            return new RegisterSessionResult(false, "CONCURRENCY_CONFLICT", "Đã xảy ra tranh chấp dữ liệu khi giữ chỗ. Vui lòng thử lại.");
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private static SessionSummary ReadSessionSummary(SqlDataReader reader)
    {
        var startTime = reader.GetTimeSpan(8);
        var endTime = reader.GetTimeSpan(9);
        var date = DateOnly.FromDateTime(reader.GetDateTime(11));
        var venueId = reader.FieldCount > 18 && !reader.IsDBNull(18) ? reader.GetInt64(18) : 1L;
        var venueAddress = reader.FieldCount > 19 && !reader.IsDBNull(19) ? reader.GetString(19) : null;
        return new SessionSummary(Id: reader.GetInt64(0), SportId: reader.GetInt64(1), SportCode: reader.GetString(2), SportName: reader.GetString(3), CourtId: reader.GetInt64(4), CourtName: reader.GetString(5), VenueName: reader.GetString(6), TimeSlotId: reader.GetInt64(7), StartTime: startTime, EndTime: endTime, TimeLabel: reader.IsDBNull(10) ? $"{startTime:hh\\:mm} - {endTime:hh\\:mm}" : reader.GetString(10), SessionDate: date, Band: reader.IsDBNull(12) ? null : reader.GetString(12), Capacity: reader.GetInt16(13), PriceVnd: reader.GetInt32(14), Status: reader.GetString(15), CreatedBy: reader.GetInt64(16), OccupiedPlaces: Convert.ToInt16(reader.GetInt32(17)), VenueId: venueId, VenueAddress: venueAddress);
    }

    private static async Task<(Dictionary<long, PlayerSportProfileInfo> Profiles, HashSet<long> Exceptions)> LoadPlayerProfilesAndExceptionsAsync(SqlConnection connection, long userId, CancellationToken cancellationToken)
    {
        var profiles = new Dictionary<long, PlayerSportProfileInfo>();
        var exceptions = new HashSet<long>();
        const string sql = """
            SELECT psp.sport_id, psp.current_band, psp.current_score, psp.rating_status, p.id AS player_id
            FROM dbo.player_sport_profiles psp
            JOIN dbo.players p ON p.id = psp.player_id
            WHERE p.user_id = @userId;

            SELECT se.session_id
            FROM dbo.session_exceptions se
            JOIN dbo.players p ON p.id = se.player_id
            WHERE p.user_id = @userId AND se.approved_by IS NOT NULL;
            """;
        await using var cmd = new SqlCommand(sql, connection);
        cmd.Parameters.Add("@userId", SqlDbType.BigInt).Value = userId;
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var sportId = reader.GetInt64(0);
            var rawBand = reader.IsDBNull(1) ? null : reader.GetString(1);
            var score = reader.IsDBNull(2) ? null : (short? )reader.GetInt16(2);
            var band = NormalizeBand(rawBand, score);
            var status = reader.IsDBNull(3) ? null : reader.GetString(3);
            var playerId = reader.GetInt64(4);
            profiles[sportId] = new PlayerSportProfileInfo(playerId, sportId, band, score, status);
        }

        if (await reader.NextResultAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                exceptions.Add(reader.GetInt64(0));
            }
        }

        return (profiles, exceptions);
    }

    internal static string? NormalizeBand(string? band, short? score)
    {
        var trimmed = band?.Trim();
        if (!string.IsNullOrWhiteSpace(trimmed))
        {
            var upper = trimmed.ToUpperInvariant();
            if (upper is "B1" or "B2" or "B3" or "B4" or "B5")
            {
                return upper;
            }
        }

        if (score.HasValue)
        {
            return score.Value switch
            {
                < 20 => "B1",
                < 40 => "B2",
                < 60 => "B3",
                < 80 => "B4",
                _ => "B5"
            };
        }

        return null;
    }

    private static (bool IsEligible, string? Reason) CheckEligibility(SessionSummary session, Dictionary<long, PlayerSportProfileInfo> profiles, HashSet<long> exceptions)
    {
        if (exceptions.Contains(session.Id))
        {
            return (true, null);
        }

        if (session.IsHourlyCourt || string.IsNullOrWhiteSpace(session.Band))
        {
            return (true, null);
        }

        if (!profiles.TryGetValue(session.SportId, out var profile))
        {
            return (false, "chưa có hồ sơ môn này");
        }

        if (string.IsNullOrWhiteSpace(profile.CurrentBand) || profile.CurrentScore == null || string.Equals(profile.RatingStatus, "PendingSurvey", StringComparison.OrdinalIgnoreCase) || string.Equals(profile.RatingStatus, "Unrated", StringComparison.OrdinalIgnoreCase))
        {
            return (false, "chưa làm survey");
        }

        if (!string.Equals(profile.CurrentBand.Trim(), session.Band.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            return (false, "lệch band kỹ năng");
        }

        return (true, null);
    }
}
