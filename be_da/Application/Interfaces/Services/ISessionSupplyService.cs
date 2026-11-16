using be_da.Application.DTOs;
using be_da.Application.Validators;

namespace be_da.Application.Interfaces.Services;
public interface ISessionSupplyService
{
    Task<OperationResult> GetSessionsAsync(DateOnly? date, long? refereeId, CancellationToken cancellationToken);
    Task<SessionSupplyDetails?> GetDetailsAsync(long sessionId, CancellationToken cancellationToken);
    Task<bool> IsRefereeAssignedAsync(long sessionId, long refereeId, CancellationToken cancellationToken);
    Task<SupplyResult> IssueAsync(long sessionId, IssueSupplyRequest request, long staffUserId, CancellationToken cancellationToken);
    Task<SupplyResult> IssueRequestAsync(long sessionId, long requestId, IssueSupplyFromRequest request, long staffUserId, CancellationToken cancellationToken);
    Task<SupplyResult> RejectRequestAsync(long sessionId, long requestId, SupplyDecisionRequest request, long staffUserId, CancellationToken cancellationToken);
    Task<SupplyResult> CreateRequestAsync(long sessionId, CreateSupplyRequest request, long refereeId, CancellationToken cancellationToken);
    Task<SupplyResult> CancelRequestAsync(long sessionId, long requestId, long refereeId, CancellationToken cancellationToken);
    Task<SupplyResult> ReportUseAsync(long sessionId, long productId, ReportSupplyUseRequest request, long refereeId, CancellationToken cancellationToken);
    Task<SupplyResult> ReconcileAsync(long sessionId, long productId, ReturnSupplyRequest request, long staffUserId, CancellationToken cancellationToken);
}
