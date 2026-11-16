using Microsoft.EntityFrameworkCore;
using TKSC.Application.DTOs;
using TKSC.Application.Interfaces.Repositories;
using TKSC.Domain.Entities;
using TKSC.Domain.Exceptions;
using TKSC.Infrastructure.Persistence.Contexts;
using System.Text.Json;

namespace TKSC.Infrastructure.Persistence.Repositories;
public sealed class PaymentRecoveryRepository(Sep490Context db, TimeProvider clock) : IPaymentRecoveryRepository
{
    public async Task<IReadOnlyList<PaymentRecoveryItem>> ListAsync(CancellationToken ct)
    {
        var payments=await db.Payments.AsNoTracking().Include(p=>p.BankTransactions)
            .Where(p=>p.Method=="payos" && (p.OrderId!=null || p.GoodsOrderId!=null) &&
                (p.Status!="paid" || p.BankTransactions.Any(b=>b.MatchStatus=="review_required")))
            .OrderByDescending(p=>p.Status=="review_required" || p.BankTransactions.Any(b=>b.MatchStatus=="review_required")).ThenByDescending(p=>p.Id).Take(150).ToListAsync(ct);
        var ids=payments.Select(p=>p.Id).ToArray();
        var logs=await db.AuditLogs.AsNoTracking().Where(a=>a.EntityType=="Payment" && a.EntityId!=null && ids.Contains(a.EntityId.Value))
            .OrderByDescending(a=>a.Id).ToListAsync(ct);
        return payments.Select(p=>new PaymentRecoveryItem(p.Id,p.GoodsOrderId.HasValue?"goods":"social",p.GoodsOrderId??p.OrderId,
            p.ReferenceCode,p.AmountVnd,p.Status,p.BankTransactions.Select(b=>new RecoveryBank(b.Id,b.ExternalId,b.AmountVnd,b.MatchStatus)).ToList(),
            logs.Where(a=>a.EntityId==p.Id).Take(20).Select(a=>new RecoveryHistory(a.ActorId,a.Action,a.NewValue)).ToList())).ToList();
    }
    public Task<Payment?> FindAsync(long id,CancellationToken ct) => db.Payments.AsNoTracking().Include(p=>p.BankTransactions)
        .Include(p=>p.Order).Include(p=>p.GoodsOrder).SingleOrDefaultAsync(p=>p.Id==id && p.Method=="payos",ct);
    public async Task RecordAsync(long id,long actor,string action,string evidence,object result,CancellationToken ct)
    {
        // A failed business transaction may leave modified tracked entities after rollback.
        // Recording the failure must never accidentally save those changes.
        db.ChangeTracker.Clear();
        db.AuditLogs.Add(new AuditLog { ActorId=actor,EntityType="Payment",EntityId=id,Action=action,
            NewValue=JsonSerializer.Serialize(new { at=clock.GetUtcNow(), evidence, result }) });
        await db.SaveChangesAsync(ct);
    }
    public async Task CloseUnpaidAsync(long id,long actor,string evidence,GatewayStatus remote,CancellationToken ct)
    {
        await new GoodsPaymentRepository(db,clock).TransactionAsync(async ()=>
        {
            var p=await db.Payments.Include(p=>p.Order).ThenInclude(o=>o!.Tickets).Include(p=>p.GoodsOrder)
                .Include(p=>p.BankTransactions).SingleOrDefaultAsync(p=>p.Id==id,ct) ?? throw new PaymentException(404,"Không tìm thấy giao dịch.");
            if (p.PayerId==actor || p.Order?.PayerId==actor || p.GoodsOrder?.BuyerId==actor) throw new PaymentException(403,"Không tự xử lý thanh toán của mình.");
            if (remote.OrderCode!=p.Id || (p.GatewayPaymentLinkId!=null && remote.LinkId!=p.GatewayPaymentLinkId) || remote.AmountPaid!=0 || remote.Transactions?.Count>0 || remote.Status is not ("CANCELLED" or "EXPIRED" or "FAILED"))
                throw new PaymentException(409,"payOS chưa xác nhận giao dịch kết thúc và không nhận tiền.");
            if (p.Status=="paid" || p.BankTransactions.Count>0 || p.Order?.Status.ToLower()=="paid" || p.GoodsOrder?.PaymentState=="Paid")
                throw new PaymentException(409,"Đã có tiền hoặc chứng từ ngân hàng; không thể đóng là chưa thu.");
            if(await db.Payments.AnyAsync(x=>x.Id!=id &&
                ((p.OrderId!=null && x.OrderId==p.OrderId) || (p.GoodsOrderId!=null && x.GoodsOrderId==p.GoodsOrderId)) &&
                (x.Status=="pending" || x.Status=="paid" || x.Status=="review_required"),ct))
                throw new PaymentException(409,"Đơn có giao dịch khác chưa kết thúc. Cần đối soát cả đơn.");
            p.Status="failed";
            if (p.GoodsOrder is { } goods)
            {
                await GoodsStockHolds.Release(db,goods.Id,ct);
                goods.Status="Cancelled";goods.PaymentState="Cancelled";
            }
            if (p.Order is { } social)
            {
                social.Status="cancelled";
                foreach(var t in social.Tickets.Where(t=>t.Status.ToLower() is "pending" or "reserved")) t.Status="cancelled";
            }
            await db.SaveChangesAsync(ct);
            await RecordAsync(id,actor,"payment_recovery_closed",evidence,new { remote.Status },ct);
            return true;
        },null,ct);
    }
}
