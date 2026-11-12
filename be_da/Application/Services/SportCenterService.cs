using be_da.Application.Interfaces.Repositories;
using be_da.Application.Interfaces.Services;

namespace be_da.Application.Services;
public sealed class SportCenterService(ISportCenterRepository repository) : ISportCenterService
{
    public Task<object> ListAsync(CancellationToken ct) => repository.ListAsync(ct);
    public Task<object> PersonnelAsync(CancellationToken ct) => repository.PersonnelAsync(ct);
    public Task<bool> AssignAsync(long id, long venueId, long actor, CancellationToken ct) => repository.AssignAsync(id, venueId, actor, ct);
}
