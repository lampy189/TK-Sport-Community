namespace be_da.Application.DTOs;
public sealed record PlayerSportProfileInfo(long PlayerId, long SportId, string? CurrentBand, short? CurrentScore, string? RatingStatus);
