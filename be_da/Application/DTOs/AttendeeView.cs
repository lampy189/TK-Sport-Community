using TKSC.Domain.Exceptions;

namespace TKSC.Application.DTOs;
public sealed record AttendeeView(long PlayerId, string Name, long? ProfileId, string? Band, string? RatingStatus, long? AssessmentId);
