namespace TKSC.Application.DTOs;
public record RefundDecisionInput(bool Approve, int AmountVnd, long RecipientId, string? Reason);
public record RefundPayoutInput(int AmountVnd, long RecipientId, string BankReference, string Document, DateTimeOffset PaidAt);
public record RefundDecisionRecord(int AmountVnd, long RecipientId, string Reason, string Basis, DateTimeOffset At);
public record RefundPayoutRecord(int AmountVnd, long RecipientId, string BankReference, string Document, DateTimeOffset PaidAt, long RecordedBy);
