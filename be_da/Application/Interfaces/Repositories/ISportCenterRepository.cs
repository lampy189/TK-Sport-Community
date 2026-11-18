namespace be_da.Application.Interfaces.Repositories;
public interface ISportCenterRepository
{
    Task<object> ListAsync(CancellationToken ct);
    Task<object> PersonnelAsync(CancellationToken ct);
    Task<bool> AssignAsync(long id, long venueId, long actor, CancellationToken ct);
}
