using be_da.Application.DTOs;

namespace be_da.Application.Interfaces.Services;
public interface ILateEntryService
{
    Task SetPaused(long? venue, long session, long actor, bool paused, CancellationToken ct);
    Task<IReadOnlyList<LateEntryOffer>> List(long? venue, long? session, bool manage, CancellationToken ct);
}
