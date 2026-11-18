using be_da.Application.DTOs;
using System.Data;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using be_da.Application.Interfaces.Repositories;
using be_da.Domain.Entities;

namespace be_da.Infrastructure.Persistence.Repositories;
public sealed class SqlProfileStore(string connectionString, be_da.Application.Interfaces.External.IDataAccessContext? dataScope = null) : IProfileStore
{
    public async Task<UserProfileDetails?> GetProfileAsync(long userId, CancellationToken cancellationToken)
    {
        await using var connection = new SqlConnection(connectionString);
        await be_da.Infrastructure.Persistence.SportCenterConnection.OpenAsync(connection, cancellationToken, dataScope);
        await using var userCmd = new SqlCommand("""
            SELECT u.id, u.email, u.full_name, r.code, u.status, p.id AS player_id, p.date_of_birth, p.gender, p.avatar_url
            FROM dbo.users u
            JOIN dbo.roles r ON r.id = u.role_id
            LEFT JOIN dbo.players p ON p.user_id = u.id
            WHERE u.id = @userId AND u.deleted_at IS NULL;
            """, connection);
        userCmd.Parameters.Add("@userId", SqlDbType.BigInt).Value = userId;
        UserProfileHeader? header = null;
        await using (var reader = await userCmd.ExecuteReaderAsync(cancellationToken))
        {
            if (await reader.ReadAsync(cancellationToken))
            {
                header = new UserProfileHeader(reader.GetInt64(0), reader.GetString(1), reader.GetString(2), reader.GetString(3), reader.GetString(4), reader.IsDBNull(5) ? null : reader.GetInt64(5), reader.IsDBNull(6) ? null : DateOnly.FromDateTime(reader.GetDateTime(6)), reader.IsDBNull(7) ? null : reader.GetString(7), reader.IsDBNull(8) ? null : reader.GetString(8));
            }
        }

        if (header is null)
            return null;
        long? playerId = header.PlayerId;
        if (!playerId.HasValue && string.Equals(header.Role, "Player", StringComparison.OrdinalIgnoreCase))
        {
            await using var ensureCmd = new SqlCommand("""
                IF NOT EXISTS (SELECT 1 FROM dbo.players WHERE user_id = @userId)
                    INSERT INTO dbo.players (user_id) VALUES (@userId);
                SELECT id FROM dbo.players WHERE user_id = @userId;
                """, connection);
            ensureCmd.Parameters.Add("@userId", SqlDbType.BigInt).Value = userId;
            playerId = Convert.ToInt64(await ensureCmd.ExecuteScalarAsync(cancellationToken));
        }

        var bio = await LoadPlayerBioAsync(connection, null, userId, cancellationToken);
        var sports = await QuerySportProfilesAsync(connection, userId, cancellationToken);
        return new UserProfileDetails(header.Id, header.Email, header.FullName, header.Role, header.Status, header.DateOfBirth, header.Gender, header.AvatarUrl, sports, playerId, bio);
    }

    public async Task<PublicPlayerProfileDetails?> GetPublicPlayerProfileAsync(long playerId, long? viewerUserId, CancellationToken cancellationToken)
    {
        await using var connection = new SqlConnection(connectionString);
        await be_da.Infrastructure.Persistence.SportCenterConnection.OpenAsync(connection, cancellationToken, dataScope);
        await using var cmd = new SqlCommand("""
            SELECT p.id, u.id AS user_id, u.full_name, p.avatar_url, p.gender, p.date_of_birth
            FROM dbo.players p
            JOIN dbo.users u ON u.id = p.user_id
            WHERE p.id = @playerId AND u.deleted_at IS NULL;
            """, connection);
        cmd.Parameters.Add("@playerId", SqlDbType.BigInt).Value = playerId;
        long targetUserId;
        string fullName;
        string? avatarUrl;
        string? gender;
        DateOnly? dateOfBirth;
        await using (var reader = await cmd.ExecuteReaderAsync(cancellationToken))
        {
            if (!await reader.ReadAsync(cancellationToken))
            {
                return null;
            }

            targetUserId = reader.GetInt64(1);
            fullName = reader.GetString(2);
            avatarUrl = reader.IsDBNull(3) ? null : reader.GetString(3);
            gender = reader.IsDBNull(4) ? null : reader.GetString(4);
            dateOfBirth = reader.IsDBNull(5) ? null : DateOnly.FromDateTime(reader.GetDateTime(5));
        }

        var bio = await LoadPlayerBioAsync(connection, null, targetUserId, cancellationToken);
        var sports = await QuerySportProfilesAsync(connection, targetUserId, cancellationToken);
        var isMe = viewerUserId.HasValue && viewerUserId.Value == targetUserId;
        var friendshipStatus = isMe ? "Self" : "None";
        long? friendshipId = null;
        if (viewerUserId.HasValue && !isMe)
        {
            await using var friendCmd = new SqlCommand("""
                DECLARE @mePlayerId BIGINT = (SELECT id FROM dbo.players WHERE user_id = @viewerUserId);
                SELECT TOP 1 id, requester_id, addressee_id, status
                FROM dbo.friendships
                WHERE @mePlayerId IS NOT NULL
                  AND ((requester_id = @mePlayerId AND addressee_id = @targetPlayerId)
                    OR (requester_id = @targetPlayerId AND addressee_id = @mePlayerId))
                  AND status IN ('Pending', 'Accepted')
                ORDER BY id DESC;
                """, connection);
            friendCmd.Parameters.Add("@viewerUserId", SqlDbType.BigInt).Value = viewerUserId.Value;
            friendCmd.Parameters.Add("@targetPlayerId", SqlDbType.BigInt).Value = playerId;
            await using var fReader = await friendCmd.ExecuteReaderAsync(cancellationToken);
            if (await fReader.ReadAsync(cancellationToken))
            {
                friendshipId = fReader.GetInt64(0);
                var requesterId = fReader.GetInt64(1);
                var rawStatus = fReader.GetString(3);
                if (string.Equals(rawStatus, "Accepted", StringComparison.OrdinalIgnoreCase))
                {
                    friendshipStatus = "Accepted";
                }
                else if (string.Equals(rawStatus, "Pending", StringComparison.OrdinalIgnoreCase))
                {
                    friendshipStatus = requesterId == playerId ? "PendingReceived" : "PendingSent";
                }
            }
        }

        const string historySql = """
            SELECT TOP 10
                h.id,
                s.code AS sport_code,
                s.name AS sport_name,
                h.old_score,
                h.new_score,
                h.old_band,
                h.new_band,
                CASE WHEN LOWER(REPLACE(a.assessment_type, '_', '')) = 'oncourt'
                    THEN 'OnCourt' ELSE ISNULL(a.assessment_type, 'SelfSurvey') END AS assessment_type,
                sess.session_date,
                c.name AS court_name
            FROM dbo.player_rating_history h
            JOIN dbo.player_sport_profiles psp ON psp.id = h.player_sport_profile_id
            JOIN dbo.sports s ON s.id = psp.sport_id
            LEFT JOIN dbo.assessments a ON a.id = h.assessment_id
            LEFT JOIN dbo.sessions sess ON sess.id = a.session_id
            LEFT JOIN dbo.courts c ON c.id = sess.court_id
            WHERE psp.player_id = @playerId
            ORDER BY h.id DESC;
            """;
        var history = new List<PublicRatingHistoryItem>();
        await using (var histCmd = new SqlCommand(historySql, connection))
        {
            histCmd.Parameters.Add("@playerId", SqlDbType.BigInt).Value = playerId;
            await using var hReader = await histCmd.ExecuteReaderAsync(cancellationToken);
            while (await hReader.ReadAsync(cancellationToken))
            {
                history.Add(new PublicRatingHistoryItem(Id: hReader.GetInt64(0), SportCode: hReader.GetString(1), SportName: hReader.GetString(2), OldScore: hReader.IsDBNull(3) ? null : hReader.GetInt16(3), NewScore: hReader.IsDBNull(4) ? null : hReader.GetInt16(4), OldBand: hReader.IsDBNull(5) ? null : hReader.GetString(5), NewBand: hReader.IsDBNull(6) ? null : hReader.GetString(6), AssessmentType: hReader.IsDBNull(7) ? null : hReader.GetString(7), SessionDate: hReader.IsDBNull(8) ? null : DateOnly.FromDateTime(hReader.GetDateTime(8)), CourtName: hReader.IsDBNull(9) ? null : hReader.GetString(9)));
            }
        }

        return new PublicPlayerProfileDetails(PlayerId: playerId, UserId: targetUserId, FullName: fullName, AvatarUrl: avatarUrl, Gender: gender, DateOfBirth: dateOfBirth, Bio: bio, IsMe: isMe, FriendshipStatus: friendshipStatus, FriendshipId: friendshipId, SportProfiles: sports, RecentRatingHistory: history);
    }

    public async Task<IReadOnlyList<SportProfileItem>?> GetSportProfilesAsync(long userId, CancellationToken cancellationToken)
    {
        await using var connection = new SqlConnection(connectionString);
        await be_da.Infrastructure.Persistence.SportCenterConnection.OpenAsync(connection, cancellationToken, dataScope);
        await using var checkCmd = new SqlCommand("SELECT COUNT(*) FROM dbo.users WHERE id=@userId AND deleted_at IS NULL;", connection);
        checkCmd.Parameters.Add("@userId", SqlDbType.BigInt).Value = userId;
        if ((int)(await checkCmd.ExecuteScalarAsync(cancellationToken))! == 0)
            return null;
        return await QuerySportProfilesAsync(connection, userId, cancellationToken);
    }

    public async Task<UserProfileDetails?> UpdateProfileAsync(long userId, UpdateProfileRequest request, CancellationToken cancellationToken)
    {
        await using var connection = new SqlConnection(connectionString);
        await be_da.Infrastructure.Persistence.SportCenterConnection.OpenAsync(connection, cancellationToken, dataScope);
        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken);
        try
        {
            await using var selectCmd = new SqlCommand("""
                SELECT u.full_name, p.date_of_birth, p.gender, p.avatar_url
                FROM dbo.users u
                LEFT JOIN dbo.players p ON p.user_id = u.id
                WHERE u.id = @userId AND u.deleted_at IS NULL;
                """, connection, transaction);
            selectCmd.Parameters.Add("@userId", SqlDbType.BigInt).Value = userId;
            string? oldName = null;
            DateOnly? oldDob = null;
            string? oldGender = null;
            string? oldAvatar = null;
            await using (var reader = await selectCmd.ExecuteReaderAsync(cancellationToken))
            {
                if (await reader.ReadAsync(cancellationToken))
                {
                    oldName = reader.GetString(0);
                    if (!reader.IsDBNull(1))
                        oldDob = DateOnly.FromDateTime(reader.GetDateTime(1));
                    if (!reader.IsDBNull(2))
                        oldGender = reader.GetString(2);
                    if (!reader.IsDBNull(3))
                        oldAvatar = reader.GetString(3);
                }
            }

            if (oldName is null)
            {
                await transaction.RollbackAsync(cancellationToken);
                return null;
            }

            var oldBio = await LoadPlayerBioAsync(connection, transaction, userId, cancellationToken);
            var trimmedName = request.FullName!.Trim();
            var trimmedGender = string.IsNullOrWhiteSpace(request.Gender) ? null : request.Gender.Trim();
            var trimmedAvatar = string.IsNullOrWhiteSpace(request.AvatarUrl) ? null : request.AvatarUrl.Trim();
            var trimmedBio = request.Bio is null ? oldBio : (string.IsNullOrWhiteSpace(request.Bio) ? null : request.Bio.Trim());
            await using var updateUsersCmd = new SqlCommand("""
                UPDATE dbo.users
                SET full_name = @fullName
                WHERE id = @userId AND deleted_at IS NULL;
                """, connection, transaction);
            updateUsersCmd.Parameters.Add("@fullName", SqlDbType.NVarChar, 255).Value = trimmedName;
            updateUsersCmd.Parameters.Add("@userId", SqlDbType.BigInt).Value = userId;
            await updateUsersCmd.ExecuteNonQueryAsync(cancellationToken);
            await using var upsertPlayerCmd = new SqlCommand("""
                IF EXISTS (SELECT 1 FROM dbo.players WHERE user_id = @userId)
                BEGIN
                    UPDATE dbo.players
                    SET date_of_birth = @dob,
                        gender = @gender,
                        avatar_url = @avatarUrl
                    WHERE user_id = @userId;
                END
                ELSE
                BEGIN
                    INSERT INTO dbo.players (user_id, date_of_birth, gender, avatar_url)
                    VALUES (@userId, @dob, @gender, @avatarUrl);
                END
                """, connection, transaction);
            upsertPlayerCmd.Parameters.Add("@userId", SqlDbType.BigInt).Value = userId;
            upsertPlayerCmd.Parameters.Add("@dob", SqlDbType.Date).Value = request.DateOfBirth.HasValue ? request.DateOfBirth.Value.ToDateTime(TimeOnly.MinValue) : DBNull.Value;
            upsertPlayerCmd.Parameters.Add("@gender", SqlDbType.VarChar, 100).Value = (object? )trimmedGender ?? DBNull.Value;
            upsertPlayerCmd.Parameters.Add("@avatarUrl", SqlDbType.VarChar, 2048).Value = (object? )trimmedAvatar ?? DBNull.Value;
            await upsertPlayerCmd.ExecuteNonQueryAsync(cancellationToken);
            var oldState = new
            {
                FullName = oldName,
                DateOfBirth = oldDob,
                Gender = oldGender,
                AvatarUrl = oldAvatar,
                Bio = oldBio
            };
            var newState = new
            {
                FullName = trimmedName,
                DateOfBirth = request.DateOfBirth,
                Gender = trimmedGender,
                AvatarUrl = trimmedAvatar,
                Bio = trimmedBio
            };
            await using var auditCmd = new SqlCommand("""
                INSERT INTO dbo.account_audit_logs (user_id, actor_id, action, old_value, new_value)
                VALUES (@userId, @userId, 'UpdateProfile', @oldValue, @newValue);
                """, connection, transaction);
            auditCmd.Parameters.Add("@userId", SqlDbType.BigInt).Value = userId;
            auditCmd.Parameters.Add("@oldValue", SqlDbType.NVarChar, -1).Value = JsonSerializer.Serialize(oldState);
            auditCmd.Parameters.Add("@newValue", SqlDbType.NVarChar, -1).Value = JsonSerializer.Serialize(newState);
            await auditCmd.ExecuteNonQueryAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }

        return await GetProfileAsync(userId, cancellationToken);
    }

    public async Task<SportProfileResult> UpdateSportProfileAsync(long userId, string sportCode, UpdateSportProfileRequest request, CancellationToken cancellationToken)
    {
        await using var connection = new SqlConnection(connectionString);
        await be_da.Infrastructure.Persistence.SportCenterConnection.OpenAsync(connection, cancellationToken, dataScope);
        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken);
        try
        {
            await using var sportCmd = new SqlCommand("SELECT id, code, name FROM dbo.sports WHERE LOWER(code)=LOWER(@code);", connection, transaction);
            sportCmd.Parameters.Add("@code", SqlDbType.VarChar, 100).Value = sportCode;
            long sportId;
            string canonicalCode;
            string sportName;
            await using (var reader = await sportCmd.ExecuteReaderAsync(cancellationToken))
            {
                if (!await reader.ReadAsync(cancellationToken))
                {
                    await reader.CloseAsync();
                    await transaction.RollbackAsync(cancellationToken);
                    return new SportProfileResult(false, "sport_not_found", $"Sport '{sportCode}' was not found.", null);
                }

                sportId = reader.GetInt64(0);
                canonicalCode = reader.GetString(1);
                sportName = reader.GetString(2);
            }

            await using var playerCmd = new SqlCommand("""
                IF NOT EXISTS (SELECT 1 FROM dbo.players WHERE user_id = @userId)
                    INSERT INTO dbo.players(user_id) VALUES(@userId);
                SELECT id FROM dbo.players WHERE user_id = @userId;
                """, connection, transaction);
            playerCmd.Parameters.Add("@userId", SqlDbType.BigInt).Value = userId;
            var playerId = (long)(await playerCmd.ExecuteScalarAsync(cancellationToken))!;
            await using var existingRatingCmd = new SqlCommand("""
                SELECT current_score, current_band, rating_status
                FROM dbo.player_sport_profiles
                WHERE player_id = @playerId AND sport_id = @sportId;
                """, connection, transaction);
            existingRatingCmd.Parameters.Add("@playerId", SqlDbType.BigInt).Value = playerId;
            existingRatingCmd.Parameters.Add("@sportId", SqlDbType.BigInt).Value = sportId;
            short? oldScore = null;
            string? oldBand = null;
            string? oldStatus = null;
            bool exists = false;
            await using (var reader = await existingRatingCmd.ExecuteReaderAsync(cancellationToken))
            {
                if (await reader.ReadAsync(cancellationToken))
                {
                    exists = true;
                    if (!reader.IsDBNull(0))
                        oldScore = reader.GetInt16(0);
                    if (!reader.IsDBNull(1))
                        oldBand = reader.GetString(1);
                    if (!reader.IsDBNull(2))
                        oldStatus = reader.GetString(2);
                }
            }

            if (string.Equals(oldStatus, "Confirmed", StringComparison.OrdinalIgnoreCase) || string.Equals(oldStatus, "Rated", StringComparison.OrdinalIgnoreCase))
            {
                await transaction.RollbackAsync(cancellationToken);
                return new SportProfileResult(false, "confirmed_rating_cannot_be_overwritten", "A confirmed on-court skill rating already exists and cannot be replaced by self-survey.", null);
            }

            var score = request.Score!.Value;
            var band = SqlSessionStore.NormalizeBand(request.Band, score) ?? ComputeBandFromScore(score);
            const string newStatus = "Provisional";
            if (exists)
            {
                await using var updateCmd = new SqlCommand("""
                    UPDATE dbo.player_sport_profiles
                    SET current_score = @score, current_band = @band, rating_status = @status
                    WHERE player_id = @playerId AND sport_id = @sportId;
                    """, connection, transaction);
                updateCmd.Parameters.Add("@playerId", SqlDbType.BigInt).Value = playerId;
                updateCmd.Parameters.Add("@sportId", SqlDbType.BigInt).Value = sportId;
                updateCmd.Parameters.Add("@score", SqlDbType.SmallInt).Value = score;
                updateCmd.Parameters.Add("@band", SqlDbType.VarChar, 100).Value = band;
                updateCmd.Parameters.Add("@status", SqlDbType.VarChar, 100).Value = newStatus;
                await updateCmd.ExecuteNonQueryAsync(cancellationToken);
            }
            else
            {
                await using var insertCmd = new SqlCommand("""
                    INSERT INTO dbo.player_sport_profiles (player_id, sport_id, current_score, current_band, rating_status)
                    VALUES (@playerId, @sportId, @score, @band, @status);
                    """, connection, transaction);
                insertCmd.Parameters.Add("@playerId", SqlDbType.BigInt).Value = playerId;
                insertCmd.Parameters.Add("@sportId", SqlDbType.BigInt).Value = sportId;
                insertCmd.Parameters.Add("@score", SqlDbType.SmallInt).Value = score;
                insertCmd.Parameters.Add("@band", SqlDbType.VarChar, 100).Value = band;
                insertCmd.Parameters.Add("@status", SqlDbType.VarChar, 100).Value = newStatus;
                await insertCmd.ExecuteNonQueryAsync(cancellationToken);
            }

            var oldAudit = new
            {
                Sport = canonicalCode,
                Score = oldScore,
                Band = oldBand,
                Status = oldStatus
            };
            var newAudit = new
            {
                Sport = canonicalCode,
                Score = score,
                Band = band,
                Status = newStatus,
                Notes = request.ExperienceNotes
            };
            await using var auditCmd = new SqlCommand("""
                INSERT INTO dbo.account_audit_logs (user_id, actor_id, action, old_value, new_value)
                VALUES (@userId, @userId, 'CompleteSportSurvey', @oldValue, @newValue);
                """, connection, transaction);
            auditCmd.Parameters.Add("@userId", SqlDbType.BigInt).Value = userId;
            auditCmd.Parameters.Add("@oldValue", SqlDbType.NVarChar, -1).Value = JsonSerializer.Serialize(oldAudit);
            auditCmd.Parameters.Add("@newValue", SqlDbType.NVarChar, -1).Value = JsonSerializer.Serialize(newAudit);
            await auditCmd.ExecuteNonQueryAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            var resultItem = new SportProfileItem(canonicalCode, sportName, score, band, newStatus, sportId);
            return new SportProfileResult(true, null, null, resultItem);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    internal static async Task<string?> LoadPlayerBioAsync(SqlConnection connection, SqlTransaction? transaction, long userId, CancellationToken cancellationToken)
    {
        await using var cmd = new SqlCommand("""
            SELECT TOP 1 new_value
            FROM dbo.account_audit_logs
            WHERE user_id = @userId AND action = 'UpdateProfile' AND new_value IS NOT NULL
            ORDER BY id DESC;
            """, connection, transaction);
        cmd.Parameters.Add("@userId", SqlDbType.BigInt).Value = userId;
        var raw = await cmd.ExecuteScalarAsync(cancellationToken) as string;
        if (string.IsNullOrWhiteSpace(raw))
            return null;
        try
        {
            using var doc = JsonDocument.Parse(raw);
            if (doc.RootElement.TryGetProperty("Bio", out var bioProp) && bioProp.ValueKind == JsonValueKind.String)
            {
                var val = bioProp.GetString();
                return string.IsNullOrWhiteSpace(val) ? null : val;
            }
        }
        catch
        {
        // Ignore malformed JSON in legacy audit log rows
        }

        return null;
    }

    private static string ComputeBandFromScore(short score) => score switch
    {
        < 20 => "B1",
        < 40 => "B2",
        < 60 => "B3",
        < 80 => "B4",
        _ => "B5"
    };
    private static async Task<List<SportProfileItem>> QuerySportProfilesAsync(SqlConnection connection, long userId, CancellationToken cancellationToken)
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
            LEFT JOIN dbo.players p ON p.user_id = @userId
            LEFT JOIN dbo.player_sport_profiles sp ON sp.sport_id = s.id AND sp.player_id = p.id
            ORDER BY s.code;
            """, connection);
        sportsCmd.Parameters.Add("@userId", SqlDbType.BigInt).Value = userId;
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

    private sealed record UserProfileHeader(long Id, string Email, string FullName, string Role, string Status, long? PlayerId, DateOnly? DateOfBirth, string? Gender, string? AvatarUrl);
}
