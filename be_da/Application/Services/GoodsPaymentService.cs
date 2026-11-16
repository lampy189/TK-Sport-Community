using TKSC.Domain.Exceptions;
using be_da.Application.Interfaces.Services;
using TKSC.Application.Interfaces.Repositories;
using TKSC.Application.DTOs.Persistence;
using System.Data;
using System.Text.Json;
using TKSC.Application.DTOs;
using TKSC.Application.Interfaces.Services;
using TKSC.Application.Interfaces.External;
using TKSC.Domain.Entities;
using TKSC.Application.Validators;

namespace TKSC.Application.Services;
public sealed partial class GoodsPaymentService(IGoodsPaymentRepository repository, IPaymentGateway gateway, TimeProvider clock, Microsoft.Extensions.Options.IOptions<GoodsOrderOptions>? options = null) : IGoodsPaymentService
{
    public async Task<object> ListAsync(CancellationToken ct) => await repository.ListOrdersListAsync(ct);
    public async Task<GoodsOrderView> ReadAsync(long id, CancellationToken ct) => await ViewAsync(await LoadAsync(id, ct), ct);
    private async Task<Order> LoadAsync(long id, CancellationToken ct) => await repository.LoadOrdersAsync(id, ct) ?? throw Error(404, "Không tìm thấy đơn hàng hóa.");
    private static IReadOnlyList<GoodsLine> Lines(Order o) => o.OrderItems.OrderBy(i => i.Id).Select(i => new GoodsLine(i.ProductId, i.Product.Name, i.Quantity, i.UnitPriceVnd, i.SubtotalVnd)).ToList();
    public static void ValidateCart(Order o)
    {
        if (o.OrderItems.Count == 0 || o.TotalAmount <= 0 || o.OrderItems.Any(i => i.Quantity <= 0 || i.UnitPriceVnd < 0 || (long)i.Quantity * i.UnitPriceVnd != i.SubtotalVnd) || o.OrderItems.Sum(i => (long)i.SubtotalVnd) != o.TotalAmount)
            throw Error(409, "Dòng hàng hoặc tổng tiền không hợp lệ. Cần cập nhật lại Cart trước khi thanh toán.");
    }

    public Task<GoodsOrderView> CashAsync(long id, long actorId, int receivedAmount, CancellationToken ct) => repository.TransactionAsync(async () =>
    {
        var order = await LoadAsync(id, ct);
        var paid = order.Payments.SingleOrDefault(p => p.Status == "paid");
        if (paid != null && paid.Method == "cash" && order.PaymentState == "Paid")
            return await ViewAsync(order, ct);
        RequireCart(order);
        ValidateCart(order);
        if (receivedAmount != order.TotalAmount)
            throw Error(400, "Số tiền xác nhận phải bằng tổng đơn hàng.");
        await EnsureHeldAsync(order, ct);
        var payment = new Payment
        {
            GoodsOrderId = id,
            AmountVnd = order.TotalAmount,
            Method = "cash",
            Status = "paid",
            ReferenceCode = $"GOODS-{Guid.NewGuid():N}",
            ConfirmedBy = actorId
        };
        order.Payments.Add(payment);
        order.PaymentState = "Paid";
        order.Status = "Paid";
        Audit(id, actorId, "goods_cash_confirmed", new { at = clock.GetUtcNow(), receivedAmount, payment.ReferenceCode, lines = Lines(order) });
        Audit(id, actorId, "goods_paid_lines", Lines(order));
        await repository.SaveAsync(ct);
        return await ViewAsync(order, ct);
    }, null, ct);
    private static void RequireCart(Order order)
    {
        if (!string.Equals(order.Status, "Cart", StringComparison.OrdinalIgnoreCase) || (!string.IsNullOrEmpty(order.PaymentState) && !string.Equals(order.PaymentState, "Unpaid", StringComparison.OrdinalIgnoreCase)) || order.Payments.Any(p => p.Status is "pending" or "paid" or "review_required"))
            throw Error(409, "Đơn không còn là Cart chưa thanh toán. Không thể thu tiền lần nữa hoặc đổi phương thức.");
    }

    public async Task<GoodsOrderView> InstructionsAsync(long id, long actorId, CancellationToken ct)
    {
        gateway.ValidateConfiguration();
        var attempt = await repository.TransactionAsync(async () =>
        {
            var order = await LoadAsync(id, ct);
            var payment = order.Payments.SingleOrDefault(p => p.Status == "pending" && p.Method == "payos");
            GoodsCheckoutSnapshot snapshot;
            if (payment == null)
            {
                RequireCart(order);
                ValidateCart(order);
                await EnsureHeldAsync(order, ct);
                snapshot = new(clock.GetUtcNow().AddMinutes(15), order.TotalAmount, Lines(order));
                // The hold lasts at least as long as the payment link (RP3 UC-36 step 5).
                order.ExpiresAt = GoodsReservationRules.ExtendHold(order.ExpiresAt, snapshot.ExpiresAt);
                payment = new Payment
                {
                    GoodsOrderId = id,
                    AmountVnd = order.TotalAmount,
                    Method = "payos",
                    Status = "pending",
                    ReferenceCode = $"GOODS-{Guid.NewGuid():N}"};
                order.Payments.Add(payment);
                order.Status = "PendingPayment";
                order.PaymentState = "Pending";
                Audit(id, actorId, "goods_checkout", snapshot);
                await repository.SaveAsync(ct);
            }
            else
                snapshot = await SnapshotAsync(id, ct);
            ValidateSnapshot(order, payment, snapshot);
            if (snapshot.ExpiresAt <= clock.GetUtcNow())
                throw Error(409, "Mã thanh toán đã hết hạn. Cần đối soát trước khi lập đơn mới.");
            return (payment.Id, payment.AmountVnd, snapshot.ExpiresAt);
        }, null, ct);
        // Commit identity before calling payOS so timeout/retry reuses the same link.
        var link = await gateway.CreateGoodsAsync(attempt.Id, id, attempt.AmountVnd, attempt.ExpiresAt);
        return await repository.TransactionAsync(async () =>
        {
            var order = await LoadAsync(id, ct);
            var payment = order.Payments.Single(p => p.Id == attempt.Id);
            if (link.OrderCode != payment.Id || link.Amount != payment.AmountVnd || string.IsNullOrWhiteSpace(link.LinkId) || !Uri.TryCreate(link.Url, UriKind.Absolute, out var uri) || uri.Scheme != "https" || (payment.GatewayPaymentLinkId != null && payment.GatewayPaymentLinkId != link.LinkId))
                throw new GatewayException("Thông tin thanh toán hàng hóa không khớp.");
            payment.GatewayPaymentLinkId = link.LinkId;
            payment.CheckoutUrl = link.Url;
            await repository.SaveAsync(ct);
            return await ViewAsync(order, ct);
        }, null, ct);
    }

    public async Task<GoodsOrderView> ConfirmTransferAsync(long id, CancellationToken ct)
    {
        var order = await LoadAsync(id, ct);
        if (order.PaymentState == "Paid")
            return await ViewAsync(order, ct);
        var payment = order.Payments.SingleOrDefault(p => p.Method == "payos" && p.Status == "pending") ?? throw Error(409, "Đơn chưa có mã chuyển khoản đang chờ thanh toán.");
        var remote = await gateway.GetAsync(payment.Id);
        if (remote.OrderCode != payment.Id || (payment.GatewayPaymentLinkId != null && remote.LinkId != payment.GatewayPaymentLinkId))
            throw new GatewayException("Giao dịch ngân hàng không khớp đơn.");
        foreach (var transfer in remote.Transactions ?? [])
            await ApplyAsync(transfer, remote, ct);
        repository.ClearTracking();
        return await ReadAsync(id, ct);
    }

    public async Task<bool> TryWebhookAsync(string payload, CancellationToken ct)
    {
        var data = await gateway.VerifyAsync(payload);
        if (!await repository.TryWebhookPaymentsExistsAsync(ct, data))
            return false;
        await ApplyAsync(data, await gateway.GetAsync(data.OrderCode), ct);
        return true;
    }

    private async Task ApplyAsync(VerifiedBankEvent data, GatewayStatus remote, CancellationToken ct)
    {
        if (data.Amount is <= 0 or > int.MaxValue || string.IsNullOrWhiteSpace(data.Reference) || data.Reference.Length > 180 || string.IsNullOrWhiteSpace(data.LinkId))
            throw Error(400, "Dữ liệu giao dịch không hợp lệ.");
        var externalId = $"payos:{data.Reference}";
        await repository.TransactionAsync(async () =>
        {
            if (await repository.ApplyBankTransactionsExistsAsync(ct, externalId))
                return true;
            var payment = await repository.ApplyPaymentsAsync(data, ct);
            var order = await LoadAsync(payment.GoodsOrderId!.Value, ct);
            var snapshot = await SnapshotAsync(order.Id, ct);
            var valid = CanConfirmTransfer(order, payment, snapshot, data, remote, clock.GetUtcNow());
            var bank = new BankTransaction
            {
                ExternalId = externalId,
                AmountVnd = (int)data.Amount,
                ReferenceCode = payment.ReferenceCode,
                MatchedPaymentId = payment.Id,
                MatchStatus = valid ? "matched" : "review_required",
                RawPayload = data.RawPayload
            };
            repository.AddBankTransactions(bank);
            if (valid)
            {
                payment.Status = "paid";
                payment.GatewayPaymentLinkId = data.LinkId;
                order.PaymentState = "Paid";
                order.Status = "Paid";
                Audit(order.Id, null, "goods_paid_lines", snapshot.Lines);
            }
            else if (order.PaymentState != "Paid")
            {
                payment.Status = "review_required";
                order.PaymentState = "NeedsReview";
            }

            Audit(order.Id, null, valid ? "goods_transfer_confirmed" : "goods_transfer_review", new { at = clock.GetUtcNow(), data.Reference, data.Amount });
            await repository.SaveAsync(ct);
            return true;
        }, externalId, ct);
    }

    public Task<GoodsOrderView> InvoiceAsync(long id, long actorId, CancellationToken ct) => repository.TransactionAsync(async () =>
    {
        var order = await LoadAsync(id, ct);
        var existing = await InvoiceRecordAsync(id, ct);
        if (existing != null)
            return await ViewAsync(order, ct);
        var payment = order.Payments.SingleOrDefault(p => p.Status == "paid" && p.OrderId == null);
        if (order.PaymentState != "Paid" || order.Status != "Paid" || payment == null || payment.AmountVnd != order.TotalAmount)
            throw Error(409, "Chỉ xuất hóa đơn cho đơn đã xác nhận đủ tiền.");
        ValidateCart(order);
        var paidLines = JsonSerializer.Deserialize<List<GoodsLine>>(await AuditValueAsync(id, "goods_paid_lines", ct) ?? throw Error(409, "Thiếu bản ghi hàng hóa đã thanh toán. Cần đối soát."))!;
        if (!Lines(order).SequenceEqual(paidLines))
            throw Error(409, "Dòng hàng đã thay đổi sau khi thanh toán. Cần đối soát trước khi xuất hóa đơn.");
        var invoice = new GoodsInvoice($"INV-G-{id:D10}", clock.GetUtcNow(), actorId, id, order.OrderCode, order.CustomerName, order.TotalAmount, payment.Method!, payment.ReferenceCode, paidLines);
        Audit(id, actorId, "goods_invoice_issued", invoice);
        order.Status = "Invoiced";
        await repository.SaveAsync(ct);
        return await ViewAsync(order, ct);
    }, null, ct);
    private async Task<GoodsOrderView> ViewAsync(Order order, CancellationToken ct)
    {
        var payment = order.Payments.OrderByDescending(p => p.Id).FirstOrDefault();
        var json = await AuditValueAsync(order.Id, "goods_checkout", ct);
        var expiry = json == null ? (DateTimeOffset? )null : JsonSerializer.Deserialize<GoodsCheckoutSnapshot>(json)!.ExpiresAt;
        return new(order.Id, order.OrderCode, order.CustomerName, order.TotalAmount, order.Status, order.PaymentState, order.HandoverState, Lines(order), payment?.Method, payment?.ReferenceCode, payment?.Status == "pending" && expiry > clock.GetUtcNow() ? payment.CheckoutUrl : null, expiry, await InvoiceRecordAsync(order.Id, ct), order.ReservationState, order.ExpiresAt, CanCancel(order), order.DeliveredAt, order.CancelledAt);
    }

    private async Task<GoodsInvoice?> InvoiceRecordAsync(long id, CancellationToken ct)
    {
        var json = await AuditValueAsync(id, "goods_invoice_issued", ct);
        return json == null ? null : JsonSerializer.Deserialize<GoodsInvoice>(json);
    }

    private Task<string?> AuditValueAsync(long id, string action, CancellationToken ct) => repository.AuditValueAuditLogsAsync(id, action, ct);
    private async Task<GoodsCheckoutSnapshot> SnapshotAsync(long id, CancellationToken ct) => JsonSerializer.Deserialize<GoodsCheckoutSnapshot>(await AuditValueAsync(id, "goods_checkout", ct) ?? throw Error(409, "Thiếu bản ghi thanh toán. Cần đối soát đơn hàng."))!;
    private static bool MatchesSnapshot(Order order, Payment payment, GoodsCheckoutSnapshot snapshot) => order.Status == "PendingPayment" && order.PaymentState == "Pending" && payment.Status == "pending" && order.TotalAmount == payment.AmountVnd && snapshot.Amount == payment.AmountVnd && Lines(order).SequenceEqual(snapshot.Lines);
    public static bool CanConfirmTransfer(Order order, Payment payment, GoodsCheckoutSnapshot snapshot, VerifiedBankEvent data, GatewayStatus remote, DateTimeOffset now) => payment.GoodsOrderId == order.Id && payment.OrderId == null && payment.Method == "payos" && data.OrderCode == payment.Id && data.Success && data.Currency == "VND" && data.Amount == payment.AmountVnd && remote.OrderCode == payment.Id && remote.LinkId == data.LinkId && remote.AmountPaid == payment.AmountVnd && remote.Status == "PAID" && (payment.GatewayPaymentLinkId == null || payment.GatewayPaymentLinkId == data.LinkId) && snapshot.ExpiresAt > now && MatchesSnapshot(order, payment, snapshot);
    private static void ValidateSnapshot(Order order, Payment payment, GoodsCheckoutSnapshot snapshot)
    {
        if (!MatchesSnapshot(order, payment, snapshot))
            throw Error(409, "Đơn đã thay đổi sau khi tạo mã thanh toán. Cần đối soát.");
    }

    private void Audit(long id, long? actor, string action, object value) => repository.AddAuditLogs(new AuditLog { EntityType = "GoodsOrder", EntityId = id, ActorId = actor, Action = action, NewValue = JsonSerializer.Serialize(value) });
    private static PaymentException Error(int status, string message) => new(status, message);
}
