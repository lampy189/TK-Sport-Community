using TKSC.Application.DTOs;
using TKSC.Application.Validators;
using Xunit;

namespace TKSC.Api.Tests.Stock;

// Task split 13/11/2026 (Dũng): stock held for goods orders, cancel and expiry (RP3 UC-36, JOB-04).
public sealed class GoodsReservationUnitTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 13, 10, 0, 0, TimeSpan.FromHours(7));
    private static CreateCartLine Line(long product, short quantity) => new(product, quantity);

    [UnitCase("UTC001", "MergeCartLines", "Normal", "product 1: 2 + 3; product 2: 1", "Return: product 1=5; product 2=1 (one order line per product)")]
    public void Merge_SameProduct()
    {
        var lines = GoodsReservationRules.MergeCartLines([Line(1, 2), Line(1, 3), Line(2, 1)]);
        Assert.Equal([(1L, 5L), (2L, 1L)], lines);
    }

    [UnitCase("UTC002", "MergeCartLines", "Boundary", "product 1: 32767 + 1", "Return: product 1=32768 (sum kept as long, no overflow)")]
    public void Merge_NoOverflow() => Assert.Equal(32768, GoodsReservationRules.MergeCartLines([Line(1, short.MaxValue), Line(1, 1)]).Single().Quantity);

    [UnitCase("UTC003", "ValidateCart", "Normal", "two lines, positive quantities", "Return: null")]
    public void ValidateCart_Ok() => Assert.Null(GoodsReservationRules.ValidateCart([Line(1, 2), Line(2, 1)]));

    [UnitCase("UTC004", "ValidateCart", "Abnormal", "no lines", "Return: choose at least one item message")]
    public void ValidateCart_Empty() => Assert.NotNull(GoodsReservationRules.ValidateCart([]));

    [UnitCase("UTC005", "ValidateCart", "Abnormal", "quantity 0", "Return: quantity must be > 0 message")]
    public void ValidateCart_Zero() => Assert.NotNull(GoodsReservationRules.ValidateCart([Line(1, 0)]));

    [UnitCase("UTC006", "ValidateCart", "Abnormal", "productId 0", "Return: invalid item message")]
    public void ValidateCart_BadProduct() => Assert.NotNull(GoodsReservationRules.ValidateCart([Line(0, 1)]));

    [UnitCase("UTC007", "ValidateCart", "Boundary", "same product 32767 + 1 (merged 32768)", "Return: at most 32767 per product message")]
    public void ValidateCart_MergedTooLarge() => Assert.NotNull(GoodsReservationRules.ValidateCart([Line(1, short.MaxValue), Line(1, 1)]));

    [UnitCase("UTC008", "ValidateCart", "Boundary", "101 lines", "Return: at most 100 lines message")]
    public void ValidateCart_TooManyLines() => Assert.NotNull(GoodsReservationRules.ValidateCart(Enumerable.Range(1, 101).Select(i => Line(i, 1)).ToList()));

    [UnitCase("UTC009", "HoldUntil", "Normal", "now 10:00; 15 minutes", "Return: 10:15")]
    public void HoldUntil_Default() => Assert.Equal(Now.AddMinutes(15), GoodsReservationRules.HoldUntil(Now, 15));

    [UnitCase("UTC010", "HoldUntil", "Boundary", "now 10:00; 0 minutes (bad configuration)", "Return: 10:01 (at least one minute)")]
    public void HoldUntil_Clamped() => Assert.Equal(Now.AddMinutes(1), GoodsReservationRules.HoldUntil(Now, 0));

    [UnitCase("UTC011", "ExtendHold", "Normal", "hold 10:05; payment link valid to 10:15", "Return: 10:15 (hold lasts as long as the link)")]
    public void ExtendHold_ToPayment() => Assert.Equal(Now.AddMinutes(15), GoodsReservationRules.ExtendHold(Now.AddMinutes(5), Now.AddMinutes(15)));

    [UnitCase("UTC012", "ExtendHold", "Normal", "hold 10:30; payment link valid to 10:15", "Return: 10:30 (never shortened)")]
    public void ExtendHold_KeepsLonger() => Assert.Equal(Now.AddMinutes(30), GoodsReservationRules.ExtendHold(Now.AddMinutes(30), Now.AddMinutes(15)));

    [UnitCase("UTC013", "ValidateCancel", "Normal", "status=Cart; payment=Unpaid; no paid payment", "Return: null (can cancel)")]
    public void Cancel_Cart() => Assert.Null(GoodsReservationRules.ValidateCancel("Cart", "Unpaid", false));

    [UnitCase("UTC014", "ValidateCancel", "Normal", "status=PendingPayment; payment=Pending", "Return: null (can cancel)")]
    public void Cancel_PendingTransfer() => Assert.Null(GoodsReservationRules.ValidateCancel("PendingPayment", "Pending", false));

    [UnitCase("UTC015", "ValidateCancel", "Abnormal", "status=Paid; payment=Paid", "Return: already paid message (paid goods are protected)")]
    public void Cancel_Paid() => Assert.NotNull(GoodsReservationRules.ValidateCancel("Paid", "Paid", true));

    [UnitCase("UTC016", "ValidateCancel", "Abnormal", "status=PendingPayment; payment=NeedsReview", "Return: money needs review message")]
    public void Cancel_NeedsReview() => Assert.NotNull(GoodsReservationRules.ValidateCancel("PendingPayment", "NeedsReview", false));

    [UnitCase("UTC017", "ValidateCancel", "Abnormal", "status=Completed; payment=Paid", "Return: already paid message")]
    public void Cancel_Completed() => Assert.NotNull(GoodsReservationRules.ValidateCancel("Completed", "Paid", true));

    [UnitCase("UTC018", "IsDueForExpiry", "Normal", "Held; expired 1 minute ago; Cart/Unpaid; no paid payment", "Return: true (release the hold)")]
    public void Expiry_Due() => Assert.True(GoodsReservationRules.IsDueForExpiry("Cart", "Unpaid", "Held", Now.AddMinutes(-1), false, Now));

    [UnitCase("UTC019", "IsDueForExpiry", "Boundary", "Held; expires exactly now", "Return: true")]
    public void Expiry_ExactlyNow() => Assert.True(GoodsReservationRules.IsDueForExpiry("PendingPayment", "Pending", "Held", Now, false, Now));

    [UnitCase("UTC020", "IsDueForExpiry", "Normal", "Held; expires in 1 minute", "Return: false")]
    public void Expiry_NotYet() => Assert.False(GoodsReservationRules.IsDueForExpiry("Cart", "Unpaid", "Held", Now.AddMinutes(1), false, Now));

    [UnitCase("UTC021", "IsDueForExpiry", "Abnormal", "Held; expired; a payment was confirmed meanwhile", "Return: false (paid goods stay held)")]
    public void Expiry_PaidMeanwhile() => Assert.False(GoodsReservationRules.IsDueForExpiry("Cart", "Unpaid", "Held", Now.AddMinutes(-1), true, Now));

    [UnitCase("UTC022", "IsDueForExpiry", "Abnormal", "Released; expired", "Return: false (never released twice)")]
    public void Expiry_AlreadyReleased() => Assert.False(GoodsReservationRules.IsDueForExpiry("Cancelled", "Unpaid", "Released", Now.AddMinutes(-1), false, Now));

    [UnitCase("UTC023", "IsDueForExpiry", "Abnormal", "legacy order: no hold (NULL); no expiry", "Return: false")]
    public void Expiry_Legacy() => Assert.False(GoodsReservationRules.IsDueForExpiry("Cart", "Unpaid", null, null, false, Now));

    [UnitCase("UTC024", "IsDueForExpiry", "Abnormal", "Held; expired; status=Paid; payment=Paid", "Return: false")]
    public void Expiry_PaidStatus() => Assert.False(GoodsReservationRules.IsDueForExpiry("Paid", "Paid", "Held", Now.AddMinutes(-1), false, Now));
}
