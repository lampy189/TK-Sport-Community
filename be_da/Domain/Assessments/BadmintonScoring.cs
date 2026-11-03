using TKSC.Domain.Entities;

namespace TKSC.Domain.Assessments;

/// <summary>Report 1 Appendix A pilot scheme. Never used for pickleball.</summary>
public static class BadmintonScoring
{
    public const string RubricVersion = "badminton-pilot-r1";
    public const string ClubRubricVersion = "BM-CLB-V1";
    private static readonly string[] PilotCodes = { "D1", "D2", "D3", "D4", "D5" };
    private static readonly string[] ClubCodes = { "BM_CLEAR_BACKHAND", "BM_DOUBLES", "BM_FOOTWORK", "BM_SERVE_NET", "BM_SMASH_DEFENSE" };

    public static bool Supports(Rubric rubric) =>
        string.Equals(rubric.Sport.Code, "badminton", StringComparison.OrdinalIgnoreCase)
        && rubric.RubricDimensions.Count == 5
        && rubric.RubricDimensions.All(d => d.WeightPercent == 20)
        && ((rubric.Version == RubricVersion
                && rubric.RubricDimensions.Select(d => d.Code).OrderBy(c => c).SequenceEqual(PilotCodes))
            || (rubric.Version == ClubRubricVersion
                && rubric.RubricDimensions.Select(d => d.Code).OrderBy(c => c).SequenceEqual(ClubCodes)));

    public static short? Calculate(IEnumerable<short?> marks)
    {
        var values = marks.ToArray();
        if (values.Length != 5 || values.Any(v => v is < 0 or > 4))
            throw new ArgumentException("Cần đúng 5 tiêu chí, điểm từ 0 đến 4.");
        return values.Any(v => v is null) ? null : (short)(values.Sum(v => v!.Value) * 5);
    }

    public static string Band(short score) => score switch
    {
        < 0 or > 100 => throw new ArgumentOutOfRangeException(nameof(score)),
        < 20 => "B1", < 40 => "B2", < 60 => "B3", < 80 => "B4", _ => "B5"
    };

    public static short ReviewScore(short latest, short previous) =>
        (short)Math.Round((latest + previous) / 2m, MidpointRounding.AwayFromZero);
}
