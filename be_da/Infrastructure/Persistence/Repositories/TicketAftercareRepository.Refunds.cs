using Microsoft.EntityFrameworkCore;
using TKSC.Domain.Entities;

namespace TKSC.Infrastructure.Persistence.Repositories;
public sealed partial class TicketAftercareRepository
{
    // Identity checks are global; resource access is checked on the scoped refund first.
    public Task<User?> RefundActorAsync(long actor) => db.Users.IgnoreQueryFilters().Include(u => u.Role).SingleOrDefaultAsync(u => u.Id == actor && u.DeletedAt == null && u.Status.ToLower() == "active");
    public async Task<bool> SameRefundPersonAsync(long actor, long other)
    {
        if (actor == other)
            return true;
        var users = await db.Users.IgnoreQueryFilters().Where(u => u.Id == actor || u.Id == other).ToListAsync();
        return users.Count == 2 && ((!string.IsNullOrWhiteSpace(users[0].GoogleSubject) && users[0].GoogleSubject == users[1].GoogleSubject) || (!string.IsNullOrWhiteSpace(users[0].Email) && string.Equals(users[0].Email.Trim(), users[1].Email.Trim(), StringComparison.OrdinalIgnoreCase)));
    }

    public Task<AuditLog?> RefundAuditAsync(long id, string action) => db.AuditLogs.AsNoTracking().Where(a => a.EntityType == "Refund" && a.EntityId == id && a.Action == action).OrderByDescending(a => a.Id).FirstOrDefaultAsync();
    public Task<List<AuditLog>> RefundHistoryAsync(long id) => db.AuditLogs.AsNoTracking().Where(a => a.EntityType == "Refund" && a.EntityId == id).OrderBy(a => a.Id).ToListAsync();
    public async Task<long> RefundTicketTotalAsync(long ticketId) => await db.Refunds.Where(r => r.TicketId == ticketId && r.Status != "rejected").SumAsync(r => (long? )r.AmountVnd) ?? 0;
    public async Task<long> RefundAllocationAsync(long ticketId, long paymentId) => await db.TicketFunding.Where(f => f.TicketId == ticketId && f.PaymentId == paymentId).SumAsync(f => (long? )f.AmountVnd) ?? 0;
    public async Task<long> RefundAllocationReservedAsync(long ticketId, long paymentId) => await db.Refunds.Where(r => r.TicketId == ticketId && r.PaymentId == paymentId && r.Status != "rejected").SumAsync(r => (long? )r.AmountVnd) ?? 0;
    public async Task LockRefundPaymentAsync(long paymentId) => _ = await db.Payments.FromSqlInterpolated($"SELECT * FROM payments WITH (UPDLOCK,HOLDLOCK) WHERE id={paymentId}").SingleAsync();
    public void AddRefundApproval(Approval approval) => db.Approvals.Add(approval);
}
