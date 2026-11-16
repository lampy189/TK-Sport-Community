using be_da.Application.DTOs;
using be_da.Application.Validators;
using be_da.Application.Interfaces.Repositories;
using be_da.Application.Interfaces.Services;

namespace be_da.Application.Services;
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
    public Task<SupplyResult> IssueAsync(long sessionId, IssueSupplyRequest request, long staffUserId, CancellationToken cancellationToken) => SupplyRules.ValidateIssue(request)is { } error ? Task.FromResult(SupplyResult.Fail("VALIDATION_FAILED", error)) : repository.IssueAsync(sessionId, request, staffUserId, cancellationToken);
    public Task<SupplyResult> ReportUseAsync(long sessionId, long productId, ReportSupplyUseRequest request, long refereeId, CancellationToken cancellationToken) => SupplyRules.ValidateUse(request)is { } error ? Task.FromResult(SupplyResult.Fail("VALIDATION_FAILED", error)) : repository.ReportUseAsync(sessionId, productId, request, refereeId, cancellationToken);
    public Task<SupplyResult> ReconcileAsync(long sessionId, long productId, ReturnSupplyRequest request, long staffUserId, CancellationToken cancellationToken) => SupplyRules.ValidateReturn(request)is { } error ? Task.FromResult(SupplyResult.Fail("VALIDATION_FAILED", error)) : repository.ReconcileAsync(sessionId, productId, request, staffUserId, cancellationToken);
}
