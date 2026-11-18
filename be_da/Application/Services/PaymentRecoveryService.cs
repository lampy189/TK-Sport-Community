using TKSC.Application.DTOs;
using TKSC.Application.Interfaces.External;
using TKSC.Application.Interfaces.Repositories;
using TKSC.Application.Interfaces.Services;
using TKSC.Domain.Entities;
using TKSC.Domain.Exceptions;

namespace TKSC.Application.Services;
public sealed class PaymentRecoveryService(IPaymentRecoveryRepository repository, IPaymentGateway gateway,
    IPaymentService social, GoodsPaymentService goods, TicketAftercareService aftercare) : IPaymentRecoveryService
{
    public Task<IReadOnlyList<PaymentRecoveryItem>> ListAsync(CancellationToken ct) => repository.ListAsync(ct);
    public async Task<object> ActAsync(long id,long actor,string action,PaymentRecoveryInput input,CancellationToken ct)
    {
        var evidence=TicketAftercareRules.Evidence(input.Evidence);
        var p=await repository.FindAsync(id,ct) ?? throw new PaymentException(404,"Không tìm thấy giao dịch tại cơ sở của bạn.");
        if (actor<=0 || actor==p.PayerId || actor==p.Order?.PayerId || actor==p.GoodsOrder?.BuyerId)
            throw new PaymentException(403,"Không tự xử lý giao dịch của mình.");
        if (action is not ("recheck" or "approve" or "close-unpaid" or "note")) throw new PaymentException(400,"Quyết định không hợp lệ.");
        await repository.RecordAsync(id,actor,"payment_recovery_requested",evidence,new { action, input.BankTransactionId },ct);
        try
        {
            if (action=="note")
            {
                await repository.RecordAsync(id,actor,"payment_recovery_note",evidence,new { status=p.Status },ct);
                return new { status=p.Status };
            }
            if (action=="recheck")
            {
                if(p.GoodsOrderId.HasValue) await goods.RecheckAsync(id,ct); else await social.RecheckAsync(id);
            }
            else
            {
                // A staff decision never substitutes for verified banking evidence.
                var remote=await gateway.GetAsync(id);
                if(action=="close-unpaid") await repository.CloseUnpaidAsync(id,actor,evidence,remote,ct);
                else
                {
                    var bank=p.BankTransactions.SingleOrDefault(b=>b.Id==input.BankTransactionId);
                    var verified=remote.Transactions?.SingleOrDefault(t=>"payos:"+t.Reference==bank?.ExternalId);
                    if(bank==null || verified==null || !verified.Success || verified.Currency!="VND" || verified.OrderCode!=p.Id ||
                        verified.LinkId!=remote.LinkId || remote.OrderCode!=p.Id || remote.Status!="PAID" || remote.AmountPaid!=p.AmountVnd || remote.Transactions?.Sum(t=>t.Amount)!=remote.AmountPaid ||
                        verified.Amount!=p.AmountVnd || bank.AmountVnd!=p.AmountVnd || (p.GatewayPaymentLinkId!=null && p.GatewayPaymentLinkId!=remote.LinkId))
                        throw new PaymentException(409,"Bằng chứng ngân hàng chưa xác nhận đúng một khoản đủ tiền. Không tự duyệt thiếu/thừa.");
                    if(p.GoodsOrderId.HasValue) await goods.ApproveReviewAsync(id,bank.Id,actor,evidence,ct);
                    else await aftercare.ApprovePaymentAsync(id,bank.Id,actor,evidence);
                }
            }
            var current=await repository.FindAsync(id,ct);
            await repository.RecordAsync(id,actor,"payment_recovery_completed",evidence,new { action, status=current?.Status },ct);
            return new { status=current?.Status };
        }
        catch(Exception ex)
        {
            await repository.RecordAsync(id,actor,"payment_recovery_failed",evidence,new { action, reason=ex is PaymentException or GatewayException ? ex.Message : "Chưa xác định kết quả; kiểm tra lại giao dịch." },CancellationToken.None);
            throw;
        }
    }
}
