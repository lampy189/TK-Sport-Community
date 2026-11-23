using be_da.Application.Interfaces.External;
namespace TKSC.Api.Tests;
// SQL SESSION_CONTEXT does not set EF's application scope when RLS is off.
internal sealed class MutableTestDataScope : IDataAccessContext
{
 public string Role {get;set;}="service";
 public long? VenueId {get;set;}
 public long? RestrictedVenueId => Role is "Owner" or "service" ? null : VenueId;
}
