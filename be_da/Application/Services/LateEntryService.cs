using be_da.Application.DTOs;
using be_da.Application.Interfaces.Repositories;
using be_da.Application.Interfaces.Services;

namespace be_da.Application.Services;
public sealed class LateEntryService(ILateEntryStore repository) : ILateEntryService
{
    public Task SetPaused(long? venue, long session, long actor, bool paused, CancellationToken ct) => repository.SetPaused(venue, session, actor, paused, ct);
    public Task<IReadOnlyList<LateEntryOffer>> List(long? venue, long? session, bool manage, CancellationToken ct) => repository.List(venue, session, manage, ct);
}
