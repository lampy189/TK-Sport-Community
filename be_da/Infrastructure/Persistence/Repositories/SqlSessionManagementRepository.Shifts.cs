using System.Data;
using be_da.Application.DTOs;
using Microsoft.Data.SqlClient;

namespace be_da.Infrastructure.Persistence.Repositories;

public sealed partial class SqlSessionManagementRepository
{
    // An acceptance belongs to one assignment, person and schedule snapshot.
    // Removing/re-adding a person or moving the session requires fresh acceptance.
    private static string AcceptanceQuery(string table, string userColumn) => $"""
        SELECT 1 FROM dbo.audit_logs l
        WHERE l.entity_type='Session' AND l.entity_id=s.id AND l.action='shift_accepted'
          AND l.actor_id=a.{userColumn}
          AND JSON_VALUE(l.new_value,'$.table')='{table}'
          AND TRY_CONVERT(bigint,JSON_VALUE(l.new_value,'$.assignmentId'))=a.id
          AND JSON_VALUE(l.new_value,'$.date')=CONVERT(varchar(10),s.session_date,23)
          AND TRY_CONVERT(bigint,JSON_VALUE(l.new_value,'$.slotId'))=s.time_slot_id
          AND TRY_CONVERT(bigint,JSON_VALUE(l.new_value,'$.courtId'))=s.court_id
          AND l.id>ISNULL((SELECT MAX(ch.id) FROM dbo.audit_logs ch WHERE ch.entity_type='Session'
              AND ch.entity_id=s.id AND ch.action='shift_schedule_changed'),0)
        """;

    public async Task<IReadOnlyList<ManagedSession>> MyShiftsAsync(long venueId, long actorId, string role, CancellationToken ct)
    {
        if (!InScope(venueId) || role is not ("Referee" or "Staff")) return [];
        await using var connection = await OpenAsync(ct);
        var table = role == "Referee" ? "session_referees" : "session_staff";
        var column = role == "Referee" ? "referee_id" : "staff_id";
        var ids = new List<long>();
        await using (var command = new SqlCommand($"""
            SELECT s.id FROM dbo.sessions s JOIN dbo.courts c ON c.id=s.court_id
            JOIN dbo.{table} a ON a.session_id=s.id
            JOIN dbo.users u ON u.id=a.{column} JOIN dbo.roles r ON r.id=u.role_id
            WHERE c.venue_id=@venue AND u.venue_id=@venue AND u.id=@actor
              AND u.deleted_at IS NULL AND LOWER(u.status)='active' AND r.code=@role;
            """, connection))
        {
            AddParameters(command, [("@venue", venueId), ("@actor", actorId), ("@role", role)]);
            await using var reader = await command.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct)) ids.Add(reader.GetInt64(0));
        }
        return await ReadSessionsByIdsAsync(connection, ids.ToArray(), ct);
    }

    public async Task<OperationResult> AcceptShiftAsync(long venueId, long actorId, string role, long sessionId, CancellationToken ct)
    {
        if (!InScope(venueId) || role is not ("Referee" or "Staff")) return Denied();
        await using var connection = await OpenAsync(ct);
        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var session = await ReadExistingAsync(connection, transaction, venueId, sessionId, ct);
        if (session is null) return await RollbackAsync(transaction, NotFound("Không tìm thấy ca tại cơ sở này."), ct);
        var table = role == "Referee" ? "session_referees" : "session_staff";
        var column = role == "Referee" ? "referee_id" : "staff_id";
        if (!await ValidatePersonnelAsync(connection, transaction, venueId, role, [actorId], true, ct))
            return await RollbackAsync(transaction, Denied(), ct);
        long assignmentId;
        bool accepted;
        await using (var command = new SqlCommand($"""
            SELECT a.id,CASE WHEN EXISTS({AcceptanceQuery(table, column)}) THEN 1 ELSE 0 END
            FROM dbo.{table} a WITH(UPDLOCK,HOLDLOCK) JOIN dbo.sessions s ON s.id=a.session_id
            WHERE a.session_id=@session AND a.{column}=@actor;
            """, connection, transaction))
        {
            AddParameters(command, [("@session", sessionId), ("@actor", actorId)]);
            await using var reader = await command.ExecuteReaderAsync(ct);
            if (!await reader.ReadAsync(ct)) return Denied();
            assignmentId = reader.GetInt64(0);
            accepted = reader.GetInt32(1) == 1;
        }
        if (accepted) return await RollbackAsync(transaction, OperationResult.Success(new { accepted = true }), ct);
        var slot = (await ReadSessionSlotAsync(connection, transaction, venueId, sessionId, ct))!.Value;
        if (!IsFuture(slot.Date, slot.Start) || session.Value.Status.ToLowerInvariant() is "cancelled" or "canceled" or "completed" or "ended")
            return await RollbackAsync(transaction, Conflict("SHIFT_UNAVAILABLE", "Ca đã bắt đầu, đã hủy hoặc kết thúc."), ct);
        if (await HasPersonnelConflictAsync(connection, transaction, table, column, sessionId, slot.Date, slot.Start, slot.End, [actorId], ct))
            return await RollbackAsync(transaction, Conflict("PERSONNEL_CONFLICT", "Ca trùng giờ với phân công khác."), ct);
        await AuditAsync(connection, transaction, actorId, sessionId, "shift_accepted", null,
            new { table, assignmentId, date = slot.Date.ToString("yyyy-MM-dd"), slotId = session.Value.SlotId, courtId = slot.CourtId, acceptedAt = DateTimeOffset.UtcNow }, ct);
        await transaction.CommitAsync(ct);
        return OperationResult.Success(new { accepted = true });
    }
}
