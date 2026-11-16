using System.Data;
using Microsoft.Data.SqlClient;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TKSC.Application.DTOs;
using TKSC.Application.Interfaces.Repositories;
using TKSC.Domain.Entities;
using TKSC.Domain.Exceptions;
using TKSC.Infrastructure.Persistence.Contexts;

namespace TKSC.Infrastructure.Persistence.Repositories;
public sealed class IncidentRepository(Sep490Context db) : IIncidentRepository
{
    public async Task<T> Transaction<T>(Func<Task<T>> action)
    {
        await using var tx = db.Database.CurrentTransaction == null ? await db.Database.BeginTransactionAsync(IsolationLevel.Serializable) : null;
        try
        {
            var result = await action();
            await db.SaveChangesAsync();
            if (tx != null)
                await tx.CommitAsync();
            return result;
        }
        catch (SqlException e)when (e.Number == 1205)
        {
            if (tx != null)
                await tx.RollbackAsync();
            db.ChangeTracker.Clear();
            throw new PaymentException(409, "Sự cố đang được cập nhật; tải lại lịch sử trước khi thử lại.");
        }
        catch
        {
            if (tx != null)
                await tx.RollbackAsync();
            db.ChangeTracker.Clear();
            throw;
        }
    }

    public async Task<IncidentActor> Actor(long id)
    {
        var a = await db.Users.Include(u => u.Role).AsNoTracking().SingleOrDefaultAsync(u => u.Id == id && u.DeletedAt == null);
        if (a == null || !a.Status.Equals("active", StringComparison.OrdinalIgnoreCase) || !new[]
        {
            "staff",
            "manager",
            "owner"
        }.Contains(a.Role.Code.ToLowerInvariant()))
            throw new PaymentException(403, "Không có quyền xử lý sự cố.");
        return new(a.Id, a.FullName, a.Role.Code.ToLowerInvariant(), a.VenueId);
    }

    public async Task<IReadOnlyList<IncidentActor>> People(long venueId) => await db.Users.AsNoTracking().Where(u => u.VenueId == venueId && u.DeletedAt == null && u.Status.ToLower() == "active" && (u.Role.Code.ToLower() == "staff" || u.Role.Code.ToLower() == "manager")).Select(u => new IncidentActor(u.Id, u.FullName, u.Role.Code, u.VenueId)).ToListAsync();
    public async Task<IncidentResource?> Resource(string kind, long id)
    {
        if (kind == "operations")
        {
            var item = await Case(id);
            if (item == null || item.Kind == "operations")
                return null;
            var source = await Resource(item.Kind, item.SubjectId);
            return source == null ? null : new(kind, id, source.VenueId, $"Ngoại lệ #{id} · {source.Title}", item.Status, item.Status is "closed" or "rejected");
        }

        if (kind == "products")
        {
            var venue = await db.Database.SqlQueryRaw<long?>("SELECT venue_id AS Value FROM dbo.products WHERE id={0}", id).SingleOrDefaultAsync();
            var product = await db.Products.AsNoTracking().SingleOrDefaultAsync(p => p.Id == id);
            return venue == null || product == null ? null : new(kind, id, venue.Value, product.Name, "stock", true);
        }

        if (kind == "venues")
        {
            var venue = await db.Venues.AsNoTracking().SingleOrDefaultAsync(v => v.Id == id);
            return venue == null ? null : new(kind, id, id, venue.Name ?? $"Cơ sở #{id}", "operational", true);
        }

        if (kind == "inbox")
        {
            var t = await db.Tickets.AsNoTracking().Include(t => t.Session).ThenInclude(s => s.Court).Include(t => t.Order).SingleOrDefaultAsync(t => t.Id == id);
            if (t == null)
                return null;
            var pending = await db.Refunds.AnyAsync(r => r.TicketId == id && r.Status != "completed" && r.Status != "rejected");
            return new(kind, id, t.Session.Court.VenueId, $"Vé #{id}", t.Status, !pending && t.Order.Status != "review_required");
        }

        if (kind == "sessions")
        {
            var s = await db.Sessions.AsNoTracking().Include(s => s.Court).SingleOrDefaultAsync(s => s.Id == id);
            if (s == null)
                return null;
            var pending = await db.Refunds.AnyAsync(r => r.Ticket.SessionId == id && r.Status != "completed" && r.Status != "rejected");
            var unhandled = await db.Tickets.AnyAsync(t => t.SessionId == id && t.Status == "confirmed");
            return new(kind, id, s.Court.VenueId, $"Buổi #{id}", s.Status, !pending && (!new[] { "suspended", "cancelled" }.Contains(s.Status.ToLower()) || !unhandled));
        }

        if (kind == "refunds")
        {
            var r = await db.Refunds.AsNoTracking().Include(r => r.Ticket).ThenInclude(t => t.Session).ThenInclude(s => s.Court).SingleOrDefaultAsync(r => r.Id == id);
            if (r == null)
                return null;
            return new(kind, id, r.Ticket.Session.Court.VenueId, $"Khoản hoàn #{id}", r.Status ?? "requested", r.Status is "completed" or "rejected");
        }

        if (kind == "reviews")
        {
            var p = await db.Payments.AsNoTracking().Include(p => p.Order).ThenInclude(o => o!.Tickets).ThenInclude(t => t.Session).ThenInclude(s => s.Court).Include(p => p.BankTransactions).SingleOrDefaultAsync(p => p.Id == id);
            if (p == null)
                return null;
            long venue;
            if (p.Order != null)
            {
                var centers = p.Order.Tickets.Select(t => t.Session.Court.VenueId).Distinct().ToArray();
                if (centers.Length != 1)
                    return null;
                venue = centers[0];
            }
            else if (p.GoodsOrderId is long goods)
                venue = await db.Database.SqlQueryRaw<long>("SELECT venue_id AS Value FROM dbo.orders WHERE id={0}", goods).SingleAsync();
            else
                return null;
            var settled = p.Status == "paid" && !p.BankTransactions.Any(b => b.MatchStatus == "review_required") || (p.Status is "failed" or "cancelled" or "expired" && !p.BankTransactions.Any(b => b.AmountVnd > 0 && b.MatchStatus != "refunded"));
            return new(kind, id, venue, p.ReferenceCode ?? $"Thanh toán #{id}", p.Status, settled);
        }

        return null;
    }

    public async Task<IncidentState> State(string kind, long id)
    {
        var row = await db.AuditLogs.AsNoTracking().Where(a => a.EntityType == "Issue:" + kind && a.EntityId == id).OrderByDescending(a => a.Id).FirstOrDefaultAsync();
        var state = row == null ? new IncidentState() : JsonSerializer.Deserialize<IncidentState>(row.NewValue!)!with
        {
            Version = row.Id
        };
        if (kind == "inbox" && state.Status == "resolved")
        {
            var message = await db.TicketSupportMessages.AsNoTracking().Where(m => m.TicketId == id && !m.IsStaff && m.CreatedAt > state.At).OrderByDescending(m => m.Id).FirstOrDefaultAsync();
            if (message != null)
                state = state with
                {
                    Status = "open",
                    AssignedTo = null,
                    AssignedName = null,
                    Note = "Người chơi gửi thông tin mới; cần tiếp tục xử lý.",
                    At = message.CreatedAt,
                    UpdatedBy = message.SenderId
                };
        }

        return state;
    }

    public async Task<IncidentState> SaveState(string kind, long id, long actor, IncidentState state)
    {
        var log = new AuditLog
        {
            ActorId = actor,
            EntityType = "Issue:" + kind,
            EntityId = id,
            Action = "issue_" + state.Status,
            NewValue = JsonSerializer.Serialize(state)
        };
        db.AuditLogs.Add(log);
        if (state.AssignedTo is long assignee && assignee != actor)
            db.Notifications.Add(new Notification { UserId = assignee, Channel = "web", ReferenceType = "issue_assigned", ReferenceId = id, Title = $"Bạn được phân công xử lý sự cố {kind} #{id}. Mở Sự cố để xem việc chờ.", Status = "unread" });
        await db.SaveChangesAsync();
        return state with
        {
            Version = log.Id
        };
    }

    public async Task<IReadOnlyList<(string Kind, long Id)>> Keys()
    {
        var rows = await db.AuditLogs.AsNoTracking().Where(a => a.EntityType != null && a.EntityType.StartsWith("Issue:") && a.EntityId != null).Select(a => new { a.EntityType, a.EntityId }).Distinct().ToListAsync();
        return rows.Select(a => (a.EntityType![6..], a.EntityId!.Value)).ToList();
    }

    public async Task<IReadOnlyList<AuditLog>> History(string type, long id) => await db.AuditLogs.AsNoTracking().Where(a => a.EntityType == type && a.EntityId == id).OrderBy(a => a.Id).ToListAsync();
    public async Task<OperationalCase> CreateCase(OperationalCase item)
    {
        var log = new AuditLog
        {
            ActorId = item.RequestedBy,
            EntityType = "OperationalRequest",
            Action = "operational_requested",
            NewValue = JsonSerializer.Serialize(item)
        };
        db.AuditLogs.Add(log);
        await db.SaveChangesAsync();
        var result = item with
        {
            Id = log.Id
        };
        await SaveCase(result, item.RequestedBy, "requested");
        return result;
    }

    public async Task<OperationalCase?> Case(long id)
    {
        var e = await db.AuditLogs.AsNoTracking().Where(a => a.EntityType == "OperationalException" && a.EntityId == id && a.Action != null && a.Action!.StartsWith("operational_")).OrderByDescending(a => a.Id).FirstOrDefaultAsync();
        return e == null ? null : JsonSerializer.Deserialize<OperationalCase>(e.NewValue!);
    }

    public async Task<IReadOnlyList<OperationalCase>> Cases()
    {
        var ids = await db.AuditLogs.AsNoTracking().Where(a => a.EntityType == "OperationalException" && a.EntityId != null && a.Action != null && a.Action!.StartsWith("operational_")).Select(a => a.EntityId!.Value).Distinct().ToListAsync();
        var result = new List<OperationalCase>();
        foreach (var id in ids)
        {
            var item = await Case(id);
            if (item != null)
                result.Add(item);
        }

        return result.OrderByDescending(i => i.Id).ToList();
    }

    public async Task<OperationalCase> SaveCase(OperationalCase item, long actor, string action)
    {
        if (actor != item.RequestedBy)
            db.Notifications.Add(new Notification { UserId = item.RequestedBy, Channel = "web", ReferenceType = "operational_exception", ReferenceId = item.Id, Title = $"Ngoại lệ #{item.Id}: {item.Status}. Mở Ngoại lệ vận hành để xem quyết định và việc chờ.", Status = "unread" });
        db.AuditLogs.Add(new AuditLog { ActorId = actor, EntityType = "OperationalException", EntityId = item.Id, Action = "operational_" + action, NewValue = JsonSerializer.Serialize(item) });
        await db.SaveChangesAsync();
        return item;
    }

    public async Task<IReadOnlyList<PaymentRecoveryItem>> RecoveryPayments()
    {
        var payments = await db.Payments.AsNoTracking().Include(p => p.BankTransactions).Where(p => p.OrderId != null && (p.Status != "paid" || p.BankTransactions.Any(b => b.MatchStatus == "review_required"))).OrderByDescending(p => p.Id).ToListAsync();
        var ids = payments.Select(p => p.Id).ToArray();
        var history = await db.AuditLogs.AsNoTracking().Where(a => a.EntityType == "Payment" && a.EntityId != null && ids.Contains(a.EntityId.Value)).OrderByDescending(a => a.Id).ToListAsync();
        return payments.Select(p => new PaymentRecoveryItem(p.Id, "social", p.OrderId, p.ReferenceCode, p.AmountVnd, p.Status, p.BankTransactions.Select(b => new RecoveryBank(b.Id, b.ExternalId, b.AmountVnd, b.MatchStatus)).ToList(), history.Where(h => h.EntityId == p.Id).Select(h => new RecoveryHistory(h.ActorId, h.Action, h.NewValue)).ToList())).ToList();
    }

    public Task<Payment?> RecoveryPayment(long id) => db.Payments.AsNoTracking().Include(p => p.BankTransactions).Include(p => p.Order).ThenInclude(o => o!.Tickets).SingleOrDefaultAsync(p => p.Id == id && p.OrderId != null);
    public async Task RecordRecovery(long id, long actor, string action, string evidence, string result)
    {
        db.ChangeTracker.Clear();
        db.AuditLogs.Add(new AuditLog { ActorId = actor, EntityType = "Payment", EntityId = id, Action = action, NewValue = JsonSerializer.Serialize(new { evidence, result, at = DateTimeOffset.UtcNow }) });
        await db.SaveChangesAsync();
    }

    public async Task CloseUnpaid(long id)
    {
        await Transaction(async () =>
        {
            var p = await db.Payments.Include(p => p.BankTransactions).Include(p => p.Order).ThenInclude(o => o!.Tickets).SingleAsync(p => p.Id == id);
            if (p.Status == "paid" || p.BankTransactions.Any(b => b.AmountVnd > 0) || p.Order!.Tickets.Any(t => t.Status == "confirmed" || t.CheckedInBy != null))
                throw new PaymentException(409, "Đã có tiền hoặc vé hợp lệ; cần đối soát, không đóng như chưa thanh toán.");
            p.Status = "failed";
            p.Order.Status = "cancelled";
            foreach (var t in p.Order.Tickets.Where(t => t.Status is "pending" or "reserved"))
                t.Status = "cancelled";
            return true;
        });
    }

    public async Task<RefundPayoutRecord?> ReturnPayout(long caseId)
    {
        var log = await db.AuditLogs.AsNoTracking().Where(a => a.EntityType == "OperationalException" && a.EntityId == caseId && a.Action == "return_payout").OrderByDescending(a => a.Id).FirstOrDefaultAsync();
        return log == null ? null : JsonSerializer.Deserialize<RefundPayoutRecord>(log.NewValue!);
    }

    public async Task RecordReturnPayout(long caseId, long actor, RefundPayoutInput input)
    {
        var value = new RefundPayoutRecord(input.AmountVnd, input.RecipientId, input.BankReference.Trim().ToUpperInvariant(), input.Document.Trim(), input.PaidAt, actor);
        var existing = await ReturnPayout(caseId);
        if (existing != null)
        {
            if (existing != value)
                throw new PaymentException(409, "Đã có chứng từ tiền ra; xác minh chứng từ đã lưu, không chuyển lại.");
            return;
        }

        if (await db.AuditLogs.AnyAsync(a => (a.Action == "refund_transfer_reference" || a.Action == "return_transfer_reference") && a.NewValue == value.BankReference) || await db.BankTransactions.AnyAsync(b => b.ExternalId == value.BankReference || b.ExternalId == "payos:" + value.BankReference))
            throw new PaymentException(409, "Mã chứng từ đã sử dụng hoặc là tiền vào.");
        db.AuditLogs.AddRange(new AuditLog { ActorId = actor, EntityType = "OperationalException", EntityId = caseId, Action = "return_transfer_reference", NewValue = value.BankReference }, new AuditLog { ActorId = actor, EntityType = "OperationalException", EntityId = caseId, Action = "return_payout", NewValue = JsonSerializer.Serialize(value) });
        await db.SaveChangesAsync();
    }

    public async Task CompleteReturn(OperationalCase item, long actor)
    {
        var p = await db.Payments.Include(p => p.BankTransactions).Include(p => p.Order).ThenInclude(o => o!.Tickets).SingleAsync(p => p.Id == item.SubjectId);
        var bank = p.BankTransactions.Single(b => b.Id == item.BankTransactionId);
        if (bank.MatchStatus != "review_required" || bank.AmountVnd != item.AmountVnd)
            throw new PaymentException(409, "Khoản thu đã thay đổi.");
        bank.MatchStatus = "refunded";
        if (p.Order!.Status != "paid")
        {
            if (p.Order.Tickets.Any(t => t.Status == "confirmed" || t.CheckedInBy != null))
                throw new PaymentException(409, "Đã có vé sử dụng khoản này; cần quy trình hoàn vé.");
            foreach (var t in p.Order.Tickets.Where(t => t.Status is "pending" or "reserved"))
                t.Status = "cancelled";
            p.Order.Status = "cancelled";
        }

        if (!p.BankTransactions.Any(b => b.MatchStatus == "review_required"))
            p.Status = p.Order.Status == "paid" ? "paid" : "failed";
        if (p.PayerId is long payer)
            db.Notifications.Add(new Notification { UserId = payer, Channel = "web", ReferenceType = "operational_exception", ReferenceId = item.Id, Title = $"Đã xác minh hoàn khoản thu chưa phân bổ {item.AmountVnd} VND. Vé chưa cấp đã hủy; có thể đặt lại theo chỗ còn trống.", Status = "unread" });
    }

    public async Task ExecuteStock(OperationalCase item, long actor)
    {
        var product = await Resource("products", item.ProductId!.Value);
        var source = await Resource(item.Kind, item.SubjectId);
        if (product == null || source == null || product.VenueId != source.VenueId)
            throw new PaymentException(404, "Mặt hàng không thuộc cơ sở của hồ sơ.");
        if (await db.StockMovements.AnyAsync(m => m.ReferenceType == "OperationalException" && m.ReferenceId == item.Id))
            throw new PaymentException(409, "Đã có bút toán điều chỉnh; tải lại hồ sơ.");
        var stock = await db.StockItems.FromSqlInterpolated($"SELECT * FROM dbo.stock_items WITH (UPDLOCK,HOLDLOCK) WHERE product_id={item.ProductId.Value}").SingleOrDefaultAsync() ?? throw new PaymentException(409, "Mặt hàng chưa có thẻ kho.");
        var delta = item.QuantityDelta ?? 0;
        var balance = (long)stock.QuantityOnHand + delta;
        if (delta == 0 || balance < 0 || balance > int.MaxValue)
            throw new PaymentException(409, "Điều chỉnh làm tồn kho âm hoặc vượt giới hạn.");
        stock.QuantityOnHand = (int)balance;
        db.StockMovements.Add(new StockMovement { StockItemId = stock.Id, MovementType = delta > 0 ? "Receipt" : "Issue", Quantity = delta, ReferenceType = "OperationalException", ReferenceId = item.Id, CreatedBy = actor });
    }

    public Task AddApproval(Approval approval)
    {
        db.Approvals.Add(approval);
        return Task.CompletedTask;
    }
}
