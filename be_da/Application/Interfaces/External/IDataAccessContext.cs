namespace be_da.Application.Interfaces.External;
// Database tenancy identity; implementations may obtain it from HTTP or a background job.
public interface IDataAccessContext
{
    string Role { get; }

    long? VenueId { get; }

    long? RestrictedVenueId { get; }
}
