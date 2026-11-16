using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using be_da.Application.Abstractions;
using be_da.Application.Skills;
using be_da.Domain.Entities;
using be_da.Infrastructure.Persistence;
using Xunit;

namespace TKSC.Api.Tests;

public sealed class SkillProfileTests
{
    [Fact]
    public async Task GetRubrics_Anonymous_ReturnsActiveRubricsWithFiveDimensionsPerSport()
    {
        await using var app = new SkillProfileApp();
        using var client = app.Client();

        var response = await client.GetAsync("/api/skills/rubrics");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var rubrics = await response.Content.ReadFromJsonAsync<List<JsonElement>>();
        Assert.NotNull(rubrics);
        Assert.Equal(2, rubrics.Count);

        var badminton = rubrics.First(r => r.GetProperty("sportCode").GetString() == "badminton");
        Assert.Equal("BM-CLB-V1", badminton.GetProperty("version").GetString());
        Assert.Equal(5, badminton.GetProperty("dimensions").GetArrayLength());

        var pickleball = rubrics.First(r => r.GetProperty("sportCode").GetString() == "pickleball");
        Assert.Equal("PK-IPTPA-DUPR-V1", pickleball.GetProperty("version").GetString());
        Assert.Equal(5, pickleball.GetProperty("dimensions").GetArrayLength());
    }

    [Fact]
    public async Task GetMySkillProfiles_Unauthenticated_Returns401()
    {
        await using var app = new SkillProfileApp();
        using var client = app.Client();

        var response = await client.GetAsync("/api/skills/me");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task SubmitSkillSurvey_ValidAnswers_CalculatesScoreAndBandAndCreatesRatingHistory()
    {
        await using var app = new SkillProfileApp();
        var player = new Account(10, "nhat.player@example.test", "Lê Trọng Nhật", "Player", "Active", "sub-10", false);
        app.AccountStore.Accounts[player.Email] = player;
        app.AccountStore.AccountsById[player.Id] = player;

        using var client = app.Client();
        await DevLogin(client, player.Email);

        // Marks: 2, 2, 2, 3, 2 -> Sum = 11 -> TotalScore = 5 * 11 = 55 -> Band B3 (Provisional)
        var payload = new
        {
            sportId = 1L,
            experienceContext = "1-3 năm",
            frequencyContext = "2-3 buổi/tuần",
            answers = new[]
            {
                new { dimensionId = 1L, score = (short)2 },
                new { dimensionId = 2L, score = (short)2 },
                new { dimensionId = 3L, score = (short)2 },
                new { dimensionId = 4L, score = (short)3 },
                new { dimensionId = 5L, score = (short)2 }
            }
        };

        var response = await PostJsonWithCsrf(client, "/api/skills/me/survey", payload);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(body.GetProperty("success").GetBoolean());

        var profile = body.GetProperty("profile");
        Assert.Equal(55, profile.GetProperty("currentScore").GetInt16());
        Assert.Equal("B3", profile.GetProperty("currentBand").GetString());
        Assert.Equal("Provisional", profile.GetProperty("ratingStatus").GetString());

        var latest = profile.GetProperty("latestAssessment");
        Assert.Equal("SelfSurvey", latest.GetProperty("assessmentType").GetString());
        Assert.Equal(55, latest.GetProperty("totalScore").GetInt16());
        Assert.Equal(5, latest.GetProperty("scores").GetArrayLength());

        var history = profile.GetProperty("ratingHistory");
        Assert.Equal(1, history.GetArrayLength());
        Assert.Equal(55, history[0].GetProperty("newScore").GetInt16());
        Assert.Equal("B3", history[0].GetProperty("newBand").GetString());
    }

    [Fact]
    public async Task SubmitSkillSurvey_WhenProfileAlreadyRefereeRated_Returns409Conflict()
    {
        await using var app = new SkillProfileApp();
        var player = new Account(11, "rated.player@example.test", "Player Rated", "Player", "Active", "sub-11", false);
        app.AccountStore.Accounts[player.Email] = player;
        app.AccountStore.AccountsById[player.Id] = player;
        app.SkillStore.RatedSports.Add((player.Id, 2L));

        using var client = app.Client();
        await DevLogin(client, player.Email);

        var payload = new
        {
            sportId = 2L,
            answers = new[]
            {
                new { dimensionId = 6L, score = (short)3 },
                new { dimensionId = 7L, score = (short)3 },
                new { dimensionId = 8L, score = (short)3 },
                new { dimensionId = 9L, score = (short)3 },
                new { dimensionId = 10L, score = (short)3 }
            }
        };

        var response = await PostJsonWithCsrf(client, "/api/skills/me/survey", payload);
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        var err = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("ALREADY_RATED", err.GetProperty("code").GetString());
    }

    private static async Task DevLogin(HttpClient client, string email)
    {
        var token = await client.GetFromJsonAsync<JsonElement>("/api/auth/csrf");
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/dev-login");
        request.Headers.Add("X-CSRF-TOKEN", token.GetProperty("token").GetString());
        request.Content = JsonContent.Create(new { Email = email });
        var res = await client.SendAsync(request);
        res.EnsureSuccessStatusCode();
    }

    private static async Task<HttpResponseMessage> PostJsonWithCsrf(HttpClient client, string path, object payload)
    {
        var token = await client.GetFromJsonAsync<JsonElement>("/api/auth/csrf");
        using var request = new HttpRequestMessage(HttpMethod.Post, path);
        request.Headers.Add("X-CSRF-TOKEN", token.GetProperty("token").GetString());
        request.Content = JsonContent.Create(payload);
        return await client.SendAsync(request);
    }
}

internal sealed class SkillProfileApp : WebApplicationFactory<Program>
{
    public FakeSessionAccounts AccountStore { get; } = new();
    public FakeSkillProfileStore SkillStore { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Authentication:Google:ClientId"] = "test-client",
            ["Authentication:Google:ClientSecret"] = "test-secret"
        }));
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IAccountStore>();
            services.AddSingleton<IAccountStore>(AccountStore);
            services.RemoveAll<ISkillProfileStore>();
            services.AddSingleton<ISkillProfileStore>(SkillStore);
            services.PostConfigure<GoogleOptions>(GoogleDefaults.AuthenticationScheme,
                options => options.Backchannel = new HttpClient(new GoogleStub(() => true)));
        });
    }

    public HttpClient Client() => CreateClient(new WebApplicationFactoryClientOptions
    { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false, HandleCookies = true });
}

internal sealed class FakeSkillProfileStore : ISkillProfileStore
{
    public List<SportRubricDto> Rubrics { get; } = new()
    {
        new(1, 1, "badminton", "Badminton", "BM-CLB-V1", "Khung đánh giá Cầu lông Phong trào VN", new List<RubricDimensionDto>
        {
            new(1, "BM_SERVE_NET", "Giao cầu, Trả giao & Xử lý trên lưới", 20),
            new(2, "BM_CLEAR_BACKHAND", "Phông cầu cuối sân (Clear) & Trái tay (Backhand)", 20),
            new(3, "BM_SMASH_DEFENSE", "Đập cầu (Smash), Chặt cầu (Drop) & Phòng thủ", 20),
            new(4, "BM_FOOTWORK", "Bộ pháp di chuyển 6 góc sân & Hồi vị", 20),
            new(5, "BM_DOUBLES", "Đội hình Đánh đôi (Công/Thủ) & Độ bền Rally", 20)
        }),
        new(2, 2, "pickleball", "Pickleball", "PK-IPTPA-DUPR-V1", "Khung đánh giá Pickleball chuẩn IPTPA & DUPR", new List<RubricDimensionDto>
        {
            new(6, "PK_SERVE_RETURN", "Giao bóng (Serve) & Trả giao bóng sâu (Return)", 20),
            new(7, "PK_THIRD_SHOT", "Cú đánh thứ 3 (3rd Shot Drop/Drive) & Lên lưới", 20),
            new(8, "PK_DINK_NVZ", "Kỹ thuật Dink & Kiểm soát vạch Kitchen (NVZ)", 20),
            new(9, "PK_VOLLEY_RESET", "Bắt Volley, Đấu tay đôi (Hand Battle) & Reset bóng", 20),
            new(10, "PK_DOUBLES_STRATEGY", "Groundstroke 2 tay & Chiến thuật Đánh đôi (Stacking)", 20)
        })
    };

    public HashSet<(long UserId, long SportId)> RatedSports { get; } = new();
    public Dictionary<(long UserId, long SportId), PlayerSportSkillProfileDto> SavedProfiles { get; } = new();

    public Task<IReadOnlyList<SportRubricDto>> GetActiveRubricsAsync(long? sportId, CancellationToken cancellationToken)
    {
        var list = sportId.HasValue ? Rubrics.Where(r => r.SportId == sportId.Value).ToList() : Rubrics;
        return Task.FromResult<IReadOnlyList<SportRubricDto>>(list);
    }

    public Task<PlayerSkillOverviewDto?> GetPlayerSkillOverviewAsync(long userId, CancellationToken cancellationToken)
    {
        var profiles = Rubrics.Select(r =>
            SavedProfiles.TryGetValue((userId, r.SportId), out var p)
                ? p
                : new PlayerSportSkillProfileDto(null, r.SportId, r.SportCode, r.SportName, null, null, "Unrated", r, null, Array.Empty<RatingHistoryItemDto>())
        ).ToList();

        return Task.FromResult<PlayerSkillOverviewDto?>(
            new PlayerSkillOverviewDto(userId, userId, "Test Player", null, profiles));
    }

    public Task<SubmitSkillSurveyResult> SubmitSelfAssessmentAsync(long userId, SubmitSkillSurveyRequest request, CancellationToken cancellationToken)
    {
        if (RatedSports.Contains((userId, request.SportId)))
        {
            return Task.FromResult(new SubmitSkillSurveyResult(false, "ALREADY_RATED", "Đã được Trọng tài xác nhận."));
        }

        var rubric = Rubrics.FirstOrDefault(r => r.SportId == request.SportId);
        if (rubric == null)
        {
            return Task.FromResult(new SubmitSkillSurveyResult(false, "RUBRIC_NOT_FOUND", "Không tìm thấy rubric."));
        }

        short sum = (short)request.Answers.Sum(a => a.Score);
        short total = (short)Math.Clamp(sum * 5, 0, 100);
        string band = SqlSkillProfileStore.MapScoreToBand(total);

        var scores = rubric.Dimensions.Select(d =>
        {
            var mark = request.Answers.First(a => a.DimensionId == d.Id).Score;
            return new AssessmentDimensionScoreDto(d.Id, d.Code, d.Name, d.WeightPercent, mark, (short)(mark * 5));
        }).ToList();

        var latest = new AssessmentResultDto(
            Id: 100,
            RubricId: rubric.Id,
            RubricName: rubric.Name,
            RubricVersion: rubric.Version,
            AssessmentType: "SelfSurvey",
            Status: "Published",
            TotalScore: total,
            BandResult: band,
            AssessorId: userId,
            AssessorName: "Test Player",
            AssessorRole: "Player",
            SessionId: null,
            SessionDate: null,
            CourtName: null,
            TimeLabel: null,
            Scores: scores
        );

        var history = new List<RatingHistoryItemDto>
        {
            new(1, null, total, null, band, userId, "Test Player", "Player", 100, "SelfSurvey", null, null, null)
        };

        var profile = new PlayerSportSkillProfileDto(
            ProfileId: 1,
            SportId: rubric.SportId,
            SportCode: rubric.SportCode,
            SportName: rubric.SportName,
            CurrentScore: total,
            CurrentBand: band,
            RatingStatus: "Provisional",
            ActiveRubric: rubric,
            LatestAssessment: latest,
            RatingHistory: history
        );

        SavedProfiles[(userId, rubric.SportId)] = profile;
        return Task.FromResult(new SubmitSkillSurveyResult(true, null, "OK", profile));
    }
}
