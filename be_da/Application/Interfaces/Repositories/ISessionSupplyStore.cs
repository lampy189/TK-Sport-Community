using be_da.Application.DTOs;
using be_da.Application.Validators;

namespace be_da.Application.Interfaces.Repositories;
public interface ISessionSupplyStore
{
    // refereeId limits the list to sessions that referee is assigned to.
    Task<IReadOnlyList<SupplySession>> GetSessionsAsync(DateOnly date, long? refereeId, CancellationToken cancellationToken);
    Task<SessionSupplyDetails?> GetDetailsAsync(long sessionId, CancellationToken cancellationToken);
    Task<bool> IsRefereeAssignedAsync(long sessionId, long refereeId, CancellationToken cancellationToken);
    // Atomic: posts one SessionIssue movement, decreases on-hand stock and records the recipient.
    Task<SupplyResult> IssueAsync(long sessionId, IssueSupplyRequest request, long staffUserId, CancellationToken cancellationToken);
    // Atomic: issues a pending referee request to its requester and marks the request Issued.
    Task<SupplyResult> IssueRequestAsync(long sessionId, long requestId, IssueSupplyFromRequest request, long staffUserId, CancellationToken cancellationToken);
    Task<SupplyResult> RejectRequestAsync(long sessionId, long requestId, string note, long staffUserId, CancellationToken cancellationToken);
    // The assigned referee asks for supplies; stock does not move until Staff issues.
    Task<SupplyResult> CreateRequestAsync(long sessionId, CreateSupplyRequest request, long refereeId, CancellationToken cancellationToken);
    Task<SupplyResult> CancelRequestAsync(long sessionId, long requestId, long refereeId, CancellationToken cancellationToken);
    // The assigned referee reports observed use and unused returns; stock does not move until Staff confirms the return.
    Task<SupplyResult> ReportUseAsync(long sessionId, long productId, ReportSupplyUseRequest request, long refereeId, CancellationToken cancellationToken);
    // Atomic: posts the unused return (if any) and closes the allocation; used = issued - returned.
    Task<SupplyResult> ReconcileAsync(long sessionId, long productId, ReturnSupplyRequest request, long staffUserId, CancellationToken cancellationToken);
}
