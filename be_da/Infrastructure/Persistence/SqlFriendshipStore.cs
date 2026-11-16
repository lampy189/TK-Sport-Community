using System.Data;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using be_da.Application.Abstractions;
using be_da.Application.Community;
using be_da.Domain.Entities;

namespace be_da.Infrastructure.Persistence;

public sealed class SqlFriendshipStore(string connectionString) : IFriendshipStore
{
    public async Task<long?> GetPlayerIdByUserIdAsync(long userId, CancellationToken cancellationToken)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        return await EnsurePlayerIdInternalAsync(connection, null, userId, cancellationToken);
    }

    public async Task<long?> GetUserIdByPlayerIdAsync(long playerId, CancellationToken cancellationToken)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var cmd = new SqlCommand("SELECT user_id FROM dbo.players WHERE id = @playerId;", connection);
        cmd.Parameters.Add("@playerId", SqlDbType.BigInt).Value = playerId;
        var result = await cmd.ExecuteScalarAsync(cancellationToken);
        return result is long id ? id : null;
    }

    private static async Task<long?> EnsurePlayerIdInternalAsync(
        SqlConnection connection,
        SqlTransaction? transaction,
        long userId,
        CancellationToken cancellationToken)
    {
        await using var cmd = new SqlCommand("""
            IF EXISTS (SELECT 1 FROM dbo.users WHERE id = @userId AND deleted_at IS NULL)
               AND NOT EXISTS (SELECT 1 FROM dbo.players WHERE user_id = @userId)
            BEGIN
                INSERT INTO dbo.players (user_id) VALUES (@userId);
            END
            SELECT id FROM dbo.players WHERE user_id = @userId;
            """, connection, transaction);
        cmd.Parameters.Add("@userId", SqlDbType.BigInt).Value = userId;
        var result = await cmd.ExecuteScalarAsync(cancellationToken);
        return result is long id ? id : (result != null && result != DBNull.Value ? Convert.ToInt64(result) : null);
    }

    public async Task<IReadOnlyList<MemberSearchResult>> SearchMembersAsync(long currentUserId, string? query, int limit, CancellationToken cancellationToken)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        await EnsurePlayerIdInternalAsync(connection, null, currentUserId, cancellationToken);

        const string sql = """
            DECLARE @mePlayerId BIGINT = (SELECT id FROM dbo.players WHERE user_id = @currentUserId);

            SELECT TOP (@limit)
                p.id AS player_id,
                u.id AS user_id,
                u.full_name,
                p.avatar_url,
                p.gender,
                f.id AS friendship_id,
                f.requester_id,
                f.addressee_id,
                f.status AS friendship_status
            FROM dbo.players p
            JOIN dbo.users u ON u.id = p.user_id
            LEFT JOIN dbo.friendships f ON (
                (f.requester_id = @mePlayerId AND f.addressee_id = p.id)
                OR (f.requester_id = p.id AND f.addressee_id = @mePlayerId)
            ) AND f.status IN ('Pending', 'Accepted')
            WHERE u.deleted_at IS NULL
              AND u.id <> @currentUserId
              AND (@query IS NULL OR u.full_name LIKE '%' + @query + '%')
            ORDER BY u.full_name;
            """;

        await using var cmd = new SqlCommand(sql, connection);
        cmd.Parameters.Add("@currentUserId", SqlDbType.BigInt).Value = currentUserId;
        cmd.Parameters.Add("@limit", SqlDbType.Int).Value = Math.Clamp(limit, 1, 100);
        cmd.Parameters.Add("@query", SqlDbType.NVarChar, 255).Value =
            string.IsNullOrWhiteSpace(query) ? DBNull.Value : query.Trim();

        var members = new List<MemberSearchResult>();
        await using (var reader = await cmd.ExecuteReaderAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                var playerId = reader.GetInt64(0);
                var userId = reader.GetInt64(1);
                var fullName = reader.GetString(2);
                var avatarUrl = reader.IsDBNull(3) ? null : reader.GetString(3);
                var gender = reader.IsDBNull(4) ? null : reader.GetString(4);
                long? friendshipId = reader.IsDBNull(5) ? null : reader.GetInt64(5);

                var status = "None";
                if (!reader.IsDBNull(8))
                {
                    var rawStatus = reader.GetString(8);
                    if (rawStatus == "Accepted") status = "Accepted";
                    else if (rawStatus == "Pending")
                    {
                        var requesterId = reader.GetInt64(6);
                        status = (requesterId == playerId) ? "PendingReceived" : "PendingSent";
                    }
                }

                members.Add(new MemberSearchResult(
                    playerId, userId, fullName, avatarUrl, gender, status, friendshipId, Array.Empty<SportProfileItem>()
                ));
            }
        }

        var finalResults = new List<MemberSearchResult>(members.Count);
        foreach (var m in members)
        {
            var sports = await QuerySportProfilesForPlayerAsync(connection, m.PlayerId, cancellationToken);
            var bio = await SqlProfileStore.LoadPlayerBioAsync(connection, null, m.UserId, cancellationToken);
            finalResults.Add(m with { SportProfiles = sports, Bio = bio });
        }

        return finalResults;
    }

    public async Task<IReadOnlyList<FriendSuggestionItem>> GetFriendSuggestionsAsync(long currentUserId, CancellationToken cancellationToken)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        await EnsurePlayerIdInternalAsync(connection, null, currentUserId, cancellationToken);

        const string sql = """
            DECLARE @mePlayerId BIGINT = (SELECT id FROM dbo.players WHERE user_id = @currentUserId);

            WITH SharedSlotStats AS (
                SELECT
                    t_other.player_id AS other_player_id,
                    COUNT(DISTINCT t_me.session_id) AS shared_count,
                    MAX(t_me.session_id) AS latest_session_id
                FROM dbo.tickets t_me
                JOIN dbo.tickets t_other ON t_other.session_id = t_me.session_id
                WHERE t_me.player_id = @mePlayerId
                  AND t_other.player_id <> @mePlayerId
                  AND LOWER(t_me.status) NOT IN ('cancelled', 'expired', 'refunded')
                  AND LOWER(t_other.status) NOT IN ('cancelled', 'expired', 'refunded')
                GROUP BY t_other.player_id
            )
            SELECT
                p.id AS player_id,
                u.id AS user_id,
                u.full_name,
                p.avatar_url,
                p.gender,
                f.id AS friendship_id,
                f.requester_id,
                f.addressee_id,
                f.status AS friendship_status,
                st.shared_count,
                (
                    SELECT TOP 1
                        (CASE WHEN LOWER(sp.code) = 'badminton' THEN N'Cầu lông' ELSE N'Pickleball' END)
                        + N' · ' + c.name
                        + N' (' + CONVERT(nvarchar(10), s.session_date, 103)
                        + CASE WHEN ts.label IS NOT NULL THEN N' · ' + ts.label ELSE N'' END + N')'
                    FROM dbo.sessions s
                    JOIN dbo.sports sp ON sp.id = s.sport_id
                    JOIN dbo.courts c ON c.id = s.court_id
                    LEFT JOIN dbo.time_slots ts ON ts.id = s.time_slot_id
                    WHERE s.id = st.latest_session_id
                ) AS latest_session_label
            FROM SharedSlotStats st
            JOIN dbo.players p ON p.id = st.other_player_id
            JOIN dbo.users u ON u.id = p.user_id
            LEFT JOIN dbo.friendships f ON (
                (f.requester_id = @mePlayerId AND f.addressee_id = p.id)
                OR (f.requester_id = p.id AND f.addressee_id = @mePlayerId)
            ) AND f.status IN ('Pending', 'Accepted')
            WHERE u.deleted_at IS NULL
              AND ISNULL(f.status, '') <> 'Accepted'
            ORDER BY st.shared_count DESC, u.full_name ASC;
            """;

        await using var cmd = new SqlCommand(sql, connection);
        cmd.Parameters.Add("@currentUserId", SqlDbType.BigInt).Value = currentUserId;

        var rawList = new List<FriendSuggestionItem>();
        await using (var reader = await cmd.ExecuteReaderAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                var playerId = reader.GetInt64(0);
                var userId = reader.GetInt64(1);
                var fullName = reader.GetString(2);
                var avatarUrl = reader.IsDBNull(3) ? null : reader.GetString(3);
                var gender = reader.IsDBNull(4) ? null : reader.GetString(4);
                long? friendshipId = reader.IsDBNull(5) ? null : reader.GetInt64(5);

                var status = "None";
                if (!reader.IsDBNull(8))
                {
                    var rawStatus = reader.GetString(8);
                    if (rawStatus == "Pending")
                    {
                        var requesterId = reader.GetInt64(6);
                        status = requesterId == playerId ? "PendingReceived" : "PendingSent";
                    }
                }

                var sharedCount = reader.GetInt32(9);
                var latestLabel = reader.IsDBNull(10) ? null : reader.GetString(10);

                rawList.Add(new FriendSuggestionItem(
                    playerId,
                    userId,
                    fullName,
                    avatarUrl,
                    gender,
                    status,
                    friendshipId,
                    sharedCount,
                    latestLabel,
                    Array.Empty<SportProfileItem>()
                ));
            }
        }

        var finalList = new List<FriendSuggestionItem>(rawList.Count);
        foreach (var item in rawList)
        {
            var sports = await QuerySportProfilesForPlayerAsync(connection, item.PlayerId, cancellationToken);
            var bio = await SqlProfileStore.LoadPlayerBioAsync(connection, null, item.UserId, cancellationToken);
            finalList.Add(item with { SportProfiles = sports, Bio = bio });
        }

        return finalList;
    }

    public async Task<IReadOnlyList<FriendItem>> GetFriendsAsync(long currentUserId, CancellationToken cancellationToken)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        await EnsurePlayerIdInternalAsync(connection, null, currentUserId, cancellationToken);

        const string sql = """
            DECLARE @mePlayerId BIGINT = (SELECT id FROM dbo.players WHERE user_id = @currentUserId);

            SELECT
                f.id AS friendship_id,
                CASE WHEN f.requester_id = @mePlayerId THEN f.addressee_id ELSE f.requester_id END AS friend_player_id,
                u.id AS friend_user_id,
                u.full_name,
                p.avatar_url,
                COALESCE(f.responded_at, f.created_at) AS connected_since,
                p.gender,
                ISNULL((
                    SELECT COUNT(*) FROM dbo.messages m
                    WHERE m.sender_id = u.id AND m.recipient_id = @currentUserId AND ISNULL(m.is_read, 0) = 0
                ), 0) AS unread_count,
                (
                    SELECT TOP 1 m.content FROM dbo.messages m
                    WHERE (m.sender_id = u.id AND m.recipient_id = @currentUserId)
                       OR (m.sender_id = @currentUserId AND m.recipient_id = u.id)
                    ORDER BY m.created_at DESC, m.id DESC
                ) AS last_message_preview,
                (
                    SELECT TOP 1 m.created_at FROM dbo.messages m
                    WHERE (m.sender_id = u.id AND m.recipient_id = @currentUserId)
                       OR (m.sender_id = @currentUserId AND m.recipient_id = u.id)
                    ORDER BY m.created_at DESC, m.id DESC
                ) AS last_message_at
            FROM dbo.friendships f
            JOIN dbo.players p ON p.id = (CASE WHEN f.requester_id = @mePlayerId THEN f.addressee_id ELSE f.requester_id END)
            JOIN dbo.users u ON u.id = p.user_id
            WHERE (f.requester_id = @mePlayerId OR f.addressee_id = @mePlayerId)
              AND f.status = 'Accepted'
              AND u.deleted_at IS NULL
            ORDER BY u.full_name;
            """;

        await using var cmd = new SqlCommand(sql, connection);
        cmd.Parameters.Add("@currentUserId", SqlDbType.BigInt).Value = currentUserId;

        var friends = new List<FriendItem>();
        await using (var reader = await cmd.ExecuteReaderAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                var friendshipId = reader.GetInt64(0);
                var friendPlayerId = reader.GetInt64(1);
                var friendUserId = reader.GetInt64(2);
                var fullName = reader.GetString(3);
                var avatarUrl = reader.IsDBNull(4) ? null : reader.GetString(4);
                var connectedSince = reader.GetDateTimeOffset(5);
                var gender = reader.IsDBNull(6) ? null : reader.GetString(6);
                var unreadCount = reader.IsDBNull(7) ? 0 : reader.GetInt32(7);
                var lastMsg = reader.IsDBNull(8) ? null : reader.GetString(8);
                var lastAt = reader.IsDBNull(9) ? null : (DateTimeOffset?)reader.GetDateTimeOffset(9);

                friends.Add(new FriendItem(
                    friendshipId,
                    friendPlayerId,
                    friendUserId,
                    fullName,
                    avatarUrl,
                    connectedSince,
                    Array.Empty<SportProfileItem>(),
                    gender,
                    null,
                    unreadCount,
                    lastMsg,
                    lastAt
                ));
            }
        }

        var results = new List<FriendItem>(friends.Count);
        foreach (var f in friends)
        {
            var sports = await QuerySportProfilesForPlayerAsync(connection, f.FriendPlayerId, cancellationToken);
            var bio = await SqlProfileStore.LoadPlayerBioAsync(connection, null, f.FriendUserId, cancellationToken);
            results.Add(f with { SportProfiles = sports, Bio = bio });
        }

        return results;
    }

    public async Task<IReadOnlyList<PendingFriendRequestItem>> GetPendingRequestsAsync(long currentUserId, CancellationToken cancellationToken)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        await EnsurePlayerIdInternalAsync(connection, null, currentUserId, cancellationToken);

        const string sql = """
            DECLARE @mePlayerId BIGINT = (SELECT id FROM dbo.players WHERE user_id = @currentUserId);

            SELECT
                f.id AS friendship_id,
                CASE WHEN f.requester_id = @mePlayerId THEN f.addressee_id ELSE f.requester_id END AS other_player_id,
                u.id AS other_user_id,
                u.full_name,
                p.avatar_url,
                f.created_at,
                CASE WHEN f.addressee_id = @mePlayerId THEN 1 ELSE 0 END AS is_incoming,
                p.gender
            FROM dbo.friendships f
            JOIN dbo.players p ON p.id = (CASE WHEN f.requester_id = @mePlayerId THEN f.addressee_id ELSE f.requester_id END)
            JOIN dbo.users u ON u.id = p.user_id
            WHERE (f.requester_id = @mePlayerId OR f.addressee_id = @mePlayerId)
              AND f.status = 'Pending'
              AND u.deleted_at IS NULL
            ORDER BY f.created_at DESC;
            """;

        await using var cmd = new SqlCommand(sql, connection);
        cmd.Parameters.Add("@currentUserId", SqlDbType.BigInt).Value = currentUserId;

        var items = new List<PendingFriendRequestItem>();
        await using (var reader = await cmd.ExecuteReaderAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                items.Add(new PendingFriendRequestItem(
                    reader.GetInt64(0),
                    reader.GetInt64(1),
                    reader.GetInt64(2),
                    reader.GetString(3),
                    reader.IsDBNull(4) ? null : reader.GetString(4),
                    reader.GetDateTimeOffset(5),
                    reader.GetInt32(6) == 1,
                    reader.IsDBNull(7) ? null : reader.GetString(7)
                ));
            }
        }

        var results = new List<PendingFriendRequestItem>(items.Count);
        foreach (var item in items)
        {
            var sports = await QuerySportProfilesForPlayerAsync(connection, item.OtherPlayerId, cancellationToken);
            var bio = await SqlProfileStore.LoadPlayerBioAsync(connection, null, item.OtherUserId, cancellationToken);
            results.Add(item with { SportProfiles = sports, Bio = bio });
        }

        return results;
    }

    public async Task<FriendshipActionResult> SendFriendRequestAsync(long currentUserId, long targetPlayerId, CancellationToken cancellationToken)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken);

        try
        {
            var mePlayerIdNullable = await EnsurePlayerIdInternalAsync(connection, transaction, currentUserId, cancellationToken);
            if (!mePlayerIdNullable.HasValue)
            {
                await transaction.RollbackAsync(cancellationToken);
                return new FriendshipActionResult(false, "player_not_found", "You must have an active player profile to connect with friends.", null);
            }
            var mePlayerId = mePlayerIdNullable.Value;

            if (mePlayerId == targetPlayerId)
            {
                await transaction.RollbackAsync(cancellationToken);
                return new FriendshipActionResult(false, "cannot_friend_self", "You cannot send a friend request to yourself.", null);
            }

            await using var targetCmd = new SqlCommand("""
                SELECT p.id, u.status FROM dbo.players p
                JOIN dbo.users u ON u.id = p.user_id
                WHERE p.id = @targetPlayerId AND u.deleted_at IS NULL;
                """, connection, transaction);
            targetCmd.Parameters.Add("@targetPlayerId", SqlDbType.BigInt).Value = targetPlayerId;
            bool targetFound = false;
            await using (var reader = await targetCmd.ExecuteReaderAsync(cancellationToken))
            {
                if (await reader.ReadAsync(cancellationToken))
                {
                    targetFound = true;
                }
            }

            if (!targetFound)
            {
                await transaction.RollbackAsync(cancellationToken);
                return new FriendshipActionResult(false, "target_player_not_found", "Target player was not found or inactive.", null);
            }

            await using var checkExistingCmd = new SqlCommand("""
                SELECT id, status, requester_id
                FROM dbo.friendships
                WHERE ((requester_id = @mePlayerId AND addressee_id = @targetPlayerId)
                    OR (requester_id = @targetPlayerId AND addressee_id = @mePlayerId))
                  AND status IN ('Pending', 'Accepted');
                """, connection, transaction);
            checkExistingCmd.Parameters.Add("@mePlayerId", SqlDbType.BigInt).Value = mePlayerId;
            checkExistingCmd.Parameters.Add("@targetPlayerId", SqlDbType.BigInt).Value = targetPlayerId;

            string? existingStatus = null;
            await using (var reader = await checkExistingCmd.ExecuteReaderAsync(cancellationToken))
            {
                if (await reader.ReadAsync(cancellationToken))
                {
                    existingStatus = reader.GetString(1);
                }
            }

            if (existingStatus != null)
            {
                await transaction.RollbackAsync(cancellationToken);
                if (existingStatus == "Accepted")
                    return new FriendshipActionResult(false, "already_friends", "You are already friends with this player.", null);
                return new FriendshipActionResult(false, "friend_request_pending", "A pending friend request already exists between you.", null);
            }

            await using var insertCmd = new SqlCommand("""
                INSERT INTO dbo.friendships (requester_id, addressee_id, status)
                VALUES (@mePlayerId, @targetPlayerId, 'Pending');
                SELECT SCOPE_IDENTITY();
                """, connection, transaction);
            insertCmd.Parameters.Add("@mePlayerId", SqlDbType.BigInt).Value = mePlayerId;
            insertCmd.Parameters.Add("@targetPlayerId", SqlDbType.BigInt).Value = targetPlayerId;
            var friendshipId = Convert.ToInt64(await insertCmd.ExecuteScalarAsync(cancellationToken));

            await using var auditCmd = new SqlCommand("""
                INSERT INTO dbo.account_audit_logs (user_id, actor_id, action, new_value)
                VALUES (@userId, @userId, 'SendFriendRequest', @val);
                """, connection, transaction);
            auditCmd.Parameters.Add("@userId", SqlDbType.BigInt).Value = currentUserId;
            auditCmd.Parameters.Add("@val", SqlDbType.NVarChar, -1).Value = JsonSerializer.Serialize(new { TargetPlayerId = targetPlayerId, FriendshipId = friendshipId });
            await auditCmd.ExecuteNonQueryAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);
            return new FriendshipActionResult(true, null, null, friendshipId);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<FriendshipActionResult> RespondFriendRequestAsync(long currentUserId, long friendshipId, bool accept, CancellationToken cancellationToken)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken);

        try
        {
            var mePlayerIdNullable = await EnsurePlayerIdInternalAsync(connection, transaction, currentUserId, cancellationToken);
            if (!mePlayerIdNullable.HasValue)
            {
                await transaction.RollbackAsync(cancellationToken);
                return new FriendshipActionResult(false, "player_not_found", "Player record not found.", null);
            }
            var mePlayerId = mePlayerIdNullable.Value;

            await using var selectCmd = new SqlCommand("""
                SELECT requester_id, addressee_id, status
                FROM dbo.friendships
                WHERE id = @friendshipId;
                """, connection, transaction);
            selectCmd.Parameters.Add("@friendshipId", SqlDbType.BigInt).Value = friendshipId;

            long? addresseeId = null;
            long? requesterId = null;
            string? status = null;
            await using (var reader = await selectCmd.ExecuteReaderAsync(cancellationToken))
            {
                if (await reader.ReadAsync(cancellationToken))
                {
                    requesterId = reader.GetInt64(0);
                    addresseeId = reader.GetInt64(1);
                    status = reader.GetString(2);
                }
            }

            if (!requesterId.HasValue)
            {
                await transaction.RollbackAsync(cancellationToken);
                return new FriendshipActionResult(false, "request_not_found", "Friend request was not found.", null);
            }

            if (status != "Pending")
            {
                await transaction.RollbackAsync(cancellationToken);
                return new FriendshipActionResult(false, "request_not_pending", "This friend request is no longer pending.", null);
            }

            if (addresseeId != mePlayerId)
            {
                await transaction.RollbackAsync(cancellationToken);
                return new FriendshipActionResult(false, "unauthorized_to_respond", "Only the recipient can accept or decline this friend request.", null);
            }

            var newStatus = accept ? "Accepted" : "Declined";

            await using var updateCmd = new SqlCommand("""
                UPDATE dbo.friendships
                SET status = @status, responded_at = SYSDATETIMEOFFSET()
                WHERE id = @friendshipId;
                """, connection, transaction);
            updateCmd.Parameters.Add("@status", SqlDbType.VarChar, 50).Value = newStatus;
            updateCmd.Parameters.Add("@friendshipId", SqlDbType.BigInt).Value = friendshipId;
            await updateCmd.ExecuteNonQueryAsync(cancellationToken);

            await using var auditCmd = new SqlCommand("""
                INSERT INTO dbo.account_audit_logs (user_id, actor_id, action, new_value)
                VALUES (@userId, @userId, @action, @val);
                """, connection, transaction);
            auditCmd.Parameters.Add("@userId", SqlDbType.BigInt).Value = currentUserId;
            auditCmd.Parameters.Add("@action", SqlDbType.VarChar, 100).Value = accept ? "AcceptFriendRequest" : "DeclineFriendRequest";
            auditCmd.Parameters.Add("@val", SqlDbType.NVarChar, -1).Value = JsonSerializer.Serialize(new { FriendshipId = friendshipId, RequesterId = requesterId });
            await auditCmd.ExecuteNonQueryAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);
            return new FriendshipActionResult(true, null, null, friendshipId);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<FriendshipActionResult> RemoveFriendAsync(long currentUserId, long friendPlayerId, CancellationToken cancellationToken)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        const string sql = """
            DECLARE @mePlayerId BIGINT = (SELECT id FROM dbo.players WHERE user_id = @currentUserId);

            UPDATE dbo.friendships
            SET status = 'Removed'
            WHERE ((requester_id = @mePlayerId AND addressee_id = @friendPlayerId)
                OR (requester_id = @friendPlayerId AND addressee_id = @mePlayerId))
              AND status IN ('Accepted', 'Pending');
            """;

        await using var cmd = new SqlCommand(sql, connection);
        cmd.Parameters.Add("@currentUserId", SqlDbType.BigInt).Value = currentUserId;
        cmd.Parameters.Add("@friendPlayerId", SqlDbType.BigInt).Value = friendPlayerId;

        var affected = await cmd.ExecuteNonQueryAsync(cancellationToken);
        if (affected == 0)
        {
            return new FriendshipActionResult(false, "friendship_not_found", "Active friendship was not found.", null);
        }

        return new FriendshipActionResult(true, null, null, null);
    }

    public async Task<bool> AreFriendsAsync(long currentUserId, long otherPlayerId, CancellationToken cancellationToken)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        const string sql = """
            DECLARE @mePlayerId BIGINT = (SELECT id FROM dbo.players WHERE user_id = @currentUserId);

            SELECT COUNT(*) FROM dbo.friendships
            WHERE ((requester_id = @mePlayerId AND addressee_id = @otherPlayerId)
                OR (requester_id = @otherPlayerId AND addressee_id = @mePlayerId))
              AND status = 'Accepted';
            """;

        await using var cmd = new SqlCommand(sql, connection);
        cmd.Parameters.Add("@currentUserId", SqlDbType.BigInt).Value = currentUserId;
        cmd.Parameters.Add("@otherPlayerId", SqlDbType.BigInt).Value = otherPlayerId;

        return (int)(await cmd.ExecuteScalarAsync(cancellationToken))! > 0;
    }

    public async Task SaveFcmTokenAsync(long userId, string fcmToken, string? deviceType, CancellationToken cancellationToken)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        const string sql = """
            IF EXISTS (SELECT 1 FROM dbo.user_fcm_tokens WHERE fcm_token = @token)
            BEGIN
                UPDATE dbo.user_fcm_tokens
                SET user_id = @userId, device_type = @deviceType, updated_at = SYSDATETIMEOFFSET()
                WHERE fcm_token = @token;
            END
            ELSE
            BEGIN
                INSERT INTO dbo.user_fcm_tokens (user_id, fcm_token, device_type)
                VALUES (@userId, @token, @deviceType);
            END
            """;

        await using var cmd = new SqlCommand(sql, connection);
        cmd.Parameters.Add("@userId", SqlDbType.BigInt).Value = userId;
        cmd.Parameters.Add("@token", SqlDbType.NVarChar, 512).Value = fcmToken;
        cmd.Parameters.Add("@deviceType", SqlDbType.VarChar, 50).Value =
            (object?)deviceType ?? DBNull.Value;

        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<string>> GetFcmTokensByUserIdAsync(long userId, CancellationToken cancellationToken)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var cmd = new SqlCommand("SELECT fcm_token FROM dbo.user_fcm_tokens WHERE user_id = @userId;", connection);
        cmd.Parameters.Add("@userId", SqlDbType.BigInt).Value = userId;

        var tokens = new List<string>();
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            tokens.Add(reader.GetString(0));
        }
        return tokens;
    }

    public async Task<ChatMessageItem?> SendMessageAsync(long currentUserId, long recipientPlayerId, string content, CancellationToken cancellationToken)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        var senderPlayerId = await GetPlayerIdByUserIdAsync(currentUserId, cancellationToken);
        if (!senderPlayerId.HasValue) return null;

        var areFriends = await AreFriendsAsync(currentUserId, recipientPlayerId, cancellationToken);
        if (!areFriends) return null;

        var recipientUserId = await GetUserIdByPlayerIdAsync(recipientPlayerId, cancellationToken);
        if (!recipientUserId.HasValue) return null;

        const string sql = """
            INSERT INTO dbo.messages (sender_id, recipient_id, content, is_read, created_at)
            OUTPUT INSERTED.id, INSERTED.created_at
            VALUES (@senderUserId, @recipientUserId, @content, 0, SYSDATETIMEOFFSET());
            """;

        await using var cmd = new SqlCommand(sql, connection);
        cmd.Parameters.Add("@senderUserId", SqlDbType.BigInt).Value = currentUserId;
        cmd.Parameters.Add("@recipientUserId", SqlDbType.BigInt).Value = recipientUserId.Value;
        cmd.Parameters.Add("@content", SqlDbType.NVarChar, 2000).Value = content;

        long msgId;
        DateTimeOffset createdAt;
        await using (var reader = await cmd.ExecuteReaderAsync(cancellationToken))
        {
            if (!await reader.ReadAsync(cancellationToken)) return null;
            msgId = reader.GetInt64(0);
            createdAt = reader.GetDateTimeOffset(1);
        }

        await using var nameCmd = new SqlCommand("SELECT full_name FROM dbo.users WHERE id = @userId;", connection);
        nameCmd.Parameters.Add("@userId", SqlDbType.BigInt).Value = currentUserId;
        var senderFullName = (string?)await nameCmd.ExecuteScalarAsync(cancellationToken) ?? "Player";

        return new ChatMessageItem(
            msgId,
            senderPlayerId.Value,
            currentUserId,
            senderFullName,
            recipientPlayerId,
            recipientUserId.Value,
            content,
            false,
            createdAt,
            true
        );
    }

    public async Task<IReadOnlyList<ChatMessageItem>> GetConversationAsync(long currentUserId, long otherPlayerId, int limit, CancellationToken cancellationToken)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        var mePlayerId = await GetPlayerIdByUserIdAsync(currentUserId, cancellationToken);
        if (!mePlayerId.HasValue) return Array.Empty<ChatMessageItem>();

        var areFriends = await AreFriendsAsync(currentUserId, otherPlayerId, cancellationToken);
        if (!areFriends) return Array.Empty<ChatMessageItem>();

        var otherUserId = await GetUserIdByPlayerIdAsync(otherPlayerId, cancellationToken);
        if (!otherUserId.HasValue) return Array.Empty<ChatMessageItem>();

        const string sql = """
            SELECT TOP (@limit)
                m.id,
                m.sender_id,
                su.full_name AS sender_name,
                sp.id AS sender_player_id,
                m.recipient_id,
                rp.id AS recipient_player_id,
                m.content,
                m.is_read,
                m.created_at
            FROM dbo.messages m
            JOIN dbo.users su ON su.id = m.sender_id
            JOIN dbo.players sp ON sp.user_id = su.id
            JOIN dbo.users ru ON ru.id = m.recipient_id
            JOIN dbo.players rp ON rp.user_id = ru.id
            WHERE (m.sender_id = @currentUserId AND m.recipient_id = @otherUserId)
               OR (m.sender_id = @otherUserId AND m.recipient_id = @currentUserId)
            ORDER BY m.created_at ASC, m.id ASC;
            """;

        await using var cmd = new SqlCommand(sql, connection);
        cmd.Parameters.Add("@limit", SqlDbType.Int).Value = Math.Clamp(limit, 1, 100);
        cmd.Parameters.Add("@currentUserId", SqlDbType.BigInt).Value = currentUserId;
        cmd.Parameters.Add("@otherUserId", SqlDbType.BigInt).Value = otherUserId.Value;

        var list = new List<ChatMessageItem>();
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var msgId = reader.GetInt64(0);
            var senderUserId = reader.GetInt64(1);
            var senderFullName = reader.GetString(2);
            var senderPlayerId = reader.GetInt64(3);
            var recipientUserId = reader.GetInt64(4);
            var recipientPlayerId = reader.GetInt64(5);
            var content = reader.GetString(6);
            var isRead = !reader.IsDBNull(7) && reader.GetBoolean(7);
            var createdAt = reader.GetDateTimeOffset(8);

            list.Add(new ChatMessageItem(
                msgId,
                senderPlayerId,
                senderUserId,
                senderFullName,
                recipientPlayerId,
                recipientUserId,
                content,
                isRead,
                createdAt,
                senderUserId == currentUserId
            ));
        }

        return list;
    }

    public async Task<bool> MarkMessagesAsReadAsync(long currentUserId, long senderPlayerId, CancellationToken cancellationToken)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        var senderUserId = await GetUserIdByPlayerIdAsync(senderPlayerId, cancellationToken);
        if (!senderUserId.HasValue) return false;

        const string sql = """
            UPDATE dbo.messages
            SET is_read = 1
            WHERE sender_id = @senderUserId AND recipient_id = @currentUserId AND is_read = 0;
            """;

        await using var cmd = new SqlCommand(sql, connection);
        cmd.Parameters.Add("@senderUserId", SqlDbType.BigInt).Value = senderUserId.Value;
        cmd.Parameters.Add("@currentUserId", SqlDbType.BigInt).Value = currentUserId;

        await cmd.ExecuteNonQueryAsync(cancellationToken);
        return true;
    }

    private static async Task<List<SportProfileItem>> QuerySportProfilesForPlayerAsync(SqlConnection connection, long playerId, CancellationToken cancellationToken)
    {
        var sports = new List<SportProfileItem>();
        await using var sportsCmd = new SqlCommand("""
            SELECT
                s.id,
                s.code,
                s.name,
                sp.current_score,
                sp.current_band,
                CASE
                    WHEN sp.id IS NOT NULL AND EXISTS (
                        SELECT 1 FROM dbo.assessments a
                        WHERE a.player_sport_profile_id = sp.id
                          AND LOWER(REPLACE(a.assessment_type, '_', '')) = 'oncourt'
                          AND LOWER(a.status) IN ('published', 'submitted')
                    ) THEN 'Rated'
                    ELSE sp.rating_status
                END AS rating_status
            FROM dbo.sports s
            LEFT JOIN dbo.player_sport_profiles sp ON sp.sport_id = s.id AND sp.player_id = @playerId
            ORDER BY s.code;
            """, connection);
        sportsCmd.Parameters.Add("@playerId", SqlDbType.BigInt).Value = playerId;

        await using var reader = await sportsCmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var sportId = reader.GetInt64(0);
            var code = reader.GetString(1);
            var name = reader.GetString(2);
            short? score = reader.IsDBNull(3) ? null : reader.GetInt16(3);
            var rawBand = reader.IsDBNull(4) ? null : reader.GetString(4);
            var band = SqlSessionStore.NormalizeBand(rawBand, score) ?? "Unrated";
            var status = reader.IsDBNull(5) ? "Unrated" : reader.GetString(5);

            sports.Add(new SportProfileItem(code, name, score, band, status, sportId));
        }

        return sports;
    }
}

