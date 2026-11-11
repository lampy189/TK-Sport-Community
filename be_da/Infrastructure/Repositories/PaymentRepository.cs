using System.Data;
<<<<<<< HEAD
=======
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore.Storage;
>>>>>>> 0d40ca35efaf07c5d4887d6f8febc66c57f5578c
using TKSC.Application.Notifications;
using Microsoft.EntityFrameworkCore;
using TKSC.Application.DTOs;
using TKSC.Application.Interfaces;
using TKSC.Domain.Entities;
using TKSC.Infrastructure.DbContext;
using TKSC.Application.Mappings;

namespace TKSC.Infrastructure.Repositories;

public sealed class PaymentRepository(Sep490Context db) : IPaymentRepository
{
    public async Task<OrderSummary?> GetOrderAsync(long orderId, long payerId)
    {
        var order = await db.SocialBookingOrders.AsNoTracking().Include(o => o.Payments).Include(o => o.Tickets)
            .SingleOrDefaultAsync(o => o.Id == orderId && o.PayerId == payerId);
        if (order is null) return null;
        var tickets = await db.Tickets.AsNoTracking().Where(t => t.OrderId == orderId)
            .Select(PaymentMappings.TicketProjection).ToListAsync();
        return PaymentMappings.ToSummary(order) with { Tickets = await Decorate(tickets, payerId) };
    }
    public async Task<IReadOnlyList<TicketSummary>> GetTicketsAsync(long userId)
    {
        var tickets = await db.Tickets.AsNoTracking().Where(t => t.Player.UserId == userId || t.Order.PayerId == userId)
            .Select(PaymentMappings.TicketProjection).ToListAsync();
        return await Decorate(tickets, userId);
    }

    private async Task<IReadOnlyList<TicketSummary>> Decorate(List<TicketSummary> tickets, long userId)
    {
        var ids=tickets.Select(t=>t.Id).ToList(); var sessions=tickets.Select(t=>t.SessionId).Distinct().ToList();
        var owned=await db.SocialBookingOrders.Where(o=>o.PayerId==userId).Select(o=>o.Id).ToListAsync();
        var held=await db.Tickets.Where(t=>ids.Contains(t.Id) && t.Player.UserId==userId).Select(t=>t.Id).ToListAsync();
        var changes=await db.TicketChanges.AsNoTracking().Where(c=>ids.Contains(c.TicketId) && c.Status=="requested").ToListAsync();
        var issues=await db.AuditLogs.AsNoTracking().Where(a=>a.EntityType=="Session" && a.Action=="venue_condition_issue" && a.EntityId!=null && sessions.Contains(a.EntityId.Value)).OrderByDescending(a=>a.Id).ToListAsync();
<<<<<<< HEAD
        return tickets.Select(t=>t with {CanManage=owned.Contains(t.OrderId), CanResolveIssue=owned.Contains(t.OrderId)||held.Contains(t.Id), PendingChangeId=changes.FirstOrDefault(c=>c.TicketId==t.Id)?.Id, VenueIssue=issues.FirstOrDefault(a=>a.EntityId==t.SessionId)?.NewValue?.Split(" | ", 2).Last()}).ToList();
=======
        var lateEnds=new Dictionary<long,DateTimeOffset>();
        if(tickets.Any(t=>TKSC.Domain.Payments.LateEntryRules.IsLate(t.TicketCode)))
        {
            await db.Database.OpenConnectionAsync();
            try
            {
                await using var cmd=db.Database.GetDbConnection().CreateCommand();
                cmd.CommandText="SELECT lt.ticket_id,lt.ends_at FROM dbo.late_entry_tickets lt JOIN dbo.tickets t ON t.id=lt.ticket_id JOIN dbo.players p ON p.id=t.player_id JOIN dbo.social_booking_orders o ON o.id=t.order_id WHERE p.user_id=@user OR o.payer_id=@user";
                var p=cmd.CreateParameter();p.ParameterName="@user";p.Value=userId;cmd.Parameters.Add(p);
                await using var reader=await cmd.ExecuteReaderAsync();while(await reader.ReadAsync())lateEnds[reader.GetInt64(0)]=reader.GetFieldValue<DateTimeOffset>(1);
            }
            finally{await db.Database.CloseConnectionAsync();}
        }
        return tickets.Select(t=>t with {CanManage=owned.Contains(t.OrderId), CanResolveIssue=owned.Contains(t.OrderId)||held.Contains(t.Id), PendingChangeId=changes.FirstOrDefault(c=>c.TicketId==t.Id)?.Id, VenueIssue=issues.FirstOrDefault(a=>a.EntityId==t.SessionId)?.NewValue?.Split(" | ", 2).Last(),TicketKind=TKSC.Domain.Payments.LateEntryRules.IsLate(t.TicketCode)?"remaining":"social",ValidUntil=lateEnds.TryGetValue(t.Id,out var until)?until:null,Valid=t.Valid&&(!TKSC.Domain.Payments.LateEntryRules.IsLate(t.TicketCode)||(lateEnds.TryGetValue(t.Id,out var end)&&end>DateTimeOffset.UtcNow))}).ToList();
>>>>>>> 0d40ca35efaf07c5d4887d6f8febc66c57f5578c
    }
    public async Task<IReadOnlyList<ReviewSummary>> GetReviewsAsync()
    {
        var payments = await db.Payments.AsNoTracking().Where(p => p.Status == "review_required" && p.OrderId != null && p.GoodsOrderId == null)
            .OrderByDescending(p => p.Id).ToListAsync();
        var ids = payments.Select(p => p.Id).ToList();
        var audits = await db.AuditLogs.AsNoTracking().Where(a => a.EntityType == "Payment" && a.EntityId != null
            && ids.Contains(a.EntityId.Value)).ToListAsync();
        var result = payments.Select(p => new ReviewSummary(p.Id, p.OrderId, p.ReferenceCode, p.AmountVnd, p.Status,
            audits.Where(a => a.EntityId == p.Id).Select(a => new ReviewReason(a.Action, a.NewValue)).ToList())).ToList();
        var unmatched = await db.BankTransactions.AsNoTracking().Where(e => e.MatchedPaymentId == null
            && e.MatchStatus == "review_required").OrderByDescending(e => e.Id).ToListAsync();
        result.AddRange(unmatched.Select(e => new ReviewSummary(-e.Id, null, e.ReferenceCode ?? e.ExternalId,
            e.AmountVnd, "review_required", [new("unmatched_transfer", "Giao dịch đã xác thực nhưng không khớp đơn social.")])));
        return result;
    }
    public Task<long?> FindOrderForPaymentAsync(long paymentId) => db.Payments.AsNoTracking()
        .Where(p => p.Id == paymentId && p.Method == "payos").Select(p => p.OrderId).SingleOrDefaultAsync();

    public async Task<T> InTransactionAsync<T>(long? orderId, string? eventKey, Func<SocialBookingOrder?, Task<T>> action)
    {
        // Avoid stale tracked entities between the two phases of a gateway request.
        db.ChangeTracker.Clear();
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        if (eventKey is not null)
        {
            // Same bank reference must be processed once even if delivered against different orders.
            await db.Database.ExecuteSqlInterpolatedAsync($@"
                DECLARE @result int;
                EXEC @result = sp_getapplock @Resource={eventKey}, @LockMode='Exclusive',
                    @LockOwner='Transaction', @LockTimeout=10000;
                IF @result < 0 THROW 51000, 'Cannot acquire payment event lock', 1;");
        }
        SocialBookingOrder? order = null;
        if (orderId is not null)
        {
            order = await db.SocialBookingOrders.FromSqlInterpolated(
                $"SELECT * FROM social_booking_orders WITH (UPDLOCK, HOLDLOCK) WHERE id = {orderId.Value}")
                .SingleOrDefaultAsync();
            if (order is not null)
            {
                await db.Entry(order).Collection(o => o.Payments).LoadAsync();
                await db.Entry(order).Collection(o => o.Tickets).LoadAsync();
                foreach(var sessionId in order.Tickets.Select(t=>t.SessionId).Distinct().OrderBy(id=>id))
                {
                    var session=await db.Sessions.FromSqlInterpolated($"SELECT * FROM sessions WITH (UPDLOCK,HOLDLOCK) WHERE id={sessionId}").SingleAsync();
                    foreach(var ticket in order.Tickets.Where(t=>t.SessionId==sessionId)) ticket.Session=session;
                }
<<<<<<< HEAD
=======
                foreach(var ticket in order.Tickets.Where(t=>TKSC.Domain.Payments.LateEntryRules.IsLate(t.TicketCode)))
                    await be_da.Infrastructure.Persistence.LateEntrySql.ValidatePayment((SqlConnection)db.Database.GetDbConnection(),(SqlTransaction)transaction.GetDbTransaction(),ticket,DateTimeOffset.UtcNow);
>>>>>>> 0d40ca35efaf07c5d4887d6f8febc66c57f5578c
            }
        }
        var previousOrder = order?.Status ?? "";
        var previousTickets = order?.Tickets.ToDictionary(t => t.Id, t => t.Status) ?? new Dictionary<long, string>();
        var result = await action(order);
        if (order is not null)
        {
            foreach (var ticket in order.Tickets)
            {
                var kind = TicketNotifications.Event(previousTickets.GetValueOrDefault(ticket.Id, ""), ticket.Status, previousOrder, order.Status);
                if (kind is null) continue;
                var holderId = await db.Players.Where(p => p.Id == ticket.PlayerId).Select(p => p.UserId).SingleAsync();
                db.Notifications.AddRange(TicketNotifications.Create(kind, ticket.Id, order.PayerId, holderId));
            }
            await db.SaveChangesAsync();
        }
        await transaction.CommitAsync();
        return result;
    }
    public Task<bool> EventExistsAsync(string externalId) => db.BankTransactions.AnyAsync(e => e.ExternalId == externalId);
    public void AddPayment(Payment payment) => db.Payments.Add(payment);
    public void AddEvent(BankTransaction bankEvent) => db.BankTransactions.Add(bankEvent);
    public void AddAudit(AuditLog audit) => db.AuditLogs.Add(audit);
    public Task<TicketCancelContext?> GetTicketCancelContextAsync(long ticketId) => db.Tickets.AsNoTracking()
        .Where(t => t.Id == ticketId)
        .Select(t => new TicketCancelContext(t.OrderId, t.Session.SessionDate, t.Session.TimeSlot.StartTime))
        .SingleOrDefaultAsync();
    public void AddRefund(Refund refund) => db.Refunds.Add(refund);
    public Task<long?> FindOrderForTicketCodeAsync(string code) => db.Tickets.AsNoTracking()
        .Where(t => t.TicketCode == code).Select(t => (long?)t.OrderId).SingleOrDefaultAsync();
    public async Task<IReadOnlyList<AssignedSessionSummary>> GetAssignedSessionsAsync(long actorId)
    {
<<<<<<< HEAD
        var sessions = await db.SessionReferees.AsNoTracking()
            .Where(sr => sr.RefereeId == actorId)
            .OrderBy(sr => sr.Session.SessionDate).ThenBy(sr => sr.Session.TimeSlot.StartTime)
            .Select(sr => new AssignedSessionSummary(
                sr.SessionId,
                sr.Session.Sport.Name,
                sr.Session.Court.Name ?? "Sân chưa đặt tên",
                sr.Session.Court.Venue.Name ?? "TK Sport",
                sr.Session.SessionDate,
                sr.Session.TimeSlot.StartTime,
                sr.Session.TimeSlot.EndTime,
                sr.Session.Status,
                sr.Session.Band,
                sr.Session.Capacity,
                (short)sr.Session.Tickets.Count(),
=======
        // Preserve assigned-referee admission on databases awaiting the center upgrade.
        // Managers remain denied until their persisted venue assignment exists.
        var scoped = await HasVenueAssignmentAsync()
            ? db.Sessions.FromSqlInterpolated($"""
            SELECT s.* FROM dbo.sessions s JOIN dbo.courts c ON c.id=s.court_id
            WHERE EXISTS(SELECT 1 FROM dbo.session_referees sr WHERE sr.session_id=s.id AND sr.referee_id={actorId})
             OR EXISTS(SELECT 1 FROM dbo.users u JOIN dbo.roles r ON r.id=u.role_id WHERE u.id={actorId}
               AND LOWER(r.code)='manager' AND LOWER(u.status)='active' AND u.deleted_at IS NULL AND u.venue_id=c.venue_id)
            """)
            : db.Sessions.Where(s=>s.SessionReferees.Any(sr=>sr.RefereeId==actorId));
        var sessions = await scoped.AsNoTracking()
            .OrderBy(sr => sr.SessionDate).ThenBy(sr => sr.TimeSlot.StartTime)
            .Select(sr => new AssignedSessionSummary(
                sr.Id,
                sr.Sport.Name,
                sr.Court.Name ?? "Sân chưa đặt tên",
                sr.Court.Venue.Name ?? "TK Sport",
                sr.SessionDate,
                sr.TimeSlot.StartTime,
                sr.TimeSlot.EndTime,
                sr.Status,
                sr.Band,
                sr.Capacity,
                (short)sr.Tickets.Count(t=>t.Status.ToLower()=="confirmed"||t.Status.ToLower()=="checkedin"||((t.Status.ToLower()=="pending"||t.Status.ToLower()=="reserved")&&t.Order.ExpiresAt>DateTimeOffset.UtcNow)),
>>>>>>> 0d40ca35efaf07c5d4887d6f8febc66c57f5578c
                true,
                "not_ready",
                "Đang chờ kiểm tra điều kiện buổi chơi."))
            .ToListAsync();
        return sessions;
    }
<<<<<<< HEAD
    public Task<CheckInContext?> GetCheckInContextAsync(string code, long actorId) => db.Tickets.AsNoTracking()
        .Where(t => t.TicketCode == code).Select(t => new CheckInContext(t.Id, t.OrderId,
            t.Session.SessionDate, t.Session.TimeSlot.EndTime, t.Session.Status,
            t.Session.SessionReferees.Any(r => r.RefereeId == actorId), t.Player.User.FullName,
            t.Session.Sport.Name, t.Session.Court.Name ?? "Sân chưa đặt tên", t.Session.TimeSlot.StartTime)).SingleOrDefaultAsync();
=======
    private async Task<bool> HasVenueAssignmentAsync() => await db.Database.SqlQueryRaw<int>(
        "SELECT CASE WHEN COL_LENGTH('dbo.users','venue_id') IS NOT NULL THEN 1 ELSE 0 END AS [Value]").SingleAsync()==1;

    public async Task<CheckInContext?> GetCheckInContextAsync(string code, long actorId)
    {
        var scoped = await HasVenueAssignmentAsync()
            ? db.Tickets.FromSqlInterpolated($"""
        SELECT t.* FROM dbo.tickets t JOIN dbo.sessions s ON s.id=t.session_id JOIN dbo.courts c ON c.id=s.court_id
        WHERE EXISTS(SELECT 1 FROM dbo.session_referees sr WHERE sr.session_id=s.id AND sr.referee_id={actorId})
         OR EXISTS(SELECT 1 FROM dbo.users u JOIN dbo.roles r ON r.id=u.role_id WHERE u.id={actorId}
           AND LOWER(r.code)='manager' AND LOWER(u.status)='active' AND u.deleted_at IS NULL AND u.venue_id=c.venue_id)
        """)
            : db.Tickets.Where(t=>t.Session.SessionReferees.Any(sr=>sr.RefereeId==actorId));
        return await scoped.AsNoTracking().Where(t => t.TicketCode == code).Select(t => new CheckInContext(t.Id, t.OrderId,
            t.Session.SessionDate, t.Session.TimeSlot.EndTime, t.Session.Status,
            t.Session.SessionReferees.Any(r => r.RefereeId == actorId), t.Player.User.FullName,
            t.Session.Sport.Name, t.Session.Court.Name ?? "Sân chưa đặt tên", t.Session.TimeSlot.StartTime)).SingleOrDefaultAsync();
    }
>>>>>>> 0d40ca35efaf07c5d4887d6f8febc66c57f5578c
    public Task<bool> HasPendingChangeAsync(long ticketId) => db.TicketChanges.AnyAsync(c=>c.TicketId==ticketId && c.Status=="requested");
    public async Task<IReadOnlyList<TicketFundingSummary>> GetFundingAsync(long ticketId) => await db.TicketFunding.Where(f=>f.TicketId==ticketId && !f.Retired).Select(f=>new TicketFundingSummary(f.PaymentId,f.AmountVnd)).ToListAsync();
    public Task SaveAsync() => db.SaveChangesAsync();
}
