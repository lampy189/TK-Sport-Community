using System.ComponentModel.DataAnnotations;

namespace be_da.Application.DTOs;
public sealed record SessionFilter(long? SportId = null, string? Sport = null, DateOnly? Date = null, long? TimeSlotId = null, string? Status = null, DateOnly? FromDate = null, DateOnly? ToDate = null, long? VenueId = null, string? Venue = null, string? SessionType = null);
