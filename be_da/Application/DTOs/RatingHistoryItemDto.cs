namespace be_da.Application.DTOs;
public sealed record RatingHistoryItemDto(long Id, short? OldScore, short? NewScore, string? OldBand, string? NewBand, long ChangedByUserId, string ChangedByName, string ChangedByRole, long? AssessmentId, string? AssessmentType, long? SessionId, DateOnly? SessionDate, string? CourtName, AssessmentResultDto? AssessmentDetails = null);
