using TKSC.Domain.Exceptions;

namespace TKSC.Application.DTOs;
public sealed record MarkInput(long DimensionId, short? Score, string? Evidence, IReadOnlyList<short>? SliderScores = null);
