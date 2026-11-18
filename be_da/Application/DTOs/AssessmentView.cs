using TKSC.Domain.Exceptions;

namespace TKSC.Application.DTOs;
public sealed record AssessmentView(long Id, long SessionId, long PlayerId, string Status, long Revision, RubricView Rubric, IReadOnlyList<MarkInput> Marks, short? TotalScore, string? BandResult, short? CurrentScore, string? CurrentBand);
