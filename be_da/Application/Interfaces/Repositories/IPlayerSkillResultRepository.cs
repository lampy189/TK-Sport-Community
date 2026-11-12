namespace TKSC.Application.Interfaces.Repositories;
public interface IPlayerSkillResultRepository
{
    Task<object> ListAsync(long userId, long sportId, CancellationToken ct);
}
