using TKSC.Domain.Exceptions;

namespace TKSC.Application.DTOs;
public sealed record AssessmentEvidence(DateTimeOffset At, IReadOnlyList<MarkInput> Marks);
