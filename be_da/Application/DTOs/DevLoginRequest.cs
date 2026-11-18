using be_da.Application.DTOs;

namespace be_da.Application.DTOs;
public sealed record DevLoginRequest(string Email, string? Portal = null, long? VenueId = null);
