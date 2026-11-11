using TKSC.Domain.Exceptions;

namespace TKSC.Application.DTOs;
public sealed record PublishAssessmentRequest(long Revision, short? ExpectedScore, string? ExpectedBand);
