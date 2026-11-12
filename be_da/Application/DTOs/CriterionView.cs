using TKSC.Domain.Exceptions;

namespace TKSC.Application.DTOs;
public sealed record CriterionView(long Id, string Code, string Name, short? WeightPercent);
