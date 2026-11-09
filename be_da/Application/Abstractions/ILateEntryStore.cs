using be_da.Application.Sessions;
namespace be_da.Application.Abstractions;
public interface ILateEntryStore
{
    Task<IReadOnlyList<LateEntryOffer>> List(long? venue,long? session,bool manage,CancellationToken ct);
    Task<LateEntryOffer> Publish(long id,long venue,long actor,PublishLateEntryRequest input,bool stop,CancellationToken ct);
}
