using be_da.Application.DTOs;
namespace be_da.Application.Interfaces.Repositories;
public interface IInvitationRepository
{
    Task<IReadOnlyList<SessionInvitation>> ListAsync(long userId, CancellationToken ct);
    Task<IReadOnlyList<InvitationCandidate>> CandidatesAsync(long userId, long sessionId, CancellationToken ct);
    Task<OperationResult> SendAsync(long userId, long sessionId, InviteSessionRequest request, CancellationToken ct);
    Task<OperationResult> DeclineAsync(long userId, long id, CancellationToken ct);
}
