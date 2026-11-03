using be_da.Application.Stock;

namespace be_da.Application.Abstractions;

public interface ISessionSupplyStore
{
    Task<IReadOnlyList<SupplySession>> GetSessionsAsync(DateOnly date, CancellationToken cancellationToken);
    Task<SessionSupplyDetails?> GetDetailsAsync(long sessionId, CancellationToken cancellationToken);
    // Atomic: posts one SessionIssue movement, decreases on-hand stock and records the recipient.
    Task<SupplyResult> IssueAsync(long sessionId, IssueSupplyRequest request, long staffUserId, CancellationToken cancellationToken);
    // Atomic: posts the unused return (if any) and closes the allocation; used = issued - returned.
    Task<SupplyResult> ReconcileAsync(long sessionId, long productId, ReturnSupplyRequest request, long staffUserId, CancellationToken cancellationToken);
}
