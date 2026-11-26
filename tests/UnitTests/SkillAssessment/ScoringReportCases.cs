using System.Text.Json;
using TKSC.Domain.Assessments;
using Xunit;
using Xunit.Abstractions;

namespace TKSC.Api.Tests.UnitTests.SkillAssessment;

public sealed class ScoringReportCases(ITestOutputHelper output)
{
    [Fact]
    public void UTC001_FourSliders() =>
        Check("SportAssessmentScoring.CriterionPoints", "level=3; sliders=[7, 8, 6, 8]", 14.5m, () => SportAssessmentScoring.CriterionPoints(3, [7, 8, 6, 8]));

    [Fact]
    public void UTC002_DifferentSliderValues() =>
        Check("SportAssessmentScoring.CriterionPoints", "level=3; sliders=[7, 7, 6, 8]", 14m, () => SportAssessmentScoring.CriterionPoints(3, [7, 7, 6, 8]));

    [Fact]
    public void UTC003_BalancedSliders() =>
        Check("SportAssessmentScoring.CriterionPoints", "level=2; sliders=[5, 5, 5, 5]", 10m, () => SportAssessmentScoring.CriterionPoints(2, [5, 5, 5, 5]));

    [Fact]
    public void UTC004_MinimumSliderValues() =>
        Check("SportAssessmentScoring.CriterionPoints", "level=0; sliders=[0, 0, 0, 0]", 0m, () => SportAssessmentScoring.CriterionPoints(0, [0, 0, 0, 0]));

    [Fact]
    public void UTC005_MaximumSliderValues() =>
        Check("SportAssessmentScoring.CriterionPoints", "level=4; sliders=[10, 10, 10, 10]", 20m, () => SportAssessmentScoring.CriterionPoints(4, [10, 10, 10, 10]));

    [Fact]
    public void UTC006_LegacyLevelTwo() =>
        Check("SportAssessmentScoring.CriterionPoints", "level=2; sliders=null", 10m, () => SportAssessmentScoring.CriterionPoints(2, null));

    [Fact]
    public void UTC007_LegacyLevelThree() =>
        Check("SportAssessmentScoring.CriterionPoints", "level=3; sliders=null", 15m, () => SportAssessmentScoring.CriterionPoints(3, null));

    [Fact]
    public void UTC008_NegativeSlider() =>
        CheckException("SportAssessmentScoring.CriterionPoints", "level=2; sliders=[5, -1, 6, 7]", () => SportAssessmentScoring.CriterionPoints(2, [5, -1, 6, 7]));

    [Fact]
    public void UTC009_SliderAboveMaximum() =>
        CheckException("SportAssessmentScoring.CriterionPoints", "level=2; sliders=[5, 11, 6, 7]", () => SportAssessmentScoring.CriterionPoints(2, [5, 11, 6, 7]));

    [Fact]
    public void UTC010_MissingSlider() =>
        CheckException("SportAssessmentScoring.CriterionPoints", "level=2; sliders=[5, 6, 7]", () => SportAssessmentScoring.CriterionPoints(2, [5, 6, 7]));

    [Fact]
    public void UTC011_FractionalTotal() =>
        Check<short?>("SportAssessmentScoring.CalculatePoints", "points=[14.5, 12, 11.5, 13, 12]", 63, () => SportAssessmentScoring.CalculatePoints([14.5m, 12m, 11.5m, 13m, 12m]));

    [Fact]
    public void UTC012_RoundFinalMidpoint() =>
        Check<short?>("SportAssessmentScoring.CalculatePoints", "points=[10.5, 10, 10, 10, 10]", 51, () => SportAssessmentScoring.CalculatePoints([10.5m, 10m, 10m, 10m, 10m]));

    [Fact]
    public void UTC013_ZeroTotal() =>
        Check<short?>("SportAssessmentScoring.CalculatePoints", "points=[0, 0, 0, 0, 0]", 0, () => SportAssessmentScoring.CalculatePoints([0m, 0m, 0m, 0m, 0m]));

    [Fact]
    public void UTC014_CriterionAboveMaximum() =>
        CheckException("SportAssessmentScoring.CalculatePoints", "points=[20.5, 12, 10, 14, 15]", () => SportAssessmentScoring.CalculatePoints([20.5m, 12m, 10m, 14m, 15m]));

    [Fact]
    public void UTC015_UnobservedCriterion() =>
        Check<short?>("SportAssessmentScoring.CalculatePoints", "points=[14.5, 12, null, 13, 12]", (short?)null, () => SportAssessmentScoring.CalculatePoints([14.5m, 12m, null, 13m, 12m]));

    [Fact]
    public void UTC016_BandB2() =>
        Check("BadmintonScoring.Band", "score=35", "B2", () => BadmintonScoring.Band(35));

    [Fact]
    public void UTC017_BandB4() =>
        Check("BadmintonScoring.Band", "score=75", "B4", () => BadmintonScoring.Band(75));

    private void Check<T>(string method, string input, T expected, Func<T> run)
    {
        output.WriteLine($"Method: {method}");
        output.WriteLine($"Input: {input}");
        output.WriteLine($"Expected: {JsonSerializer.Serialize(expected)}");
        var actual = run();
        output.WriteLine($"Actual: {JsonSerializer.Serialize(actual)}");
        Assert.Equal(expected, actual);
    }

    private void CheckException(string method, string input, Func<object?> run)
    {
        output.WriteLine($"Method: {method}");
        output.WriteLine($"Input: {input}");
        output.WriteLine("Expected: ArgumentException");
        object? value = null;
        var error = Record.Exception(() => { value = run(); });
        output.WriteLine(error is null
            ? $"Actual: Returned {JsonSerializer.Serialize(value)}"
            : $"Actual: {error.GetType().Name}: {error.Message}");
        Assert.IsType<ArgumentException>(error);
    }
}
