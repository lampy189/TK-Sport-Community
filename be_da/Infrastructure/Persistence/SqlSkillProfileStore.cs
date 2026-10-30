using System.Data;
using Microsoft.Data.SqlClient;
using be_da.Application.Abstractions;
using be_da.Application.Skills;

namespace be_da.Infrastructure.Persistence;

public sealed class SqlSkillProfileStore(string connectionString) : ISkillProfileStore
{
    public async Task<IReadOnlyList<SportRubricDto>> GetActiveRubricsAsync(long? sportId, CancellationToken cancellationToken)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        return await LoadActiveRubricsInternalAsync(connection, null, sportId, cancellationToken);
    }

    public async Task<PlayerSkillOverviewDto?> GetPlayerSkillOverviewAsync(long userId, CancellationToken cancellationToken)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        // 1. Resolve User and Ensure Player Record Exists
        const string userPlayerSql = """
            SELECT u.id, u.full_name, p.id AS player_id, p.avatar_url
            FROM dbo.users u
            LEFT JOIN dbo.players p ON p.user_id = u.id
            WHERE u.id = @userId AND u.deleted_at IS NULL;
            """;

        string fullName;
        long? existingPlayerId;
        string? avatarUrl;

        await using (var cmd = new SqlCommand(userPlayerSql, connection))
        {
            cmd.Parameters.Add("@userId", SqlDbType.BigInt).Value = userId;
            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken))
            {
                return null;
            }
            fullName = reader.GetString(1);
            existingPlayerId = reader.IsDBNull(2) ? null : reader.GetInt64(2);
            avatarUrl = reader.IsDBNull(3) ? null : reader.GetString(3);
        }

        long playerId;
        if (existingPlayerId.HasValue)
        {
            playerId = existingPlayerId.Value;
        }
        else
        {
            await using var createPlayerCmd = new SqlCommand("""
                IF NOT EXISTS (SELECT 1 FROM dbo.players WHERE user_id = @userId)
                    INSERT INTO dbo.players (user_id) VALUES (@userId);
                SELECT id FROM dbo.players WHERE user_id = @userId;
                """, connection);
            createPlayerCmd.Parameters.Add("@userId", SqlDbType.BigInt).Value = userId;
            playerId = Convert.ToInt64(await createPlayerCmd.ExecuteScalarAsync(cancellationToken));
        }

        // 2. Load Sports
        var sports = new List<(long Id, string Code, string Name)>();
        await using (var sportCmd = new SqlCommand("SELECT id, code, name FROM dbo.sports ORDER BY id ASC;", connection))
        await using (var reader = await sportCmd.ExecuteReaderAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                sports.Add((reader.GetInt64(0), reader.GetString(1), reader.GetString(2)));
            }
        }

        // 3. Load Active Rubrics
        var rubrics = await LoadActiveRubricsInternalAsync(connection, null, null, cancellationToken);
        var rubricBySportId = rubrics
            .GroupBy(r => r.SportId)
            .ToDictionary(g => g.Key, g => g.First());

        // 4. Load Player Sport Profiles
        var rawProfiles = new Dictionary<long, (long ProfileId, short? Score, string? Band, string Status)>();
        const string profilesSql = """
            SELECT id, sport_id, current_score, current_band, rating_status
            FROM dbo.player_sport_profiles
            WHERE player_id = @playerId;
            """;

        await using (var profCmd = new SqlCommand(profilesSql, connection))
        {
            profCmd.Parameters.Add("@playerId", SqlDbType.BigInt).Value = playerId;
            await using var reader = await profCmd.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                var pId = reader.GetInt64(0);
                var spId = reader.GetInt64(1);
                var score = reader.IsDBNull(2) ? null : (short?)reader.GetInt16(2);
                var band = reader.IsDBNull(3) ? null : reader.GetString(3);
                var status = reader.IsDBNull(4) ? "Unrated" : reader.GetString(4);
                rawProfiles[spId] = (pId, score, band, status);
            }
        }

        var profiles = new List<PlayerSportSkillProfileDto>(sports.Count);
        foreach (var sport in sports)
        {
            rubricBySportId.TryGetValue(sport.Id, out var activeRubric);
            if (!rawProfiles.TryGetValue(sport.Id, out var prof))
            {
                profiles.Add(new PlayerSportSkillProfileDto(
                    ProfileId: null,
                    SportId: sport.Id,
                    SportCode: sport.Code,
                    SportName: sport.Name,
                    CurrentScore: null,
                    CurrentBand: null,
                    RatingStatus: "Unrated",
                    ActiveRubric: activeRubric,
                    LatestAssessment: null,
                    RatingHistory: Array.Empty<RatingHistoryItemDto>()
                ));
                continue;
            }

            var latestAssessment = await LoadLatestAssessmentAsync(connection, null, prof.ProfileId, cancellationToken);
            var history = await LoadRatingHistoryAsync(connection, null, prof.ProfileId, cancellationToken);

            var hasOnCourtRefereeRating =
                string.Equals(latestAssessment?.AssessmentType, "OnCourt", StringComparison.OrdinalIgnoreCase) ||
                history.Any(h => string.Equals(h.AssessmentType, "OnCourt", StringComparison.OrdinalIgnoreCase));

            var normalizedStatus = (!prof.Score.HasValue || string.IsNullOrWhiteSpace(prof.Band))
                ? "Unrated"
                : hasOnCourtRefereeRating
                    ? "Rated"
                    : prof.Status;

            profiles.Add(new PlayerSportSkillProfileDto(
                ProfileId: prof.ProfileId,
                SportId: sport.Id,
                SportCode: sport.Code,
                SportName: sport.Name,
                CurrentScore: prof.Score,
                CurrentBand: prof.Band,
                RatingStatus: normalizedStatus,
                ActiveRubric: activeRubric,
                LatestAssessment: latestAssessment,
                RatingHistory: history
            ));
        }

        return new PlayerSkillOverviewDto(
            PlayerId: playerId,
            UserId: userId,
            FullName: fullName,
            AvatarUrl: avatarUrl,
            Profiles: profiles
        );
    }

    public async Task<SubmitSkillSurveyResult> SubmitSelfAssessmentAsync(
        long userId,
        SubmitSkillSurveyRequest request,
        CancellationToken cancellationToken)
    {
        if (request.Answers == null || request.Answers.Count == 0)
        {
            return new SubmitSkillSurveyResult(false, "INVALID_ANSWERS", "Vui lòng trả lời đầy đủ 5 tiêu chí kỹ năng của môn học.");
        }

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        try
        {
            // 1. Validate Sport & Active Rubric
            var rubrics = await LoadActiveRubricsInternalAsync(connection, transaction, request.SportId, cancellationToken);
            var activeRubric = rubrics.FirstOrDefault();
            if (activeRubric == null || activeRubric.Dimensions.Count == 0)
            {
                await transaction.RollbackAsync(cancellationToken);
                return new SubmitSkillSurveyResult(false, "RUBRIC_NOT_FOUND", "Chưa tìm thấy bộ tiêu chí đánh giá đang kích hoạt cho môn thể thao này.");
            }

            // Validate every dimension in active rubric has a valid 0..4 score
            var answerByDim = request.Answers
                .GroupBy(a => a.DimensionId)
                .ToDictionary(g => g.Key, g => g.Last().Score);

            short rawSum = 0;
            foreach (var dim in activeRubric.Dimensions)
            {
                if (!answerByDim.TryGetValue(dim.Id, out var score))
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return new SubmitSkillSurveyResult(false, "INCOMPLETE_SURVEY", $"Vui lòng chọn mức điểm cho tiêu chí '{dim.Name}'.");
                }
                if (score is < 0 or > 4)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return new SubmitSkillSurveyResult(false, "SCORE_OUT_OF_RANGE", $"Điểm tiêu chí '{dim.Name}' phải nằm trong thang 0 – 4.");
                }
                rawSum += score;
            }

            // Formula per RP1 Appendix A1 & A2: Total Score = 5 * (D1 + D2 + D3 + D4 + D5), range 0..100
            short totalScore = (short)Math.Clamp(rawSum * 5, 0, 100);
            string bandResult = MapScoreToBand(totalScore);

            // 2. Ensure Player Record Exists
            long playerId;
            await using (var playerCmd = new SqlCommand("""
                IF NOT EXISTS (SELECT 1 FROM dbo.players WHERE user_id = @userId)
                    INSERT INTO dbo.players (user_id) VALUES (@userId);
                SELECT id FROM dbo.players WITH (UPDLOCK, HOLDLOCK) WHERE user_id = @userId;
                """, connection, transaction))
            {
                playerCmd.Parameters.Add("@userId", SqlDbType.BigInt).Value = userId;
                playerId = Convert.ToInt64(await playerCmd.ExecuteScalarAsync(cancellationToken));
            }

            // 3. Inspect or Create PlayerSportProfile
            long profileId;
            short? oldScore = null;
            string? oldBand = null;
            string? currentStatus = null;

            await using (var inspectCmd = new SqlCommand("""
                SELECT id, current_score, current_band, rating_status
                FROM dbo.player_sport_profiles WITH (UPDLOCK, HOLDLOCK)
                WHERE player_id = @playerId AND sport_id = @sportId;
                """, connection, transaction))
            {
                inspectCmd.Parameters.Add("@playerId", SqlDbType.BigInt).Value = playerId;
                inspectCmd.Parameters.Add("@sportId", SqlDbType.BigInt).Value = request.SportId;
                await using var reader = await inspectCmd.ExecuteReaderAsync(cancellationToken);
                if (await reader.ReadAsync(cancellationToken))
                {
                    profileId = reader.GetInt64(0);
                    oldScore = reader.IsDBNull(1) ? null : reader.GetInt16(1);
                    oldBand = reader.IsDBNull(2) ? null : reader.GetString(2);
                    currentStatus = reader.IsDBNull(3) ? null : reader.GetString(3);
                }
                else
                {
                    profileId = 0;
                }
            }

            // FR-03.4: Lock self-assessment once the player has been rated by Referee OR already completed their initial survey
            bool hasOnCourtRecord = false;
            if (profileId != 0)
            {
                await using var checkOnCourtCmd = new SqlCommand("""
                    SELECT CASE WHEN EXISTS (
                        SELECT 1 FROM dbo.assessments
                        WHERE player_sport_profile_id = @profileId AND assessment_type = 'OnCourt'
                    ) THEN 1 ELSE 0 END;
                    """, connection, transaction);
                checkOnCourtCmd.Parameters.Add("@profileId", SqlDbType.BigInt).Value = profileId;
                hasOnCourtRecord = Convert.ToInt32(await checkOnCourtCmd.ExecuteScalarAsync(cancellationToken)) == 1;
            }

            if (string.Equals(currentStatus, "Rated", StringComparison.OrdinalIgnoreCase) || hasOnCourtRecord)
            {
                await transaction.RollbackAsync(cancellationToken);
                return new SubmitSkillSurveyResult(
                    false,
                    "ALREADY_RATED",
                    "Hồ sơ kỹ năng môn này đã được Trọng tài chấm điểm chính thức trên sân, tự đánh giá đã bị khóa để tránh tự thêm bớt điểm.");
            }

            if (profileId != 0 && oldScore.HasValue && string.Equals(currentStatus, "Provisional", StringComparison.OrdinalIgnoreCase))
            {
                await transaction.RollbackAsync(cancellationToken);
                return new SubmitSkillSurveyResult(
                    false,
                    "ALREADY_SURVEYED",
                    "Bạn đã hoàn thành khảo sát tự đánh giá ban đầu để nhận điểm tạm thời. Tự đánh giá hiện đã khóa để chờ Trọng tài chấm điểm sau buổi chơi.");
            }

            if (profileId == 0)
            {
                await using var insertProfCmd = new SqlCommand("""
                    INSERT INTO dbo.player_sport_profiles (player_id, sport_id, current_score, current_band, rating_status)
                    VALUES (@playerId, @sportId, @score, @band, 'Provisional');
                    SELECT CONVERT(bigint, SCOPE_IDENTITY());
                    """, connection, transaction);
                insertProfCmd.Parameters.Add("@playerId", SqlDbType.BigInt).Value = playerId;
                insertProfCmd.Parameters.Add("@sportId", SqlDbType.BigInt).Value = request.SportId;
                insertProfCmd.Parameters.Add("@score", SqlDbType.SmallInt).Value = totalScore;
                insertProfCmd.Parameters.Add("@band", SqlDbType.VarChar, 100).Value = bandResult;
                profileId = (long)(await insertProfCmd.ExecuteScalarAsync(cancellationToken))!;
            }
            else
            {
                await using var updateProfCmd = new SqlCommand("""
                    UPDATE dbo.player_sport_profiles
                    SET current_score = @score, current_band = @band, rating_status = 'Provisional'
                    WHERE id = @profileId;
                    """, connection, transaction);
                updateProfCmd.Parameters.Add("@score", SqlDbType.SmallInt).Value = totalScore;
                updateProfCmd.Parameters.Add("@band", SqlDbType.VarChar, 100).Value = bandResult;
                updateProfCmd.Parameters.Add("@profileId", SqlDbType.BigInt).Value = profileId;
                await updateProfCmd.ExecuteNonQueryAsync(cancellationToken);
            }

            // 4. Insert Assessment (SelfSurvey) & 5 Dimension Scores
            long assessmentId;
            await using (var insertAssCmd = new SqlCommand("""
                INSERT INTO dbo.assessments (player_sport_profile_id, session_id, rubric_id, assessment_type, assessor_id, status, total_score, band_result)
                VALUES (@profileId, NULL, @rubricId, 'SelfSurvey', @userId, 'Published', @totalScore, @bandResult);
                SELECT CONVERT(bigint, SCOPE_IDENTITY());
                """, connection, transaction))
            {
                insertAssCmd.Parameters.Add("@profileId", SqlDbType.BigInt).Value = profileId;
                insertAssCmd.Parameters.Add("@rubricId", SqlDbType.BigInt).Value = activeRubric.Id;
                insertAssCmd.Parameters.Add("@userId", SqlDbType.BigInt).Value = userId;
                insertAssCmd.Parameters.Add("@totalScore", SqlDbType.SmallInt).Value = totalScore;
                insertAssCmd.Parameters.Add("@bandResult", SqlDbType.VarChar, 100).Value = bandResult;
                assessmentId = (long)(await insertAssCmd.ExecuteScalarAsync(cancellationToken))!;
            }

            foreach (var dim in activeRubric.Dimensions)
            {
                var dimScore = answerByDim[dim.Id];
                await using var scoreCmd = new SqlCommand("""
                    INSERT INTO dbo.assessment_scores (assessment_id, dimension_id, score)
                    VALUES (@assessmentId, @dimensionId, @score);
                    """, connection, transaction);
                scoreCmd.Parameters.Add("@assessmentId", SqlDbType.BigInt).Value = assessmentId;
                scoreCmd.Parameters.Add("@dimensionId", SqlDbType.BigInt).Value = dim.Id;
                scoreCmd.Parameters.Add("@score", SqlDbType.SmallInt).Value = dimScore;
                await scoreCmd.ExecuteNonQueryAsync(cancellationToken);
            }

            // 5. Record in player_rating_history
            await using (var histCmd = new SqlCommand("""
                INSERT INTO dbo.player_rating_history (player_sport_profile_id, old_score, new_score, old_band, new_band, changed_by, assessment_id)
                VALUES (@profileId, @oldScore, @newScore, @oldBand, @newBand, @changedBy, @assessmentId);
                """, connection, transaction))
            {
                histCmd.Parameters.Add("@profileId", SqlDbType.BigInt).Value = profileId;
                histCmd.Parameters.Add("@oldScore", SqlDbType.SmallInt).Value = (object?)oldScore ?? DBNull.Value;
                histCmd.Parameters.Add("@newScore", SqlDbType.SmallInt).Value = totalScore;
                histCmd.Parameters.Add("@oldBand", SqlDbType.VarChar, 100).Value = (object?)oldBand ?? DBNull.Value;
                histCmd.Parameters.Add("@newBand", SqlDbType.VarChar, 100).Value = bandResult;
                histCmd.Parameters.Add("@changedBy", SqlDbType.BigInt).Value = userId;
                histCmd.Parameters.Add("@assessmentId", SqlDbType.BigInt).Value = assessmentId;
                await histCmd.ExecuteNonQueryAsync(cancellationToken);
            }

            var latestAssessment = await LoadLatestAssessmentAsync(connection, transaction, profileId, cancellationToken);
            var history = await LoadRatingHistoryAsync(connection, transaction, profileId, cancellationToken);

            await transaction.CommitAsync(cancellationToken);

            var updatedProfile = new PlayerSportSkillProfileDto(
                ProfileId: profileId,
                SportId: activeRubric.SportId,
                SportCode: activeRubric.SportCode,
                SportName: activeRubric.SportName,
                CurrentScore: totalScore,
                CurrentBand: bandResult,
                RatingStatus: "Provisional",
                ActiveRubric: activeRubric,
                LatestAssessment: latestAssessment,
                RatingHistory: history
            );

            return new SubmitSkillSurveyResult(
                Success: true,
                ErrorCode: null,
                Message: $"Đã lưu khảo sát tự đánh giá ban đầu: {totalScore}/100 điểm — Band {bandResult} (Tạm thời - chờ Trọng tài xác nhận trên sân).",
                Profile: updatedProfile
            );
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public static string MapScoreToBand(short score) => score switch
    {
        < 20 => "B1",
        < 40 => "B2",
        < 60 => "B3",
        < 80 => "B4",
        _ => "B5"
    };

    private static async Task<IReadOnlyList<SportRubricDto>> LoadActiveRubricsInternalAsync(
        SqlConnection connection,
        SqlTransaction? transaction,
        long? sportId,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT r.id, r.sport_id, s.code AS sport_code, s.name AS sport_name, r.version, r.name,
                   rd.id AS dim_id, rd.code AS dim_code, rd.name AS dim_name, ISNULL(rd.weight_percent, 20) AS weight_percent
            FROM dbo.rubrics r
            JOIN dbo.sports s ON s.id = r.sport_id
            LEFT JOIN dbo.rubric_dimensions rd ON rd.rubric_id = r.id
            WHERE r.is_active = 1
              AND (@sportId IS NULL OR r.sport_id = @sportId)
            ORDER BY r.sport_id ASC, r.id DESC, rd.id ASC;
            """;

        await using var cmd = new SqlCommand(sql, connection, transaction);
        cmd.Parameters.Add("@sportId", SqlDbType.BigInt).Value = (object?)sportId ?? DBNull.Value;

        var rubricMap = new Dictionary<long, (long Id, long SportId, string SportCode, string SportName, string Version, string Name, List<RubricDimensionDto> Dims)>();

        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var rId = reader.GetInt64(0);
            if (!rubricMap.TryGetValue(rId, out var entry))
            {
                entry = (
                    rId,
                    reader.GetInt64(1),
                    reader.GetString(2),
                    reader.GetString(3),
                    reader.GetString(4),
                    reader.GetString(5),
                    new List<RubricDimensionDto>()
                );
                rubricMap[rId] = entry;
            }

            if (!reader.IsDBNull(6))
            {
                entry.Dims.Add(new RubricDimensionDto(
                    Id: reader.GetInt64(6),
                    Code: reader.GetString(7),
                    Name: reader.GetString(8),
                    WeightPercent: reader.GetInt16(9)
                ));
            }
        }

        return rubricMap.Values
            .Select(x => new SportRubricDto(x.Id, x.SportId, x.SportCode, x.SportName, x.Version, x.Name, x.Dims))
            .ToList();
    }

    private static async Task<AssessmentResultDto?> LoadLatestAssessmentAsync(
        SqlConnection connection,
        SqlTransaction? transaction,
        long profileId,
        CancellationToken cancellationToken)
    {
        const string headSql = """
            SELECT TOP 1
                a.id, a.rubric_id, r.name AS rubric_name, r.version AS rubric_version,
                ISNULL(a.assessment_type, 'OnCourt') AS assessment_type,
                ISNULL(a.status, 'Published') AS status,
                ISNULL(a.total_score, 0) AS total_score,
                ISNULL(a.band_result, 'B1') AS band_result,
                a.assessor_id, u.full_name AS assessor_name, ro.code AS assessor_role,
                a.session_id, s.session_date, c.name AS court_name, ts.label AS time_label
            FROM dbo.assessments a
            JOIN dbo.rubrics r ON r.id = a.rubric_id
            JOIN dbo.users u ON u.id = a.assessor_id
            JOIN dbo.roles ro ON ro.id = u.role_id
            LEFT JOIN dbo.sessions s ON s.id = a.session_id
            LEFT JOIN dbo.courts c ON c.id = s.court_id
            LEFT JOIN dbo.time_slots ts ON ts.id = s.time_slot_id
            WHERE a.player_sport_profile_id = @profileId
            ORDER BY a.id DESC;
            """;

        long assessmentId;
        long rubricId;
        string rubricName;
        string rubricVersion;
        string assessmentType;
        string status;
        short totalScore;
        string bandResult;
        long assessorId;
        string assessorName;
        string assessorRole;
        long? sessionId;
        DateOnly? sessionDate;
        string? courtName;
        string? timeLabel;

        await using (var cmd = new SqlCommand(headSql, connection, transaction))
        {
            cmd.Parameters.Add("@profileId", SqlDbType.BigInt).Value = profileId;
            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken))
            {
                return null;
            }

            assessmentId = reader.GetInt64(0);
            rubricId = reader.GetInt64(1);
            rubricName = reader.GetString(2);
            rubricVersion = reader.GetString(3);
            assessmentType = reader.GetString(4);
            status = reader.GetString(5);
            totalScore = reader.GetInt16(6);
            bandResult = reader.GetString(7);
            assessorId = reader.GetInt64(8);
            assessorName = reader.GetString(9);
            assessorRole = reader.GetString(10);
            sessionId = reader.IsDBNull(11) ? null : reader.GetInt64(11);
            sessionDate = reader.IsDBNull(12) ? null : DateOnly.FromDateTime(reader.GetDateTime(12));
            courtName = reader.IsDBNull(13) ? null : reader.GetString(13);
            timeLabel = reader.IsDBNull(14) ? null : reader.GetString(14);
        }

        const string scoresSql = """
            SELECT rd.id, rd.code, rd.name, ISNULL(rd.weight_percent, 20) AS weight_percent, ISNULL(sc.score, 0) AS score
            FROM dbo.rubric_dimensions rd
            LEFT JOIN dbo.assessment_scores sc ON sc.dimension_id = rd.id AND sc.assessment_id = @assessmentId
            WHERE rd.rubric_id = @rubricId
            ORDER BY rd.id ASC;
            """;

        var scores = new List<AssessmentDimensionScoreDto>();
        await using (var scoreCmd = new SqlCommand(scoresSql, connection, transaction))
        {
            scoreCmd.Parameters.Add("@assessmentId", SqlDbType.BigInt).Value = assessmentId;
            scoreCmd.Parameters.Add("@rubricId", SqlDbType.BigInt).Value = rubricId;
            await using var reader = await scoreCmd.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                var mark = reader.GetInt16(4);
                scores.Add(new AssessmentDimensionScoreDto(
                    DimensionId: reader.GetInt64(0),
                    DimensionCode: reader.GetString(1),
                    DimensionName: reader.GetString(2),
                    WeightPercent: reader.GetInt16(3),
                    Score: mark,
                    WeightedPoints: (short)(mark * 5)
                ));
            }
        }

        return new AssessmentResultDto(
            Id: assessmentId,
            RubricId: rubricId,
            RubricName: rubricName,
            RubricVersion: rubricVersion,
            AssessmentType: assessmentType,
            Status: status,
            TotalScore: totalScore,
            BandResult: bandResult,
            AssessorId: assessorId,
            AssessorName: assessorName,
            AssessorRole: assessorRole,
            SessionId: sessionId,
            SessionDate: sessionDate,
            CourtName: courtName,
            TimeLabel: timeLabel,
            Scores: scores
        );
    }

    private static async Task<IReadOnlyList<RatingHistoryItemDto>> LoadRatingHistoryAsync(
        SqlConnection connection,
        SqlTransaction? transaction,
        long profileId,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT
                h.id, h.old_score, h.new_score, h.old_band, h.new_band,
                h.changed_by, u.full_name AS changed_by_name, ro.code AS changed_by_role,
                h.assessment_id, a.assessment_type, a.session_id, s.session_date, c.name AS court_name
            FROM dbo.player_rating_history h
            JOIN dbo.users u ON u.id = h.changed_by
            JOIN dbo.roles ro ON ro.id = u.role_id
            LEFT JOIN dbo.assessments a ON a.id = h.assessment_id
            LEFT JOIN dbo.sessions s ON s.id = a.session_id
            LEFT JOIN dbo.courts c ON c.id = s.court_id
            WHERE h.player_sport_profile_id = @profileId
            ORDER BY h.id DESC;
            """;

        var items = new List<RatingHistoryItemDto>();
        await using var cmd = new SqlCommand(sql, connection, transaction);
        cmd.Parameters.Add("@profileId", SqlDbType.BigInt).Value = profileId;

        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            items.Add(new RatingHistoryItemDto(
                Id: reader.GetInt64(0),
                OldScore: reader.IsDBNull(1) ? null : reader.GetInt16(1),
                NewScore: reader.IsDBNull(2) ? null : reader.GetInt16(2),
                OldBand: reader.IsDBNull(3) ? null : reader.GetString(3),
                NewBand: reader.IsDBNull(4) ? null : reader.GetString(4),
                ChangedByUserId: reader.GetInt64(5),
                ChangedByName: reader.GetString(6),
                ChangedByRole: reader.GetString(7),
                AssessmentId: reader.IsDBNull(8) ? null : reader.GetInt64(8),
                AssessmentType: reader.IsDBNull(9) ? null : reader.GetString(9),
                SessionId: reader.IsDBNull(10) ? null : reader.GetInt64(10),
                SessionDate: reader.IsDBNull(11) ? null : DateOnly.FromDateTime(reader.GetDateTime(11)),
                CourtName: reader.IsDBNull(12) ? null : reader.GetString(12)
            ));
        }

        return items;
    }
}

