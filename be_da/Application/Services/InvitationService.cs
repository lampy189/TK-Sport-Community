using be_da.Application.DTOs;
using be_da.Application.Interfaces.Repositories;
using be_da.Application.Interfaces.Services;
namespace be_da.Application.Services;
public sealed class InvitationService(IInvitationRepository repository, ISessionStore sessions) : IInvitationService
{
    public async Task<OperationResult> ListAsync(long userId, CancellationToken ct) => OperationResult.Success(await repository.ListAsync(userId, ct));
    public async Task<OperationResult> CandidatesAsync(long userId, long sessionId, CancellationToken ct) => OperationResult.Success(await repository.CandidatesAsync(userId, sessionId, ct));
    public Task<OperationResult> SendAsync(long userId, long sessionId, InviteSessionRequest request, CancellationToken ct) =>
        request.FriendPlayerId <= 0 || request.PaymentChoice is not ("separate" or "inviter")
            ? Task.FromResult(OperationResult.Invalid(new { code = "invalid_invitation", message = "Chọn bạn và người thanh toán hợp lệ." }))
            : repository.SendAsync(userId, sessionId, request, ct);
    public async Task<OperationResult> ReplyAsync(long userId, long id, bool accept, CancellationToken ct)
    {
        if (!accept) return await repository.DeclineAsync(userId, id, ct);
        var result = await sessions.AcceptInvitationAsync(userId, id, ct);
        return result.Success ? OperationResult.Success(result) : OperationResult.Conflict(new { code = result.ErrorCode, message = result.Message });
    }
}
