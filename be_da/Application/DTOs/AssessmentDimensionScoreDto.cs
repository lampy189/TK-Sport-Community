namespace be_da.Application.DTOs;
public sealed record AssessmentDimensionScoreDto(long DimensionId, string DimensionCode, string DimensionName, short WeightPercent, short Score, decimal WeightedPoints, string? Evidence = null);
