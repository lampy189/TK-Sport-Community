using be_da.Application.DTOs;
using be_da.Application.Validators;
using be_da.Application.Interfaces.Repositories;
using be_da.Application.Interfaces.Services;

namespace be_da.Application.Services;
// UC-41 / UC-42: request -> issue -> use report -> confirmed return. Requests are validated before any SQL runs.
public sealed class SessionSupplyService(ISessionSupplyStore repository, TimeProvider clock) : ISessionSupplyService
{
    public async Task<OperationResult> GetSessionsAsync(DateOnly? date, long? refereeId, CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(clock.GetUtcNow().ToOffset(TimeSpan.FromHours(7)).DateTime);
        var day = date ?? today;
        if (SupplyRules.ValidateDay(day, today)is { } error)
            return OperationResult.Invalid(new { code = "VALIDATION_FAILED", message = error });
        return OperationResult.Success(await repository.GetSessionsAsync(day, refereeId, cancellationToken));
    }

    public Task<SessionSupplyDetails?> GetDetailsAsync(long sessionId, CancellationToken cancellationToken) => repository.GetDetailsAsync(sessionId, cancellationToken);
    public Task<bool> IsRefereeAssignedAsync(long sessionId, long refereeId, CancellationToken cancellationToken) => repository.IsRefereeAssignedAsync(sessionId, refereeId, cancellationToken);
    public Task<SupplyResult> IssueAsync(long sessionId, IssueSupplyRequest request, long staffUserId, CancellationToken cancellationToken) => SupplyRules.ValidateIssue(request)is { } error ? Invalid(error) : repository.IssueAsync(sessionId, request, staffUserId, cancellationToken);
    public Task<SupplyResult> IssueRequestAsync(long sessionId, long requestId, IssueSupplyFromRequest request, long staffUserId, CancellationToken cancellationToken) => SupplyRules.ValidateIssueFromRequest(request)is { } error ? Invalid(error) : repository.IssueRequestAsync(sessionId, requestId, request, staffUserId, cancellationToken);
    public Task<SupplyResult> RejectRequestAsync(long sessionId, long requestId, SupplyDecisionRequest request, long staffUserId, CancellationToken cancellationToken) => SupplyRules.ValidateReject(request)is { } error ? Invalid(error) : repository.RejectRequestAsync(sessionId, requestId, request.Note!.Trim(), staffUserId, cancellationToken);
    public Task<SupplyResult> CreateRequestAsync(long sessionId, CreateSupplyRequest request, long refereeId, CancellationToken cancellationToken) => SupplyRules.ValidateRequest(request)is { } error ? Invalid(error) : repository.CreateRequestAsync(sessionId, request, refereeId, cancellationToken);
    public Task<SupplyResult> CancelRequestAsync(long sessionId, long requestId, long refereeId, CancellationToken cancellationToken) => repository.CancelRequestAsync(sessionId, requestId, refereeId, cancellationToken);
    public Task<SupplyResult> ReportUseAsync(long sessionId, long productId, ReportSupplyUseRequest request, long refereeId, CancellationToken cancellationToken) => SupplyRules.ValidateUse(request)is { } error ? Invalid(error) : repository.ReportUseAsync(sessionId, productId, request, refereeId, cancellationToken);
    public Task<SupplyResult> ReconcileAsync(long sessionId, long productId, ReturnSupplyRequest request, long staffUserId, CancellationToken cancellationToken) => SupplyRules.ValidateReturn(request)is { } error ? Invalid(error) : repository.ReconcileAsync(sessionId, productId, request, staffUserId, cancellationToken);
    private static Task<SupplyResult> Invalid(string error) => Task.FromResult(SupplyResult.Fail("VALIDATION_FAILED", error));
}
