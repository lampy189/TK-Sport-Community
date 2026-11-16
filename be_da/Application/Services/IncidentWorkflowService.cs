using TKSC.Application.DTOs;
using TKSC.Application.Interfaces.Repositories;
using TKSC.Domain.Entities;
using TKSC.Domain.Exceptions;

namespace TKSC.Application.Services;
public sealed class IncidentWorkflowService(IIncidentRepository repository, TimeProvider clock, TKSC.Application.Interfaces.External.IPaymentGateway? gateway = null) : TKSC.Application.Interfaces.Services.IIncidentWorkflowService
{
    private async Task<IncidentResource> Scope(IncidentActor actor, string kind, long id)
    {
        var resource = await repository.Resource(kind, id);
        if (resource == null || actor.Role != "owner" && actor.VenueId != resource.VenueId)
            throw new PaymentException(404, "Không tìm thấy sự cố tại cơ sở của bạn.");
        return resource;
    }

    public async Task<object> States(long actor)
    {
        var a = await repository.Actor(actor);
        var rows = new List<object>();
        foreach (var(kind, id)in await repository.Keys())
        {
            try
            {
                await Scope(a, kind, id);
                rows.Add(new { key = $"{kind}-{id}", state = await repository.State(kind, id) });
            }
            catch (PaymentException e)when (e.StatusCode == 404)
            {
            }
        }

        return rows;
    }

    public async Task<object> Detail(long actor, string kind, long id)
    {
        var a = await repository.Actor(actor);
        var r = await Scope(a, kind, id);
        return new
        {
            resource = r,
            state = await repository.State(kind, id),
            history = kind == "operations" ? (await repository.History("Issue:" + kind, id)).Concat(await repository.History("OperationalException", id)).OrderBy(h => h.Id).ToList() : await repository.History("Issue:" + kind, id),
            people = await repository.People(r.VenueId)
        };
    }

    public Task<object> Act(long actor, string kind, long id, IncidentAction input) => repository.Transaction<object>(async () =>
    {
        var a = await repository.Actor(actor);
        var resource = await Scope(a, kind, id);
        var old = await repository.State(kind, id);
        var reason = TicketAftercareRules.Evidence(input.Reason);
        if (old.Version != input.ExpectedVersion)
            throw new PaymentException(409, "Sự cố đã được người khác cập nhật; tải lại trước khi xử lý.");
        var manager = a.Role is "manager" or "owner";
        var next = old with
        {
            Note = reason,
            UpdatedBy = actor,
            At = clock.GetUtcNow()
        };
        if (input.Action == "claim")
        {
            if (old.Status == "resolved")
                throw new PaymentException(409, "Mở lại sự cố trước khi nhận xử lý.");
            if (old.AssignedTo != null && old.AssignedTo != actor && !manager)
                throw new PaymentException(409, "Sự cố đang do người khác xử lý.");
            var assignee = await repository.Actor(input.AssigneeId ?? actor);
            if (assignee.Id != actor && !manager)
                throw new PaymentException(403, "Chỉ Manager/Owner được phân công người khác.");
            await Scope(assignee, kind, id);
            next = next with
            {
                AssignedTo = assignee.Id,
                AssignedName = assignee.Name,
                Status = "in_progress"
            };
        }
        else if (input.Action == "reopen")
        {
            if (old.Status != "resolved" || !manager)
                throw new PaymentException(403, "Manager/Owner mở lại sự cố đã đóng.");
            next = next with
            {
                Status = "open",
                AssignedTo = null,
                AssignedName = null
            };
        }
        else
        {
            if (old.Status == "resolved" || old.AssignedTo == null || old.AssignedTo != actor && !manager)
                throw new PaymentException(409, "Nhận xử lý trước và chỉ cập nhật sự cố được giao.");
            var status = input.Action switch
            {
                "wait_bank" => "waiting_bank",
                "wait_approval" => "waiting_approval",
                "resolve" => "resolved",
                _ => throw new PaymentException(400, "Trạng thái xử lý không hợp lệ.")};
            if (status == "resolved" && !resource.CanResolve)
                throw new PaymentException(409, "Nghiệp vụ còn chờ đối soát/đổi/hoàn; không đóng sự cố để che khoản chưa xử lý.");
            next = next with
            {
                Status = status
            };
        }

        return await repository.SaveState(kind, id, actor, next);
    });
    public async Task<object> Cases(long actor)
    {
        var a = await repository.Actor(actor);
        var result = new List<object>();
        foreach (var c in await repository.Cases())
        {
            try
            {
                var r = await Scope(a, c.Kind, c.SubjectId);
                result.Add(new { item = c, resource = r, payout = await repository.ReturnPayout(c.Id) });
            }
            catch (PaymentException e)when (e.StatusCode == 404)
            {
            }
        }

        return result;
    }

    public Task<object> Request(long actor, OperationalRequest input) => repository.Transaction<object>(async () =>
    {
        var a = await repository.Actor(actor);
        if (!new[]
        {
            "inbox",
            "reviews",
            "refunds",
            "sessions",
            "venues"
        }.Contains(input.Kind))
            throw new PaymentException(400, "Đối tượng ngoại lệ không hợp lệ.");
        if (input.Kind == "venues" && input.SubjectId == 0)
            input = input with
            {
                SubjectId = a.VenueId ?? throw new PaymentException(403, "Tài khoản chưa được gán cơ sở.")
            };
        await Scope(a, input.Kind, input.SubjectId);
        if (a.Role == "owner")
            throw new PaymentException(403, "Owner quyết định yêu cầu của nhân sự, không tự yêu cầu rồi tự duyệt.");
        var reason = TicketAftercareRules.Evidence(input.Reason);
        if (!new[]
        {
            "payment_mismatch",
            "funding",
            "refund_funds",
            "stock_adjustment",
            "return_unallocated",
            "court",
            "other"
        }.Contains(input.Category))
            throw new PaymentException(400, "Chọn loại ngoại lệ hợp lệ.");
        if (input.Kind != "venues" && (await repository.Cases()).Any(c => c.Kind == input.Kind && c.SubjectId == input.SubjectId && (input.Category != "return_unallocated" || c.BankTransactionId == input.BankTransactionId) && c.Status is "submitted" or "approved" or "executed"))
            throw new PaymentException(409, "Đã có yêu cầu còn chờ xử lý cho sự cố này.");
        if (input.Category == "return_unallocated")
        {
            if (input.Kind != "reviews" || input.BankTransactionId == null)
                throw new PaymentException(400, "Chọn giao dịch ngân hàng đã ghi nhận để hoàn toàn bộ khoản chưa phân bổ.");
            var payment = await repository.RecoveryPayment(input.SubjectId) ?? throw new PaymentException(404, "Không tìm thấy giao dịch vé.");
            var bank = payment.BankTransactions.SingleOrDefault(b => b.Id == input.BankTransactionId);
            if (bank == null || bank.MatchStatus != "review_required" || bank.AmountVnd <= 0 || payment.PayerId == null || payment.PayerId == actor)
                throw new PaymentException(409, "Khoản thu không còn chờ xử lý hoặc không được tự xử lý tiền của mình.");
            await VerifyIncoming(payment, bank);
            input = input with
            {
                AmountVnd = bank.AmountVnd,
                RecipientId = payment.PayerId
            };
        }
        else if (input.BankTransactionId != null || input.AmountVnd != null || input.RecipientId != null)
            throw new PaymentException(400, "Thông tin tiền chỉ dùng cho hoàn khoản thu chưa phân bổ.");
        if (input.Category == "stock_adjustment")
        {
            if (input.ProductId is not > 0 || input.QuantityDelta is null or 0 or < -100000 or > 100000 || input.Kind != "venues")
                throw new PaymentException(400, "Chọn mặt hàng và mức điều chỉnh khác 0 trong giới hạn 100.000 đơn vị cơ sở.");
            var product = await Scope(a, "products", input.ProductId.Value);
            if (product.VenueId != input.SubjectId)
                throw new PaymentException(409, "Mặt hàng không thuộc cơ sở của hồ sơ.");
        }
        else if (input.ProductId != null || input.QuantityDelta != null)
            throw new PaymentException(400, "Thông tin kho chỉ dùng cho điều chỉnh kho.");
        return await repository.CreateCase(new(0, input.Kind, input.SubjectId, input.Category, reason, actor, a.Role, "submitted", RequestedAt: clock.GetUtcNow(), UpdatedAt: clock.GetUtcNow(), ProductId: input.ProductId, QuantityDelta: input.QuantityDelta, BankTransactionId: input.BankTransactionId, AmountVnd: input.AmountVnd, RecipientId: input.RecipientId));
    });
    public Task<object> Decide(long actor, long id, OperationalDecision input) => repository.Transaction<object>(async () =>
    {
        var a = await repository.Actor(actor);
        var c = await repository.Case(id) ?? throw new PaymentException(404, "Không tìm thấy yêu cầu.");
        await Scope(a, c.Kind, c.SubjectId);
        if (a.Role == "staff" || actor == c.RequestedBy || actor == c.RecipientId || c.RequesterRole == "manager" && a.Role != "owner")
            throw new PaymentException(403, "Cần người duyệt độc lập; yêu cầu của Manager phải do Owner quyết định.");
        if (c.Status != "submitted")
            throw new PaymentException(409, "Yêu cầu đã được quyết định.");
        var note = TicketAftercareRules.Evidence(input.Reason);
        var next = c with
        {
            Status = input.Approve ? "approved" : "rejected",
            DecidedBy = actor,
            Decision = note,
            UpdatedAt = clock.GetUtcNow()
        };
        await repository.AddApproval(new Approval { EntityType = "OperationalException", EntityId = id, RequestedBy = c.RequestedBy, ApprovedBy = actor, Status = next.Status });
        return await repository.SaveCase(next, actor, next.Status);
    });
    private async Task VerifyIncoming(Payment payment, BankTransaction bank)
    {
        if (gateway == null || payment.Method != "payos")
            throw new PaymentException(409, "Chưa có đối soát ngân hàng tự động cho nguồn này; chuyển Owner kiểm tra.");
        var remote = await gateway.GetAsync(payment.Id);
        var transfer = remote.Transactions?.SingleOrDefault(t => "payos:" + t.Reference == bank.ExternalId);
        if (remote.OrderCode != payment.Id || remote.Status != "PAID" || remote.LinkId != payment.GatewayPaymentLinkId || transfer == null || !transfer.Success || transfer.Currency != "VND" || transfer.OrderCode != payment.Id || transfer.LinkId != remote.LinkId || transfer.Amount != bank.AmountVnd || (remote.Transactions?.Sum(t => t.Amount) ?? 0) != remote.AmountPaid)
            throw new PaymentException(409, "Chưa xác minh khoản tiền thật đúng giao dịch ngân hàng; tiếp tục chờ đối soát.");
        if (payment.Order!.Status == "paid" && payment.BankTransactions.Where(b => b.Id != bank.Id && b.MatchStatus == "matched").Sum(b => (long)b.AmountVnd) < payment.Order.TotalAmount)
            throw new PaymentException(409, "Khoản này đang bảo đảm vé đã cấp; xử lý theo hoàn vé, không hoàn tiền chưa phân bổ.");
    }

    public Task<object> RecordReturn(long actor, long id, RefundPayoutInput input) => repository.Transaction<object>(async () =>
    {
        var a = await repository.Actor(actor);
        var c = await repository.Case(id) ?? throw new PaymentException(404, "Không tìm thấy hồ sơ.");
        await Scope(a, c.Kind, c.SubjectId);
        if (c.Category != "return_unallocated" || c.Status != "approved" || actor == c.RecipientId)
            throw new PaymentException(409, "Hồ sơ chưa duyệt hoặc người chi không hợp lệ.");
        if (input.AmountVnd != c.AmountVnd || input.RecipientId != c.RecipientId || input.PaidAt <= DateTimeOffset.UnixEpoch || input.PaidAt > clock.GetUtcNow())
            throw new PaymentException(400, "Chứng từ phải đúng người trả gốc, toàn bộ khoản thu và thời điểm hợp lệ.");
        TicketAftercareRules.Evidence(input.Document);
        if (input.BankReference?.Trim()is not { Length: >= 6 and <= 100 })
            throw new PaymentException(400, "Nhập mã giao dịch tiền ra từ 6–100 ký tự.");
        var payment = await repository.RecoveryPayment(c.SubjectId) ?? throw new PaymentException(404, "Không tìm thấy thanh toán.");
        var bank = payment.BankTransactions.SingleOrDefault(b => b.Id == c.BankTransactionId);
        if (bank == null || bank.MatchStatus != "review_required" || bank.AmountVnd != c.AmountVnd || payment.PayerId != c.RecipientId)
            throw new PaymentException(409, "Nguồn tiền/người nhận đã thay đổi; tiếp tục đối soát trước khi chi.");
        await VerifyIncoming(payment, bank);
        await repository.RecordReturnPayout(id, actor, input);
        return new
        {
            status = c.Status,
            verificationRequired = true
        };
    });
    public Task<object> VerifyReturn(long actor, long id, string reason) => repository.Transaction<object>(async () =>
    {
        var a = await repository.Actor(actor);
        var c = await repository.Case(id) ?? throw new PaymentException(404, "Không tìm thấy hồ sơ.");
        await Scope(a, c.Kind, c.SubjectId);
        var note = TicketAftercareRules.Evidence(reason);
        var payout = await repository.ReturnPayout(id);
        if (a.Role is not ("manager" or "owner") || actor == c.RecipientId || actor == payout?.RecordedBy || payout == null)
            throw new PaymentException(403, "Cần Manager/Owner độc lập với người chi và người nhận xác minh chứng từ.");
        if (c.Category != "return_unallocated" || payout.AmountVnd != c.AmountVnd || payout.RecipientId != c.RecipientId)
            throw new PaymentException(409, "Chứng từ không khớp phạm vi được duyệt.");
        if (c.Status is "executed" or "closed")
            return c;
        if (c.Status != "approved")
            throw new PaymentException(409, "Hồ sơ chưa duyệt.");
        var p = await repository.RecoveryPayment(c.SubjectId) ?? throw new PaymentException(404, "Không tìm thấy thanh toán.");
        var bank = p.BankTransactions.SingleOrDefault(b => b.Id == c.BankTransactionId);
        if (bank == null || bank.MatchStatus != "review_required" || bank.AmountVnd != c.AmountVnd)
            throw new PaymentException(409, "Khoản thu đã thay đổi; tiếp tục đối soát.");
        await VerifyIncoming(p, bank);
        await repository.CompleteReturn(c, actor);
        return await repository.SaveCase(c with { Status = "executed", Decision = c.Decision + " | Xác minh hoàn tiền: " + note, UpdatedAt = clock.GetUtcNow() }, actor, "return_verified");
    });
    public Task<object> Execute(long actor, long id, string reason) => repository.Transaction<object>(async () =>
    {
        var a = await repository.Actor(actor);
        var c = await repository.Case(id) ?? throw new PaymentException(404, "Không tìm thấy hồ sơ.");
        await Scope(a, c.Kind, c.SubjectId);
        var note = TicketAftercareRules.Evidence(reason);
        if (c.Category != "stock_adjustment" || c.ProductId == null)
            throw new PaymentException(409, "Thực hiện nghiệp vụ tiền tại giao dịch/hoàn tiền nguồn.");
        if (c.Status is "executed" or "closed")
            return c;
        if (c.Status != "approved" || c.DecidedBy == null)
            throw new PaymentException(409, "Chưa có phê duyệt độc lập.");
        await Scope(a, "products", c.ProductId.Value);
        await repository.ExecuteStock(c, actor);
        return await repository.SaveCase(c with { Status = "executed", UpdatedAt = clock.GetUtcNow(), Decision = c.Decision + " | Thực hiện: " + note }, actor, "executed");
    });
    public Task<object> Close(long actor, long id, string reason) => repository.Transaction<object>(async () =>
    {
        var a = await repository.Actor(actor);
        var c = await repository.Case(id) ?? throw new PaymentException(404, "Không tìm thấy yêu cầu.");
        var r = await Scope(a, c.Kind, c.SubjectId);
        if ((c.Category is "stock_adjustment" or "return_unallocated" ? c.Status != "executed" : c.Status != "approved") || !r.CanResolve && c.Category != "return_unallocated")
            throw new PaymentException(409, "Chưa có quyết định hoặc nghiệp vụ nguồn chưa hoàn tất; tiếp tục theo dõi việc chờ.");
        if ((c.Kind == "venues" || c.Category is "payment_mismatch" or "funding" or "refund_funds") && (a.Role == "staff" || actor == c.RequestedBy))
            throw new PaymentException(403, "Cần Manager/Owner độc lập xác nhận kết quả ngoại lệ tài chính.");
        var note = TicketAftercareRules.Evidence(reason);
        return await repository.SaveCase(c with { Status = "closed", UpdatedAt = clock.GetUtcNow(), Decision = (c.Decision ?? "") + " | Kết quả: " + note }, actor, "closed");
    });
}
