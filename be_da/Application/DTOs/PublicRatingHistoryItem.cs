namespace be_da.Application.DTOs;
public sealed record PublicRatingHistoryItem(long Id, string SportCode, string SportName, short? OldScore, short? NewScore, string? OldBand, string? NewBand, string? AssessmentType, DateOnly? SessionDate, string? CourtName);
