using TKSC.Application.DTOs;
using TKSC.Application.Interfaces.Repositories;
using TKSC.Application.Interfaces.Services;
using TKSC.Application.Interfaces.External;
using be_da.Application.Interfaces.Services;
using TKSC.Domain.Exceptions;

namespace TKSC.Application.Services;
public sealed class TicketRecoveryService(IIncidentRepository repository, IPaymentGateway gateway, IPaymentService payments, ITicketAftercareService aftercare) : ITicketRecoveryService
{
    public async Task<object> List(long actor)
    {
        var a = await repository.Actor(actor);
        var list = new List<PaymentRecoveryItem>();
        foreach (var p in await repository.RecoveryPayments())
        {
            var r = await repository.Resource("reviews", p.Id);
            if (r != null && (a.Role == "owner" || a.VenueId == r.VenueId))
                list.Add(p);
        }

        return list;
    }

    public async Task<object> Act(long actor, long id, string action, PaymentRecoveryInput input)
    {
        var a = await repository.Actor(actor);
        var r = await repository.Resource("reviews", id);
        if (r == null || a.Role != "owner" && a.VenueId != r.VenueId)
            throw new PaymentException(404, "Không tìm thấy giao dịch tại cơ sở.");
        var p = await repository.RecoveryPayment(id) ?? throw new PaymentException(404, "Không tìm thấy giao dịch vé.");
        if (p.PayerId == actor || p.Order?.PayerId == actor)
            throw new PaymentException(403, "Không tự đối soát thanh toán của mình.");
        var evidence = TicketAftercareRules.Evidence(input.Evidence);
        if (!new[]
        {
            "recheck",
            "approve",
            "close-unpaid",
            "note"
        }.Contains(action))
            throw new PaymentException(400, "Quyết định không hợp lệ.");
        var state = await repository.State("reviews", id);
        if (action != "note" && (state.AssignedTo == null || state.AssignedTo != actor && a.Role == "staff"))
            throw new PaymentException(409, "Nhận xử lý sự cố trước khi thực hiện đối soát.");
        try
        {
            if (action == "note")
            {
                await repository.RecordRecovery(id, actor, "payment_recovery_note", evidence, p.Status);
                return new
                {
                    status = p.Status
                };
            }

            if (!string.Equals(p.Method, "payos", StringComparison.OrdinalIgnoreCase))
                throw new PaymentException(409, "Không có chứng từ payOS cho giao dịch này; gửi ngoại lệ đối soát với bằng chứng ngân hàng.");
            if (action == "recheck")
                await payments.RecheckAsync(id);
            else
            {
                var remote = await gateway.GetAsync(id);
                if (remote.OrderCode != id || p.GatewayPaymentLinkId != null && p.GatewayPaymentLinkId != remote.LinkId)
                    throw new PaymentException(409, "Chứng từ không khớp mã giao dịch/link thanh toán.");
                if (action == "close-unpaid")
                {
                    if (remote.AmountPaid != 0 || (remote.Transactions?.Count ?? 0) != 0 || remote.Status is not ("CANCELLED" or "EXPIRED" or "FAILED"))
                        throw new PaymentException(409, "Ngân hàng chưa xác nhận kết thúc không thu tiền; tiếp tục chờ đối soát.");
                    await repository.CloseUnpaid(id);
                }
                else
                {
                    var bank = p.BankTransactions.SingleOrDefault(b => b.Id == input.BankTransactionId);
                    var transfer = remote.Transactions?.SingleOrDefault(t => "payos:" + t.Reference == bank?.ExternalId);
                    if (bank == null || transfer == null || !transfer.Success || transfer.Currency != "VND" || transfer.OrderCode != id || transfer.LinkId != remote.LinkId || remote.Status != "PAID" || remote.AmountPaid != p.AmountVnd || remote.Transactions?.Sum(t => t.Amount) != remote.AmountPaid || transfer.Amount != p.AmountVnd || bank.AmountVnd != p.AmountVnd)
                        throw new PaymentException(409, "Chưa xác nhận đủ tiền đúng nguồn; gửi ngoại lệ nếu thiếu/thừa hoặc chưa thể cấp vé.");
                    await aftercare.ApprovePaymentAsync(id, bank.Id, actor, evidence);
                }
            }

            var current = await repository.RecoveryPayment(id);
            await repository.RecordRecovery(id, actor, "payment_recovery_completed", evidence, current?.Status ?? "unknown");
            return new
            {
                status = current?.Status
            };
        }
        catch (Exception e)
        {
            await repository.RecordRecovery(id, actor, "payment_recovery_failed", evidence, e is PaymentException or GatewayException ? e.Message : "Chưa xác định kết quả; chờ ngân hàng, không thanh toán lại.");
            throw;
        }
    }
}
