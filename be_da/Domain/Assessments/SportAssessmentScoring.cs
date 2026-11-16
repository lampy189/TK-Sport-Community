using TKSC.Domain.Entities;

namespace TKSC.Domain.Assessments;

/// <summary>Approved internal club scale, independently validated for each sport and rubric.
/// Pickleball results are internal B1–B5 bands, not official DUPR ratings.</summary>
public static class SportAssessmentScoring
{
    public static bool Supports(Rubric rubric)
    {
        if (BadmintonScoring.Supports(rubric)) return true; // Retain existing pilot forms.
        string[] expected = (rubric.Sport.Code.ToLowerInvariant(), rubric.Version) switch
        {
            ("badminton", "BM-CLB-V1") => new[] { "BM_SERVE_NET", "BM_CLEAR_BACKHAND", "BM_SMASH_DEFENSE", "BM_FOOTWORK", "BM_DOUBLES" },
            ("pickleball", "PK-IPTPA-DUPR-V1") => new[] { "PK_SERVE_RETURN", "PK_THIRD_SHOT", "PK_DINK_NVZ", "PK_VOLLEY_RESET", "PK_DOUBLES_STRATEGY" },
            _ => Array.Empty<string>()
        };
        return expected.Length == 5 && rubric.RubricDimensions.Count == 5
            && rubric.RubricDimensions.All(d => d.WeightPercent == 20)
            && rubric.RubricDimensions.Select(d => d.Code).OrderBy(x => x).SequenceEqual(expected.OrderBy(x => x));
    }

    // Identical arithmetic is explicitly approved; skill dimensions and profiles stay sport-specific.
    public static short? Calculate(IEnumerable<short?> marks) => BadmintonScoring.Calculate(marks);
    public static string Band(short score) => BadmintonScoring.Band(score);
    public static short ReviewScore(short latest, short previous) => BadmintonScoring.ReviewScore(latest, previous);
}
