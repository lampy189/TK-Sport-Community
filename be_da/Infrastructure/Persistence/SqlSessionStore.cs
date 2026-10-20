using System.Data;
using Microsoft.Data.SqlClient;
using be_da.Application.Abstractions;
using be_da.Application.Sessions;
using be_da.Domain.Entities;

namespace be_da.Infrastructure.Persistence;

public sealed class SqlSessionStore(string connectionString) : ISessionStore
{
    public async Task<IReadOnlyList<SessionSummary>> GetSessionsAsync(SessionFilter filter, long? currentUserId, CancellationToken cancellationToken)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        const string sql = """
            SELECT 
                s.id, s.sport_id, sp.code AS sport_code, sp.name AS sport_name,
                s.court_id, c.name AS court_name, v.name AS venue_name,
                s.time_slot_id, ts.start_time, ts.end_time, ts.label AS time_label,
                s.session_date, s.band, s.capacity, s.price_vnd, s.status, s.created_by,
                ISNULL(occ.occupied_count, 0) AS occupied_places
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
                      t.status IN ('Valid', 'Paid', 'Confirmed', 'CheckedIn')
                      OR (t.status = 'Pending' AND o.expires_at > SYSUTCDATETIME())
                  )
            ) occ
            WHERE (@status IS NOT NULL AND s.status = @status OR @status IS NULL AND s.status IN ('Open', 'Published'))
              AND (@sportId IS NULL OR s.sport_id = @sportId)
              AND (@sport IS NULL OR sp.code = @sport OR sp.name = @sport)
              AND (@sessionDate IS NULL OR s.session_date = @sessionDate)
              AND (@timeSlotId IS NULL OR s.time_slot_id = @timeSlotId)
            ORDER BY s.session_date ASC, ts.start_time ASC;
            """;

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add("@status", SqlDbType.VarChar, 100).Value = (object?)filter.Status ?? DBNull.Value;
        command.Parameters.Add("@sportId", SqlDbType.BigInt).Value = (object?)filter.SportId ?? DBNull.Value;
        command.Parameters.Add("@sport", SqlDbType.NVarChar, 100).Value = (object?)filter.Sport ?? DBNull.Value;
        command.Parameters.Add("@sessionDate", SqlDbType.Date).Value = filter.Date.HasValue
            ? filter.Date.Value.ToDateTime(TimeOnly.MinValue) : DBNull.Value;
        command.Parameters.Add("@timeSlotId", SqlDbType.BigInt).Value = (object?)filter.TimeSlotId ?? DBNull.Value;

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
        var (profiles, exceptions) = await LoadPlayerProfilesAndExceptionsAsync(connection, currentUserId.Value, cancellationToken);
        var result = new List<SessionSummary>(rawSessions.Count);
        foreach (var session in rawSessions)
        {
            var (eligible, reason) = CheckEligibility(session, profiles, exceptions);
            result.Add(session with { IsEligibleSuggestion = eligible, IneligibilityReason = reason });
        }

        return result;
    }

    public async Task<SessionDetails?> GetSessionByIdAsync(long id, long? currentUserId, CancellationToken cancellationToken)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        const string sessionSql = """
            SELECT 
                s.id, s.sport_id, sp.code AS sport_code, sp.name AS sport_name,
                s.court_id, c.name AS court_name, v.name AS venue_name,
                s.time_slot_id, ts.start_time, ts.end_time, ts.label AS time_label,
                s.session_date, s.band, s.capacity, s.price_vnd, s.status, s.created_by,
                ISNULL(occ.occupied_count, 0) AS occupied_places
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
                      t.status IN ('Valid', 'Paid', 'Confirmed', 'CheckedIn')
                      OR (t.status = 'Pending' AND o.expires_at > SYSUTCDATETIME())
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

        if (summary == null) return null;

        // Fetch public participants
        const string participantsSql = """
            SELECT 
                t.id AS ticket_id,
                p.id AS player_id,
                u.full_name,
                p.avatar_url,
                psp.current_band,
                t.status AS ticket_status
            FROM dbo.tickets t
            JOIN dbo.players p ON p.id = t.player_id
            JOIN dbo.users u ON u.id = p.user_id
            LEFT JOIN dbo.social_booking_orders o ON o.id = t.order_id
            LEFT JOIN dbo.player_sport_profiles psp ON psp.player_id = p.id AND psp.sport_id = @sportId
            WHERE t.session_id = @sessionId
              AND (
                  t.status IN ('Valid', 'Paid', 'Confirmed', 'CheckedIn')
                  OR (t.status = 'Pending' AND o.expires_at > SYSUTCDATETIME())
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
                participants.Add(new SessionParticipant(
                    TicketId: reader.GetInt64(0),
                    PlayerId: reader.GetInt64(1),
                    FullName: reader.GetString(2),
                    AvatarUrl: reader.IsDBNull(3) ? null : reader.GetString(3),
                    CurrentBand: reader.IsDBNull(4) ? null : reader.GetString(4),
                    TicketStatus: reader.GetString(5)
                ));
            }
        }

        if (currentUserId.HasValue)
        {
            var (profiles, exceptions) = await LoadPlayerProfilesAndExceptionsAsync(connection, currentUserId.Value, cancellationToken);
            var (eligible, reason) = CheckEligibility(summary, profiles, exceptions);
            summary = summary with { IsEligibleSuggestion = eligible, IneligibilityReason = reason };
        }

        return new SessionDetails(summary, participants);
    }

    public async Task<RegisterSessionResult> RegisterSessionAsync(long userId, long sessionId, CancellationToken cancellationToken)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        try
        {
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
                SELECT id, sport_id, band, capacity, price_vnd, status
                FROM dbo.sessions WITH (UPDLOCK, HOLDLOCK)
                WHERE id = @sessionId;
                """;

            long sportId;
            string? sessionBand;
            short capacity;
            int priceVnd;
            string sessionStatus;

            await using (var sessionCmd = new SqlCommand(sessionLockSql, connection, transaction))
            {
                sessionCmd.Parameters.Add("@sessionId", SqlDbType.BigInt).Value = sessionId;
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
            }

            if (!string.Equals(sessionStatus, "Open", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(sessionStatus, "Published", StringComparison.OrdinalIgnoreCase))
            {
                await transaction.RollbackAsync(cancellationToken);
                return new RegisterSessionResult(false, "SESSION_NOT_OPEN", "Buổi chơi chưa mở hoặc đã kết thúc đăng ký.");
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

            if (!hasApprovedException && !string.IsNullOrWhiteSpace(sessionBand))
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
                        playerBand = reader.IsDBNull(1) ? null : reader.GetString(1);
                        ratingStatus = reader.IsDBNull(2) ? null : reader.GetString(2);
                    }
                }

                if (!profileFound)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return new RegisterSessionResult(false, "SKILL_INELIGIBLE", "Người chơi chưa có hồ sơ kỹ năng cho môn này.", "chưa có hồ sơ môn này");
                }

                if (string.IsNullOrWhiteSpace(playerBand) || currentScore == null ||
                    string.Equals(ratingStatus, "PendingSurvey", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(ratingStatus, "Unrated", StringComparison.OrdinalIgnoreCase))
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
                      t.status IN ('Valid', 'Paid', 'Confirmed', 'CheckedIn')
                      OR (t.status = 'Pending' AND o.expires_at > SYSUTCDATETIME())
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
                      t.status IN ('Valid', 'Paid', 'Confirmed', 'CheckedIn')
                      OR (t.status = 'Pending' AND o.expires_at > SYSUTCDATETIME())
                  );
                """, connection, transaction))
            {
                occCmd.Parameters.Add("@sessionId", SqlDbType.BigInt).Value = sessionId;
                occupiedCount = Convert.ToInt32(await occCmd.ExecuteScalarAsync(cancellationToken));
            }

            if (occupiedCount >= capacity)
            {
                await transaction.RollbackAsync(cancellationToken);
                return new RegisterSessionResult(false, "SESSION_FULL", "Buổi chơi đã đủ người.");
            }

            // Bước C: Record Pending Registration (Timed place hold)
            var expiresAt = DateTimeOffset.UtcNow.AddMinutes(15);
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
                insertOrderCmd.Parameters.Add("@payerId", SqlDbType.BigInt).Value = userId;
                insertOrderCmd.Parameters.Add("@totalAmount", SqlDbType.Int).Value = priceVnd;
                insertOrderCmd.Parameters.Add("@expiresAt", SqlDbType.DateTimeOffset, 0).Value = expiresAt;
                orderId = (long)(await insertOrderCmd.ExecuteScalarAsync(cancellationToken))!;
            }

            var ticketCode = $"TKT-{orderId}-{DateTimeOffset.UtcNow:yyMMddHHmmss}-{Random.Shared.Next(100, 999)}";
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

            await transaction.CommitAsync(cancellationToken);

            return new RegisterSessionResult(
                Success: true,
                ErrorCode: null,
                Message: "Giữ chỗ thành công. Vui lòng thanh toán trước khi hết hạn.",
                Reason: null,
                OrderId: orderId,
                OrderCode: orderCode,
                TicketId: ticketId,
                TicketCode: ticketCode,
                AmountVnd: priceVnd,
                ExpiresAt: expiresAt
            );
        }
        catch (SqlException ex) when (ex.Number is 2601 or 2627)
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

        return new SessionSummary(
            Id: reader.GetInt64(0),
            SportId: reader.GetInt64(1),
            SportCode: reader.GetString(2),
            SportName: reader.GetString(3),
            CourtId: reader.GetInt64(4),
            CourtName: reader.GetString(5),
            VenueName: reader.GetString(6),
            TimeSlotId: reader.GetInt64(7),
            StartTime: startTime,
            EndTime: endTime,
            TimeLabel: reader.IsDBNull(10) ? $"{startTime:hh\\:mm} - {endTime:hh\\:mm}" : reader.GetString(10),
            SessionDate: date,
            Band: reader.IsDBNull(12) ? null : reader.GetString(12),
            Capacity: reader.GetInt16(13),
            PriceVnd: reader.GetInt32(14),
            Status: reader.GetString(15),
            CreatedBy: reader.GetInt64(16),
            OccupiedPlaces: Convert.ToInt16(reader.GetInt32(17))
        );
    }

    private static async Task<(Dictionary<long, PlayerSportProfileInfo> Profiles, HashSet<long> Exceptions)> LoadPlayerProfilesAndExceptionsAsync(
        SqlConnection connection, long userId, CancellationToken cancellationToken)
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
            var band = reader.IsDBNull(1) ? null : reader.GetString(1);
            var score = reader.IsDBNull(2) ? null : (short?)reader.GetInt16(2);
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

    private static (bool IsEligible, string? Reason) CheckEligibility(
        SessionSummary session,
        Dictionary<long, PlayerSportProfileInfo> profiles,
        HashSet<long> exceptions)
    {
        if (exceptions.Contains(session.Id))
        {
            return (true, null);
        }

        if (string.IsNullOrWhiteSpace(session.Band))
        {
            return (true, null);
        }

        if (!profiles.TryGetValue(session.SportId, out var profile))
        {
            return (false, "chưa có hồ sơ môn này");
        }

        if (string.IsNullOrWhiteSpace(profile.CurrentBand) || profile.CurrentScore == null ||
            string.Equals(profile.RatingStatus, "PendingSurvey", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(profile.RatingStatus, "Unrated", StringComparison.OrdinalIgnoreCase))
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
