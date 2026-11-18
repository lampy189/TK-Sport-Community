using be_da.Application.DTOs;

namespace be_da.Application.Interfaces.Repositories;
public interface ILateEntryStore
{
    Task<IReadOnlyList<LateEntryOffer>> List(long? venue, long? session, bool manage, CancellationToken ct);
}
