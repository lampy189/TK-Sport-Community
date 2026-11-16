namespace TKSC.Application.DTOs;
public record PaymentRecoveryInput(string? Evidence, long? BankTransactionId);
public record RecoveryBank(long Id, string Reference, int AmountVnd, string? Status);
public record RecoveryHistory(long? ActorId, string? Action, string? Detail);
public record PaymentRecoveryItem(long Id, string Kind, long? OrderId, string Reference, int AmountVnd, string Status, IReadOnlyList<RecoveryBank> Transactions, IReadOnlyList<RecoveryHistory> History);
