using be_da.Application.DTOs;

namespace be_da.Application.Interfaces.Services;
public interface ILateEntryService
{
    Task<IReadOnlyList<LateEntryOffer>> List(long? venue, long? session, bool manage, CancellationToken ct);
}
