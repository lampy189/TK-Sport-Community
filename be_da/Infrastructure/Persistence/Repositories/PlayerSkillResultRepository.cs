using Microsoft.EntityFrameworkCore;
using TKSC.Domain.Assessments;
using TKSC.Infrastructure.Persistence.Contexts;
using TKSC.Application.Interfaces.Repositories;

namespace TKSC.Infrastructure.Persistence.Repositories;
public sealed class PlayerSkillResultRepository(Sep490Context db) : IPlayerSkillResultRepository
{
    public async Task<object> ListAsync(long userId, long sportId, CancellationToken ct) => await db.PlayerRatingHistories.AsNoTracking().Where(h => h.PlayerSportProfile.Player.UserId == userId && h.PlayerSportProfile.SportId == sportId).Where(RatingPublicationRules.OfficialHistory).OrderByDescending(h => h.Id).Select(h => new { h.Id, h.AssessmentId, Score = h.NewScore, Band = h.NewBand, SportId = h.PlayerSportProfile.SportId }).ToListAsync(ct);
}
