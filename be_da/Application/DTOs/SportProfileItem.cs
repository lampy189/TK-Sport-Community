namespace be_da.Application.DTOs;
public sealed record SportProfileItem(string SportCode, string SportName, short? CurrentScore, string? CurrentBand, string? RatingStatus, long SportId = 0);
