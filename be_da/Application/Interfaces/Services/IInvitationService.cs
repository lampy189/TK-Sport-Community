using be_da.Application.DTOs;
namespace be_da.Application.Interfaces.Services;
public interface IInvitationService
{
    Task<OperationResult> ListAsync(long userId, CancellationToken ct);
    Task<OperationResult> CandidatesAsync(long userId, long sessionId, CancellationToken ct);
    Task<OperationResult> SendAsync(long userId, long sessionId, InviteSessionRequest request, CancellationToken ct);
    Task<OperationResult> ReplyAsync(long userId, long id, bool accept, CancellationToken ct);
}
