using TKSC.Application.DTOs;
using be_da.Application.Interfaces.Services;
using TKSC.Application.Interfaces.Repositories;

namespace TKSC.Application.Services;
public sealed class PlayerSkillResultService(IPlayerSkillResultRepository repository) : IPlayerSkillResultService
{
    public Task<object> ListAsync(long userId, long sportId, CancellationToken ct) => repository.ListAsync(userId, sportId, ct);
}
