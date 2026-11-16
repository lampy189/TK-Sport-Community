using TKSC.Application.DTOs;
using TKSC.Domain.Exceptions;
namespace TKSC.Application.Services;
public sealed partial class GoodsPaymentService
{
    public async Task RecheckAsync(long paymentId, CancellationToken ct)
    {
        var payment=await repository.RecoveryPaymentAsync(paymentId,ct) ?? throw Error(404,"Không tìm thấy giao dịch hàng hóa.");
        var remote=await gateway.GetAsync(paymentId);
        if (remote.OrderCode!=paymentId || (remote.Transactions?.Sum(t=>t.Amount)??0)!=remote.AmountPaid ||
            (payment.GatewayPaymentLinkId!=null && remote.LinkId!=payment.GatewayPaymentLinkId))
            throw new GatewayException("Chi tiết payOS chưa khớp giao dịch.");
        foreach(var transfer in remote.Transactions??[]) await ApplyAsync(transfer,remote,ct);
    }
    public Task<GoodsOrderView> ApproveReviewAsync(long paymentId,long bankId,long actor,string evidence,CancellationToken ct) => repository.TransactionAsync(async ()=>
    {
        var note=TicketAftercareRules.Evidence(evidence);
        var lookup=await repository.RecoveryPaymentAsync(paymentId,ct) ?? throw Error(404,"Không tìm thấy giao dịch.");
        var order=await LoadAsync(lookup.GoodsOrderId!.Value,ct);
        if (order.BuyerId==actor || lookup.PayerId==actor) throw Error(403,"Không tự duyệt giao dịch của mình.");
        var p=order.Payments.Single(p=>p.Id==paymentId);
        var bank=await repository.RecoveryBankAsync(paymentId,bankId,ct);
        if (p.Status=="paid" && order.PaymentState=="Paid" && bank?.MatchStatus=="matched") return await ViewAsync(order,ct);
        if (p.Status!="review_required" || order.PaymentState!="NeedsReview" || order.Status=="Cancelled" || bank?.MatchStatus!="review_required" ||
            bank.AmountVnd!=p.AmountVnd || p.AmountVnd!=order.TotalAmount || order.Payments.Any(x=>x.Id!=p.Id && x.Status=="paid"))
            throw Error(409,"Chỉ duyệt giao dịch đã đối soát đủ tiền cho đơn chưa thanh toán, chưa hủy.");
        var snapshot=await SnapshotAsync(order.Id,ct);
        if (snapshot.Amount!=order.TotalAmount || !Lines(order).SequenceEqual(snapshot.Lines)) throw Error(409,"Dòng hàng đã thay đổi.");
        // Manual approval may reacquire an expired hold, but must compete for current available stock.
        await repository.ReserveAsync(order,clock.GetUtcNow().AddMinutes(15),true,ct);
        if (!await repository.CommitHoldAsync(order,ct)) throw Error(409,"Không đủ giữ tồn để duyệt.");
        p.Status="paid";p.ConfirmedBy=actor;bank.MatchStatus="matched";order.PaymentState="Paid";order.Status="Paid";
        Audit(order.Id,actor,"goods_paid_lines",snapshot.Lines);
        Audit(order.Id,actor,"goods_review_approved",new { at=clock.GetUtcNow(), bankId, evidence=note });
        repository.AddAuditLogs(new TKSC.Domain.Entities.AuditLog { EntityType="Payment",EntityId=p.Id,ActorId=actor,Action="payment_recovery_approved",NewValue=System.Text.Json.JsonSerializer.Serialize(new { at=clock.GetUtcNow(),bankId,evidence=note }) });
        await repository.SaveAsync(ct);
        return await ViewAsync(order,ct);
    },null,ct);
}
