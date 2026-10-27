namespace TKSC.Application.DTOs;

public record PaymentSummary(long Id, string ReferenceCode, string Status, int AmountVnd);
public record TicketSummary(long Id, long OrderId, long PlayerId, long SessionId, int AmountVnd,
    string Status, string? TicketCode, bool Valid, DateOnly? SessionDate, TimeOnly? StartTime, TimeOnly? EndTime);
public record OrderSummary(long Id, string OrderCode, int TotalAmount, string Status,
    DateTimeOffset? ExpiresAt, IReadOnlyList<PaymentSummary> Payments, IReadOnlyList<TicketSummary> Tickets);
public record ReviewSummary(long Id, long? OrderId, string ReferenceCode, int AmountVnd,
    string Status, IReadOnlyList<ReviewReason> Reasons);
public record ReviewReason(string? Action, string? NewValue);
public record CheckoutResult(long Id, string ReferenceCode, string Status, string PaymentUrl);
public record GatewayCheckout(long OrderCode, long Amount, string LinkId, string Url, string Status);
public record GatewayStatus(long OrderCode, long AmountPaid, string Status, string LinkId, IReadOnlyList<VerifiedBankEvent>? Transactions = null);
public record VerifiedBankEvent(long OrderCode, long Amount, string Reference, string LinkId,
    string Currency, bool Success, string RawPayload);
public sealed class PaymentException(int statusCode, string message) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
}
public sealed class GatewayException(string message, bool invalidSignature = false) : Exception(message)
{
    public bool InvalidSignature { get; } = invalidSignature;
}
