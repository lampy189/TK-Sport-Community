using TKSC.Domain.Exceptions;

namespace TKSC.Application.DTOs;
public sealed record RubricView(long Id, long SportId, string Version, string Name, bool ScoringSupported, IReadOnlyList<CriterionView> Criteria);
