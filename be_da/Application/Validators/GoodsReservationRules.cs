using TKSC.Application.DTOs;

namespace TKSC.Application.Validators;
// Stock reservation of goods orders (RP3 UC-36..UC-38, JOB-04; task split 13/11/2026).
// Available to sell = on hand - reserved. An order holds its lines from cart until handover, cancellation or expiry.
public static class GoodsReservationRules
{
    public const string Held = "Held";
    public const string Released = "Released";
    public const string Consumed = "Consumed";
    public const string Cancelled = "Cancelled";
    public const string Expired = "Expired";
    public const int MinHoldMinutes = 1;
    public const int MaxHoldMinutes = 24 * 60;

    // order_items holds one row per product (UX_order_items_order_id_product_id), so cart lines of one product merge.
    public static IReadOnlyList<(long ProductId, long Quantity)> MergeCartLines(IEnumerable<CreateCartLine> lines) =>
        lines.GroupBy(line => line.ProductId).Select(group => (group.Key, group.Sum(line => (long)line.Quantity))).ToList();

    public static string? ValidateCart(IReadOnlyList<CreateCartLine>? lines)
    {
        if (lines is null || lines.Count == 0)
            return "Vui lòng chọn ít nhất một món hàng.";
        if (lines.Count > 100)
            return "Một đơn tối đa 100 dòng hàng.";
        if (lines.Any(line => line.ProductId <= 0))
            return "Mặt hàng không hợp lệ.";
        if (lines.Any(line => line.Quantity <= 0))
            return "Số lượng phải lớn hơn 0.";
        return MergeCartLines(lines).Any(line => line.Quantity > short.MaxValue) ? $"Tổng số lượng mỗi mặt hàng tối đa {short.MaxValue}." : null;
    }

    public static int ClampHoldMinutes(int minutes) => Math.Clamp(minutes, MinHoldMinutes, MaxHoldMinutes);
    public static DateTimeOffset HoldUntil(DateTimeOffset now, int minutes) => now.AddMinutes(ClampHoldMinutes(minutes));
    // A payment link must stay usable for as long as it is valid, so the hold never ends before it.
    public static DateTimeOffset? ExtendHold(DateTimeOffset? current, DateTimeOffset paymentExpiresAt) => current is null || current < paymentExpiresAt ? paymentExpiresAt : current;

    // Only orders that nobody has paid for can be cancelled; paid goods stay protected until handover.
    public static string? ValidateCancel(string status, string? paymentState, bool hasPaidPayment) =>
        hasPaidPayment || paymentState is "Paid" or "NeedsReview" ? "Đơn đã có tiền thanh toán, không thể hủy. Cần đối soát hoặc hoàn tiền." :
        status is "Cart" or "PendingPayment" ? null : "Chỉ hủy được đơn chưa thanh toán.";

    // JOB-04: release only unpaid holds past their expiry.
    public static bool IsDueForExpiry(string status, string? paymentState, string? reservationState, DateTimeOffset? expiresAt, bool hasPaidPayment, DateTimeOffset now) =>
        reservationState == Held && expiresAt is { } end && end <= now && !hasPaidPayment &&
        status is "Cart" or "PendingPayment" && paymentState is null or "Unpaid" or "Pending";
}
