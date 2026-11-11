namespace be_da.Application.Interfaces.Services;
public interface IPlayerSkillResultService
{
    Task<object> ListAsync(long userId, long sportId, CancellationToken ct);
}
