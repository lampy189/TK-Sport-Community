using be_da.Application.Sessions;
using be_da.Domain.Entities;

namespace be_da.Application.Abstractions;

public interface ISessionStore
{
    Task<IReadOnlyList<SessionSummary>> GetSessionsAsync(SessionFilter filter, long? currentUserId, CancellationToken cancellationToken);
    Task<SessionDetails?> GetSessionByIdAsync(long id, long? currentUserId, CancellationToken cancellationToken);
    Task<RegisterSessionResult> RegisterSessionAsync(long userId, long sessionId, CancellationToken cancellationToken);
}
