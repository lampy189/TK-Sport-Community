using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TKSC.Domain.Assessments;
using TKSC.Domain.Entities;
using Xunit;

namespace TKSC.Api.Tests;

public sealed class SkillAssessmentTests
{
    [Fact]
    public void SelfSurveyHistoryDoesNotBlockFirstOfficialPublicationOrAppearAsOfficialResult()
    {
        var history = new[] {
            new PlayerRatingHistory { Id = 1, Assessment = new Assessment { AssessmentType = "SelfSurvey", Status = "Published" } },
            new PlayerRatingHistory { Id = 2, Assessment = new Assessment { AssessmentType = "on_court", SessionId = 10, Status = "draft" } }
        }.AsQueryable();
        Assert.Empty(history.Where(RatingPublicationRules.OfficialHistory));
        history.ElementAt(1).Assessment!.Status = "published";
        Assert.Equal(2, Assert.Single(history.Where(RatingPublicationRules.OfficialHistory)).Id);
    }

    [Fact]
    public void Report1Example_Produces55AndB3()
    {
        var score = BadmintonScoring.Calculate(new short?[] { 2, 2, 2, 3, 2 });
        Assert.Equal((short)55, score);
        Assert.Equal("B3", BadmintonScoring.Band(score!.Value));
    }

    [Fact]
    public void Unobserved_IsNotZero_AndCannotProduceAFullResult()
    {
        Assert.Null(BadmintonScoring.Calculate(new short?[] { 4, 4, null, 4, 4 }));
        Assert.Equal((short)0, BadmintonScoring.Calculate(new short?[] { 0, 0, 0, 0, 0 }));
    }

    [Theory]
    [InlineData(0, "B1")]
    [InlineData(19, "B1")]
    [InlineData(20, "B2")]
    [InlineData(39, "B2")]
    [InlineData(40, "B3")]
    [InlineData(59, "B3")]
    [InlineData(60, "B4")]
    [InlineData(79, "B4")]
    [InlineData(80, "B5")]
    [InlineData(100, "B5")]
    public void BandsHaveNoOverlappingBoundaries(short score, string band) =>
        Assert.Equal(band, BadmintonScoring.Band(score));

    [Theory]
    [InlineData(-1)]
    [InlineData(5)]
    public void InvalidMarksAreRejected(short mark) => Assert.Throws<ArgumentException>(() =>
        BadmintonScoring.Calculate(new short?[] { mark, 2, 2, 2, 2 }));

    [Fact]
    public void IncompleteCriterionSetIsRejected() => Assert.Throws<ArgumentException>(() =>
        BadmintonScoring.Calculate(new short?[] { 2, 2, 2, 2 }));

    [Fact]
    public void ReviewUsesBothObservationsWithExplicitHalfUpRounding() =>
        Assert.Equal((short)58, BadmintonScoring.ReviewScore(55, 60));

    [Fact]
    public void BadmintonSchemeCannotLeakIntoPickleballOrAnotherVersion()
    {
        var rubric = new Rubric { Sport = new Sport { Code = "badminton" }, Version = BadmintonScoring.RubricVersion,
            RubricDimensions = Enumerable.Range(1, 5).Select(i => new RubricDimension { Code = $"D{i}", WeightPercent = 20 }).ToList() };
        Assert.True(BadmintonScoring.Supports(rubric));
        rubric.Sport.Code = "pickleball";
        Assert.False(BadmintonScoring.Supports(rubric));
        rubric.Sport.Code = "badminton";
        rubric.Version = "other-version";
        Assert.False(BadmintonScoring.Supports(rubric));
        rubric.Version = BadmintonScoring.RubricVersion;
        rubric.RubricDimensions.First().WeightPercent = 40;
        Assert.False(BadmintonScoring.Supports(rubric));
    }

    [Theory]
    [InlineData("/api/v1/skill/sports/1/rubric")]
    [InlineData("/api/v1/skill/sessions/1/attendees")]
    [InlineData("/api/v1/skill/assessments/1")]
    [InlineData("/api/v1/skill/my-results?sportId=1")]
    public async Task AnonymousCannotReadRubricsRosterEvidenceOrResults(string path)
    {
        await using var app = new SessionApp();
        using var client = app.Client();
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync(path)).StatusCode);
    }

    [Theory]
    [InlineData("Player")]
    [InlineData("Staff")]
    [InlineData("Manager")]
    [InlineData("Owner")]
    [InlineData("SystemAdmin")]
    public async Task OtherRolesCannotOpenOrPublishAssessments(string role)
    {
        await using var app = new SkillAuthorizationApp();
        using var client = app.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });
        client.DefaultRequestHeaders.Add("Test-Role", role);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsync("/api/v1/skill/sessions/1/players/2/assessment", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsJsonAsync("/api/v1/skill/assessments/1/publish",
            new { revision = 1, expectedScore = 50, expectedBand = "B3" })).StatusCode);
    }

    [Fact]
    public async Task RefereeMutationRequiresCsrfEvenWithRequestHeader()
    {
        await using var app = new SkillAuthorizationApp();
        using var client = app.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });
        client.DefaultRequestHeaders.Add("Test-Role", "Referee");
        client.DefaultRequestHeaders.Add("X-TKSC-Request", "1");
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsync("/api/v1/skill/sessions/1/players/2/assessment", null)).StatusCode);
    }
}

internal sealed class SkillAuthorizationApp : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(
            new Dictionary<string, string?> { ["ConnectionStrings:Sep490"] =
                "Server=unused.invalid;Database=unused;Integrated Security=True;Connect Timeout=1" }));
        builder.ConfigureTestServices(services => services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = "skill-test";
            options.DefaultChallengeScheme = "skill-test";
            options.DefaultForbidScheme = "skill-test";
        }).AddScheme<AuthenticationSchemeOptions, SkillTestAuth>("skill-test", _ => { }));
    }
}

internal sealed class SkillTestAuth(IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger, UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync() => Task.FromResult(AuthenticateResult.Success(
        new AuthenticationTicket(new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "10"), new Claim(ClaimTypes.Role, Request.Headers["Test-Role"].ToString())
        }, Scheme.Name)), Scheme.Name)));
}
