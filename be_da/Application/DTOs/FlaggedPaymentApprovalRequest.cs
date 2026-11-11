using be_da.Application.DTOs;

namespace be_da.Application.DTOs;
public record FlaggedPaymentApprovalRequest(long BankTransactionId, string? Evidence);
