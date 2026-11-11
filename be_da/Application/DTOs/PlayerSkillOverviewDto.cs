namespace be_da.Application.DTOs;
public sealed record PlayerSkillOverviewDto(long PlayerId, long UserId, string FullName, string? AvatarUrl, IReadOnlyList<PlayerSportSkillProfileDto> Profiles);
