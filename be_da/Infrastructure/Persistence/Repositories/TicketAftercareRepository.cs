using TKSC.Domain.Exceptions;
using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using TKSC.Application.DTOs;
using TKSC.Application.Services.Notifications;
using TKSC.Domain.Entities;
using TKSC.Infrastructure.Persistence.Contexts;
using TKSC.Application.Services;
using TKSC.Application.DTOs.Persistence;
using TKSC.Application.Interfaces.Repositories;

namespace TKSC.Infrastructure.Persistence.Repositories;
public sealed partial class TicketAftercareRepository(Sep490Context db, TimeProvider clock) : ITicketAftercareRepository
{
    public Task LoadLateEntryEligibilityAsync(Ticket ticket, DateTimeOffset now) => be_da.Infrastructure.Persistence.Repositories.LateEntrySql.ValidatePayment((Microsoft.Data.SqlClient.SqlConnection)db.Database.GetDbConnection(), (Microsoft.Data.SqlClient.SqlTransaction)db.Database.CurrentTransaction!.GetDbTransaction(), ticket, now);
    private DateTimeOffset Now => clock.GetUtcNow();

    private static TKSC.Domain.Exceptions.PaymentException Error(int status, string message) => new(status, message);
    public async Task<T> Transaction<T>(Func<Task<T>> work)
    {
        db.ChangeTracker.Clear();
        if(db.Database.CurrentTransaction!=null){var joined=await work();await db.SaveChangesAsync();return joined;}
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var result = await work();
        await db.SaveChangesAsync();
        await tx.CommitAsync();
        return result;
    }

    public async Task LockOrder(long id) => _ = await db.SocialBookingOrders.FromSqlInterpolated($"SELECT * FROM social_booking_orders WITH (UPDLOCK,HOLDLOCK) WHERE id={id}").SingleOrDefaultAsync();
    private IQueryable<Ticket> Occupied(long sessionId, long except = 0) => db.Tickets.Where(t => t.SessionId == sessionId && t.Id != except && (new[] { "confirmed", "checkedin", "valid", "paid" }.Contains(t.Status.ToLower()) || (new[] { "pending", "reserved" }.Contains(t.Status.ToLower()) && t.Order.ExpiresAt > Now)));
    public void AddAuditLogs(AuditLog value) => db.AuditLogs.Add(value);
    public void AddNotifications(IEnumerable<Notification> value) => db.Notifications.AddRange(value);
    public Task<Session?> SessionSessionsAsync(long id) => be_da.Infrastructure.Persistence.OwnerPrice.Resolve(db, db.Sessions.FromSqlInterpolated($"SELECT * FROM sessions WITH (UPDLOCK,HOLDLOCK) WHERE id={id}").SingleOrDefaultAsync());
    public Task<Session?> GetSessionDetailsAsync(long id) => be_da.Infrastructure.Persistence.OwnerPrice.Resolve(db, db.Sessions.Include(s => s.TimeSlot).Include(s => s.Sport).Include(s => s.Court).ThenInclude(c => c.Venue).SingleOrDefaultAsync(s => s.Id == id));
    public Task<long?> TicketTicketsAsync(long id) => db.Tickets.Where(t => t.Id == id).Select(t => (long? )t.OrderId).SingleOrDefaultAsync();
    public Task<Ticket> GetTicketDetailsAsync(long id) => db.Tickets.Include(t => t.Order).ThenInclude(o => o.Payments).Include(t => t.Player).ThenInclude(p => p.User).Include(t => t.Refunds).SingleAsync(t => t.Id == id);
    public Task<bool> EligibilityPlayersExistsAsync(long playerId) => db.Players.AnyAsync(p => p.Id == playerId && p.User.DeletedAt == null && p.User.Status.ToLower() == "active");
    public Task<bool> EligibilitySessionExceptionsExistsAsync(long playerId, Session session) => db.SessionExceptions.AnyAsync(e => e.PlayerId == playerId && e.SessionId == session.Id && e.ApprovedBy != null);
    public Task<PlayerSportProfile?> EligibilityPlayerSportProfilesAsync(long playerId, Session session) => db.PlayerSportProfiles.SingleOrDefaultAsync(p => p.PlayerId == playerId && p.SportId == session.SportId);
    public Task<bool> EligibilityTicketsExistsAsync(long playerId, Session session, long except, DateTimeOffset now) => db.Tickets.AnyAsync(t => t.Id != except && t.PlayerId == playerId && t.Session.SessionDate == session.SessionDate && t.Session.TimeSlot.StartTime < session.TimeSlot.EndTime && t.Session.TimeSlot.EndTime > session.TimeSlot.StartTime && (new[] { "confirmed", "checkedin", "valid", "paid" }.Contains(t.Status.ToLower()) || (new[] { "pending", "reserved" }.Contains(t.Status.ToLower()) && t.Order.ExpiresAt > now)));
    public Task<List<TicketFunding>> FundingTicketFundingListAsync(Ticket t) => db.TicketFunding.Where(f => f.TicketId == t.Id && !f.Retired).OrderBy(f => f.Id).ToListAsync();
    public Task<Payment?> FundingPaymentsAsync(Ticket t) => db.Payments.Where(p => p.OrderId == t.OrderId && p.BankTransactions.Any(b => b.MatchStatus == "matched")).FirstOrDefaultAsync();
    public void AddTicketFunding(TicketFunding value) => db.TicketFunding.Add(value);
    public Task<Payment> RefundPaymentsAsync(long paymentId) => db.Payments.SingleAsync(p => p.Id == paymentId);
    public Task<long?> RefundRefundsTotalAsync(long paymentId) => db.Refunds.Where(r => r.PaymentId == paymentId && r.Status != "rejected").SumAsync(r => (long? )r.AmountVnd);
    public long RefundChangeTrackerSum(long paymentId) => db.ChangeTracker.Entries<Refund>().Where(e => e.State == EntityState.Added && e.Entity.PaymentId == paymentId).Sum(e => (long)(e.Entity.AmountVnd ?? 0));
    public void AddRefunds(Refund value) => db.Refunds.Add(value);
    public Task<List<AftercareSessionSummary>> SessionsSessionsListAsync(bool issuesOnly, DateOnly today) => db.Sessions.AsNoTracking().Where(s => issuesOnly ? s.Status.ToLower() == "suspended" || s.Status.ToLower() == "cancelled" : s.SessionDate >= today.AddDays(-1)).OrderBy(s => s.SessionDate).ThenBy(s => s.TimeSlot.StartTime).Select(s => new AftercareSessionSummary { Id = s.Id, SessionDate = s.SessionDate, Status = s.Status, StartTime = s.TimeSlot.StartTime, EndTime = s.TimeSlot.EndTime, Sport = s.Sport.Name, Court = s.Court.Name, Venue = s.Court.Venue.Name, Reason = db.AuditLogs.Where(a => a.EntityType == "Session" && a.EntityId == s.Id && a.Action == "venue_condition_issue").OrderByDescending(a => a.Id).Select(a => a.NewValue).FirstOrDefault() }).ToListAsync();
    public Task<List<Ticket>> ReportTicketsListAsync(long id) => db.Tickets.Include(t => t.Player).Include(t => t.Order).Where(t => t.SessionId == id && (t.Status.ToLower() == "confirmed" || t.Status.ToLower() == "pending" || t.Status.ToLower() == "reserved")).ToListAsync();
    public Task<Ticket?> OptionsTicketsAsync(long id, long user) => db.Tickets.Include(t => t.Player).Include(t => t.Order).Include(t => t.Refunds).Include(t => t.Session).ThenInclude(s => s.TimeSlot).Include(t => t.Session).ThenInclude(s => s.Court).SingleOrDefaultAsync(t => t.Id == id && (t.Player.UserId == user || t.Order.PayerId == user));
    private Task<List<Session>> LoadOptionsSessionsAsync(Ticket ticket, DateOnly today) => db.Sessions.Include(s => s.TimeSlot).Include(s => s.Court).Include(s => s.Sport).Where(s => s.Id != ticket.SessionId && s.Court.VenueId == ticket.Session.Court.VenueId && s.SportId == ticket.Session.SportId && s.SessionDate >= today && (s.Status.ToLower() == "open" || s.Status.ToLower() == "published")).OrderBy(s => s.SessionDate).ThenBy(s => s.TimeSlot.StartTime).Take(100).ToListAsync();
    public async Task<List<Session>> OptionsSessionsListAsync(Ticket ticket, DateOnly today)
    {
        var sessions=await LoadOptionsSessionsAsync(ticket,today);
        foreach(var session in sessions) await be_da.Infrastructure.Persistence.OwnerPrice.Resolve(db,Task.FromResult<Session?>(session));
        return sessions;
    }
    public Task<int> OptionsCountAsync(Session target) => Occupied(target.Id).CountAsync();
    public Task<bool> RequestChangeTicketChangesExistsAsync(long id) => db.TicketChanges.AnyAsync(c => c.TicketId == id && c.Status == "requested");
    public Task<int> RequestChangeCountAsync(long targetId) => Occupied(targetId).CountAsync();
    public void AddTicketChanges(TicketChange value) => db.TicketChanges.Add(value);
    public Task<int> SaveAsync() => db.SaveChangesAsync();
    public Task<TicketChange?> CancelChangeTicketChangesAsync(long id) => db.TicketChanges.AsNoTracking().SingleOrDefaultAsync(c => c.Id == id);
    public Task<TicketChange> GetTrackedChangeForCancellationAsync(long id) => db.TicketChanges.SingleAsync(c => c.Id == id);
    public Task<bool> RequestRefundTicketChangesExistsAsync(long id) => db.TicketChanges.AnyAsync(c => c.TicketId == id && c.Status == "requested");
    public Task<List<TicketChangeSummary>> ChangesTicketChangesListAsync() => (
        from c in db.TicketChanges.AsNoTracking()join t in db.Tickets on c.TicketId equals t.Id
        join s in db.Sessions on c.TargetSessionId equals s.Id
        where c.Status == "requested"
        orderby c.Id descending
        select new TicketChangeSummary
        {
            Id = c.Id,
            TicketId = c.TicketId,
            OldPriceVnd = c.OldPriceVnd,
            NewPriceVnd = c.NewPriceVnd,
            Status = c.Status,
            Holder = t.Player.User.FullName,
            Payer = db.Users.Where(u=>u.Id==t.Order.PayerId).Select(u=>u.FullName).FirstOrDefault() ?? "Người thanh toán gốc",
            Court = s.Court.Name,
            SessionDate = s.SessionDate,
            StartTime = s.TimeSlot.StartTime,
            EndTime = s.TimeSlot.EndTime
        }

    ).ToListAsync();
    public Task<TicketChange?> CompleteChangeTicketChangesAsync(long id) => db.TicketChanges.AsNoTracking().SingleOrDefaultAsync(c => c.Id == id);
    public Task<TicketChange> GetTrackedChangeForSettlementAsync(long id) => db.TicketChanges.SingleAsync(c => c.Id == id);
    public Task<int> CompleteChangeCountAsync(Session target) => Occupied(target.Id).CountAsync();
    public Task<bool> CompleteChangeBankTransactionsExistsAsync(string? bankReference) => db.BankTransactions.AnyAsync(b => b.ExternalId == "staff:" + bankReference || b.ExternalId == "payos:" + bankReference || b.ExternalId == bankReference || b.ReferenceCode == bankReference);
    public Task<bool> CompleteChangePaymentsExistsAsync(string? bankReference) => db.Payments.AnyAsync(p => p.ReferenceCode == bankReference);
    public void AddSocialBookingOrders(SocialBookingOrder value) => db.SocialBookingOrders.Add(value);
    public void AddTickets(Ticket value) => db.Tickets.Add(value);
    public void AddPayments(Payment value) => db.Payments.Add(value);
    public void AddBankTransactions(BankTransaction value) => db.BankTransactions.Add(value);
    public Task<List<PartnerRequest>> CompleteChangePartnerRequestsListAsync(Ticket t) => db.PartnerRequests.Where(p => p.SessionId == t.SessionId && (p.RequesterId == t.PlayerId || p.PartnerId == t.PlayerId)).ToListAsync();
    public Task<List<TicketRefundSummary>> RefundsRefundsListAsync() => db.Refunds.AsNoTracking().OrderByDescending(r => r.Id).Select(r => new TicketRefundSummary { RecipientId=r.RecipientId, Id = r.Id, TicketId = r.TicketId, AmountVnd = r.AmountVnd, Status = r.Status, Recipient = db.Users.Where(u=>u.Id==r.RecipientId).Select(u=>u.FullName).FirstOrDefault() ?? "Người thanh toán gốc", Email = db.Users.Where(u=>u.Id==r.RecipientId).Select(u=>u.Email).FirstOrDefault() ?? "", ReferenceCode = r.Payment.ReferenceCode, ApprovedBy = r.ApprovedBy, Decision = db.AuditLogs.Where(a => a.EntityType == "Refund" && a.EntityId == r.Id && a.Action == "refund_decision").OrderByDescending(a => a.Id).Select(a => a.NewValue).FirstOrDefault(), Payout = db.AuditLogs.Where(a => a.EntityType == "Refund" && a.EntityId == r.Id && a.Action == "refund_payout").OrderByDescending(a => a.Id).Select(a => a.NewValue).FirstOrDefault() }).ToListAsync();
    public Task<Refund?> CompleteRefundRefundsAsync(long id) => db.Refunds.AsNoTracking().SingleOrDefaultAsync(r => r.Id == id);
    public Task<Refund> GetTrackedRefundForSettlementAsync(long id) => db.Refunds.SingleAsync(r => r.Id == id);
    public Task<bool> CompleteRefundAuditLogsExistsAsync(string? reference) => db.AuditLogs.AnyAsync(a => (a.Action == "refund_transfer_reference" || a.Action == "return_transfer_reference") && a.NewValue == reference);
    public Task<Payment> CompleteRefundPaymentsAsync(Refund r) => db.Payments.SingleAsync(p => p.Id == r.PaymentId);
    public Task<long?> CompleteRefundRefundsTotalAsync(Refund r) => db.Refunds.Where(x => x.PaymentId == r.PaymentId && x.Status != "rejected").SumAsync(x => (long? )x.AmountVnd);
    public Task<List<FlaggedTicketPayment>> ReviewsPaymentsListAsync() => db.Payments.AsNoTracking().Where(p => p.Status == "review_required" && p.OrderId != null).OrderByDescending(p => p.Id).Select(p => new FlaggedTicketPayment { Id = p.Id, OrderId = p.OrderId, ReferenceCode = p.ReferenceCode, AmountVnd = p.AmountVnd, Payer = db.Users.Where(u=>u.Id==p.PayerId).Select(u=>u.FullName).FirstOrDefault() ?? "Người thanh toán gốc", Transactions = p.BankTransactions.Where(b => b.MatchStatus == "review_required").Select(b => new FlaggedBankTransaction { Id = b.Id, ExternalId = b.ExternalId, AmountVnd = b.AmountVnd, ReferenceCode = b.ReferenceCode }).ToList() }).ToListAsync();
    public Task<Payment?> ApprovePaymentPaymentsAsync(long id) => db.Payments.AsNoTracking().SingleOrDefaultAsync(p => p.Id == id && p.OrderId != null && p.GoodsOrderId == null);
    public Task<SocialBookingOrder> ApprovePaymentSocialBookingOrdersAsync(Payment lookup) => db.SocialBookingOrders.Include(o => o.Payments).SingleAsync(o => o.Id == lookup.OrderId);
    public Task<BankTransaction?> ApprovePaymentBankTransactionsAsync(long id, long bankId) => db.BankTransactions.SingleOrDefaultAsync(b => b.Id == bankId && b.MatchedPaymentId == id);
    public Task<List<Ticket>> ApprovePaymentTicketsListAsync(SocialBookingOrder order) => db.Tickets.Include(t => t.Player).Where(t => t.OrderId == order.Id && t.Status.ToLower() != "cancelled").ToListAsync();
    public Task<int> ApprovePaymentCountAsync(SocialBookingOrder order, Session session) => Occupied(session.Id).Where(t => t.OrderId != order.Id).CountAsync();
}
