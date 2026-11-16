namespace be_da.Application.Interfaces.Services;
public interface ISportCenterService
{
    Task<object> ListAsync(CancellationToken ct);
    Task<object> PersonnelAsync(CancellationToken ct);
    Task<bool> AssignAsync(long id, long venueId, long actor, CancellationToken ct);
}
