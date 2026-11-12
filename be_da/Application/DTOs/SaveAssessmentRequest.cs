using TKSC.Domain.Exceptions;

namespace TKSC.Application.DTOs;
public sealed record SaveAssessmentRequest(long Revision, IReadOnlyList<MarkInput> Marks);
