namespace be_da.Application.DTOs;
public sealed record SportRubricDto(long Id, long SportId, string SportCode, string SportName, string Version, string Name, IReadOnlyList<RubricDimensionDto> Dimensions);
