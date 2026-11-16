using be_da.Application.DTOs;
namespace be_da.Application.Interfaces.Repositories;
public interface IOwnerPolicyRepository
{
    Task<OperationResult> SubmitAsync(SubmitPolicyRequest request, long actor, CancellationToken ct);
    Task<OperationResult> DecideAsync(long id, DecidePolicyRequest request, long actor, CancellationToken ct);
    Task<IReadOnlyList<PolicyVersion>> HistoryAsync(string? code, long? venueId, CancellationToken ct);
}
