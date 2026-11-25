using TKSC.Domain.Assessments;
using TKSC.Domain.Entities;
using Xunit;

namespace TKSC.Api.Tests;

public sealed class SportAssessmentScoringTests
{
    [Fact]
    public void DetailedSlidersRetainHalfPointsInsteadOfRoundingEachCriterion()
    {
        Assert.Equal(14.5m, SportAssessmentScoring.CriterionPoints(3, new short[] { 7, 8, 6, 8 }));
        Assert.Equal(14m, SportAssessmentScoring.CriterionPoints(3, new short[] { 7, 7, 6, 8 }));
        Assert.Equal((short)63, SportAssessmentScoring.CalculatePoints(new decimal?[] { 14.5m, 12m, 11.5m, 13m, 12m }));
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(10, 20)]
    public void DetailedZeroAndMaximumAreValid(short value, decimal expected)
        => Assert.Equal(expected, SportAssessmentScoring.CriterionPoints(value == 0 ? (short)0 : (short)4, new short[] { value, value, value, value }));

    [Fact]
    public void RoundOnlyFinalTotalAndKeepLegacyLevelsAndUnobserved()
    {
        Assert.Equal((short)61, SportAssessmentScoring.CalculatePoints(new decimal?[] { 12.5m, 12m, 12m, 12m, 12m }));
        Assert.Equal(15m, SportAssessmentScoring.CriterionPoints(3, null));
        Assert.Null(SportAssessmentScoring.CalculatePoints(new decimal?[] { 12m, 12m, null, 12m, 12m }));
    }

    [Fact]
    public void DetailedSlidersRejectMissingOutOfRangeAndUnobservedPayloads()
    {
        Assert.Throws<ArgumentException>(() => SportAssessmentScoring.CriterionPoints(2, new short[] { 5, 5, 5 }));
        Assert.Throws<ArgumentException>(() => SportAssessmentScoring.CriterionPoints(2, new short[] { 5, 5, 5, 11 }));
        Assert.Throws<ArgumentException>(() => SportAssessmentScoring.CriterionPoints(null, new short[] { 5, 5, 5, 5 }));
    }
    private static Rubric Pickleball() => new() {
        Sport = new Sport { Code = "pickleball" }, Version = "PK-IPTPA-DUPR-V1",
        RubricDimensions = new[] { "PK_SERVE_RETURN", "PK_THIRD_SHOT", "PK_DINK_NVZ", "PK_VOLLEY_RESET", "PK_DOUBLES_STRATEGY" }
            .Select(code => new RubricDimension { Code = code, WeightPercent = 20 }).ToList()
    };
    [Fact]
    public void ApprovedPickleballUsesInternalScale()
    {
        Assert.True(SportAssessmentScoring.Supports(Pickleball()));
        var score = SportAssessmentScoring.Calculate(new short?[] { 2, 2, 2, 3, 2 });
        Assert.Equal((short)55, score);
        Assert.Equal("B3", SportAssessmentScoring.Band(score!.Value));
    }
    [Fact]
    public void ClubBadmintonAndLegacyPilotAreSupportedSeparately()
    {
        var rubric = new Rubric { Sport = new Sport { Code = "badminton" }, Version = "BM-CLB-V1",
            RubricDimensions = new[] { "BM_SERVE_NET", "BM_CLEAR_BACKHAND", "BM_SMASH_DEFENSE", "BM_FOOTWORK", "BM_DOUBLES" }
                .Select(code => new RubricDimension { Code = code, WeightPercent = 20 }).ToList() };
        Assert.True(SportAssessmentScoring.Supports(rubric));
        rubric.Version = BadmintonScoring.RubricVersion;
        Assert.False(SportAssessmentScoring.Supports(rubric));
        rubric.RubricDimensions = Enumerable.Range(1, 5).Select(i => new RubricDimension { Code = $"D{i}", WeightPercent = 20 }).ToList();
        Assert.True(SportAssessmentScoring.Supports(rubric));
    }
    [Fact]
    public void WrongSportVersionWeightsAndDuplicateDimensionsAreRejected()
    {
        var r = Pickleball(); r.Sport.Code = "badminton";
        Assert.False(SportAssessmentScoring.Supports(r));
        r = Pickleball(); r.Version = "unknown";
        Assert.False(SportAssessmentScoring.Supports(r));
        r = Pickleball(); r.RubricDimensions.First().WeightPercent = 40;
        Assert.False(SportAssessmentScoring.Supports(r));
        r = Pickleball(); r.RubricDimensions.First().Code = "PK_THIRD_SHOT";
        Assert.False(SportAssessmentScoring.Supports(r));
    }
    [Fact]
    public void UnobservedIsNotZeroAndInvalidMarksAreRejected()
    {
        Assert.Null(SportAssessmentScoring.Calculate(new short?[] { 2, 2, null, 3, 2 }));
        Assert.Equal((short)0, SportAssessmentScoring.Calculate(new short?[] { 0, 0, 0, 0, 0 }));
        Assert.Throws<ArgumentException>(() => SportAssessmentScoring.Calculate(new short?[] { 2, 2, 5, 3, 2 }));
    }
}
