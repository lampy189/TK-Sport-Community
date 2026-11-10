using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using be_da.Application.Chatbot;
using be_da.Domain.Entities;
using be_da.Infrastructure.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace TKSC.Api.Tests;

public class PlayerChatbotTests
{
    [Theory]
    [InlineData("Làm sao đăng ký buổi chơi?", "booking")]
    [InlineData("lam sao dang ky buoi choi", "booking")]
    [InlineData("Tôi muốn mời bạn bè tham gia", "invitations")]
    [InlineData("Chuyển khoản rồi nhưng chưa có vé?", "payment")]
    [InlineData("Điểm pickleball có phải DUPR chính thức?", "assessment")]
    [InlineData("hoan tien ve", "refund")]
    public void BundledKnowledgeFindsRelevantVietnameseSource(string question, string id)
        => Assert.Contains((new PlayerKnowledgeIndex(TestArticles())).Search(question), a => a.Id == id);

    [Theory]
    [InlineData("Thời tiết London ngày mai?")]
    [InlineData("Viết mã Python sắp xếp mảng")]
    [InlineData("xin chào")]
    public async Task UnsupportedQuestionDoesNotCallProvider(string question)
    {
        var generator = new StubGenerator(null);
        var answer = await CreateBot(generator).AskAsync(new ChatQuestion(question, "policies"), 12, default);
        Assert.Equal("no_match", answer.Mode);
        Assert.Empty(answer.Sources);
        Assert.Equal(0, generator.Calls);
    }

    [Fact]
    public async Task MissingProviderReturnsExplicitReferenceMode()
    {
        var answer = await CreateBot(new StubGenerator(null))
            .AskAsync(new ChatQuestion("Chuyển khoản thanh toán vé", "policies"), 12, default);
        Assert.Equal("reference", answer.Mode);
        Assert.Contains(answer.Sources, s => s.Id == "payment");
        Assert.All(answer.Sources, s => Assert.Contains(s.Excerpt, answer.Answer));
    }

    [Fact]
    public async Task UnknownCitationIsNotReturnedAsGeneratedAnswer()
    {
        var answer = await CreateBot(new StubGenerator(new("invented", ["private-payments"])))
            .AskAsync(new ChatQuestion("Đăng ký buổi chơi", "policies"), 12, default);
        Assert.Equal("reference", answer.Mode);
        Assert.DoesNotContain("invented", answer.Answer);
        Assert.DoesNotContain(answer.Sources, s => s.Id == "private-payments");
    }

    [Fact]
    public async Task GeneratedAnswerUsesOnlyRetrievedCitations()
    {
        var answer = await CreateBot(new StubGenerator(new("Mở Lịch social để đăng ký.", ["booking"])))
            .AskAsync(new ChatQuestion("Đăng ký buổi chơi", "policies"), 12, default);
        Assert.Equal("generated", answer.Mode);
        Assert.Equal("booking", Assert.Single(answer.Sources).Id);
    }

    [Theory]
    [InlineData(HttpStatusCode.TooManyRequests)]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.InternalServerError)]
    public async Task ProviderFailuresFallBackWithoutLeakingResponse(HttpStatusCode status)
    {
        var handler = new ProviderStub(status, "secret upstream details");
        using var client = new HttpClient(handler);
        var generator = Generator(client);
        Assert.Null(await generator.GenerateAsync("Thanh toán", (new PlayerKnowledgeIndex(TestArticles())).Search("Thanh toán"), default));
    }

    [Fact]
    public async Task ProviderRequestIsGroundedAndDoesNotStoreConversation()
    {
        var handler = new ProviderStub(HttpStatusCode.OK, JsonSerializer.Serialize(new
        {
            choices = new[] { new { finish_reason = "stop", message = new { content = "{\"answer\":\"Mở Lịch social.\",\"sourceIds\":[\"booking\"]}" } } }
        }));
        using var client = new HttpClient(handler);
        var answer = await Generator(client).GenerateAsync("Đăng ký buổi chơi", (new PlayerKnowledgeIndex(TestArticles())).Search("Đăng ký buổi chơi"), default);
        Assert.NotNull(answer);
        Assert.Equal("booking", Assert.Single(answer.SourceIds));
        using var payload = JsonDocument.Parse(handler.Payload!);
        Assert.False(payload.RootElement.GetProperty("store").GetBoolean());
        Assert.Equal("json_schema", payload.RootElement.GetProperty("response_format").GetProperty("type").GetString());
        Assert.Contains("booking", payload.RootElement.GetProperty("messages")[1].GetProperty("content").GetString());
        Assert.DoesNotContain("unit-test-key", handler.Payload);
    }

    [Fact]
    public async Task ProviderWithNoSupportingSourceDoesNotGuess()
    {
        var answer = await CreateBot(new StubGenerator(new("Unsupported claim", [])))
            .AskAsync(new ChatQuestion("Đăng ký buổi chơi", "policies"), 12, default);
        Assert.Equal("no_match", answer.Mode);
        Assert.Empty(answer.Sources);
        Assert.DoesNotContain("Unsupported claim", answer.Answer);
    }

    [Fact]
    public async Task MissingKeyNeverCallsProvider()
    {
        var handler = new ProviderStub(HttpStatusCode.OK, "unexpected");
        using var client = new HttpClient(handler);
        var generator = new OpenAiChatAnswerGenerator(client, Options.Create(new ChatbotOptions()), NullLogger<OpenAiChatAnswerGenerator>.Instance);
        Assert.Null(await generator.GenerateAsync("Đăng ký", (new PlayerKnowledgeIndex(TestArticles())).Search("Đăng ký"), default));
        Assert.Null(handler.Payload);
    }

    [Fact]
    public async Task InvalidProviderJsonFallsBack()
    {
        using var client = new HttpClient(new ProviderStub(HttpStatusCode.OK, "not json"));
        Assert.Null(await Generator(client).GenerateAsync("Đăng ký", (new PlayerKnowledgeIndex(TestArticles())).Search("Đăng ký"), default));
    }

    [Fact]
    public async Task ChatEndpointEnforcesAuthenticationRoleCsrfAndValidation()
    {
        await using var app = new LoginApp();
        app.Store.Account = new Account(12, "chat.test@tksc.local", "Chat Player", "Player", "Active", "", false);
        using var client = app.Client();
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/player/chatbot/ask", new { question = "test" })).StatusCode);
        var login = await Post(client, "/api/auth/dev-login", new { email = app.Store.Account.Email });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/player/chatbot/ask", new { question = "test" })).StatusCode);
        foreach (var invalid in new[] { "  ", "a", new string('a', 1201) })
            Assert.Equal(HttpStatusCode.BadRequest, (await Post(client, "/api/player/chatbot/ask", new { question = invalid })).StatusCode);
        var result = await Post(client, "/api/player/chatbot/ask", new { question = "Thời tiết London?" });
        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        Assert.Equal("no_match", (await result.Content.ReadFromJsonAsync<ChatAnswer>())!.Mode);
        Assert.True(result.Headers.CacheControl!.NoStore);
        app.Store.Account = app.Store.Account with { Role = "Owner" };
        Assert.Equal(HttpStatusCode.Forbidden, (await Post(client, "/api/player/chatbot/ask", new { question = "test" })).StatusCode);
    }

    [Fact]
    public async Task ChatEndpointLimitsRequestsPerPlayer()
    {
        await using var app = new LoginApp();
        app.Store.Account = new Account(12, "chat.limit@tksc.local", "Chat Player", "Player", "Active", "", false);
        using var client = app.Client();
        Assert.Equal(HttpStatusCode.OK, (await Post(client, "/api/auth/dev-login", new { email = app.Store.Account.Email })).StatusCode);
        for (var i = 0; i < 10; i++)
            Assert.Equal(HttpStatusCode.OK, (await Post(client, "/api/player/chatbot/ask", new { question = "Thời tiết London?" })).StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests, (await Post(client, "/api/player/chatbot/ask", new { question = "Thời tiết London?" })).StatusCode);
    }

    private static OpenAiChatAnswerGenerator Generator(HttpClient client) => new(client,
        Options.Create(new ChatbotOptions { ApiKey = "unit-test-key" }), NullLogger<OpenAiChatAnswerGenerator>.Instance);

    private static KnowledgeArticle[] TestArticles()
    {
        using var stream = typeof(PlayerChatbotTests).Assembly.GetManifestResourceStream("TKSC.Api.Tests.Fixtures.player-guide.json")!;
        return JsonSerializer.Deserialize<KnowledgeArticle[]>(stream, new JsonSerializerOptions(JsonSerializerDefaults.Web))!;
    }
    private static PlayerChatbot CreateBot(IChatAnswerGenerator generator) => new(new PolicyFixture(), generator,
        TimeProvider.System, NullLogger<PlayerChatbot>.Instance);
    private sealed class PolicyFixture : IPlayerChatData
    {
        public Task<ChatData> ReadAsync(ChatLookup lookup, long userId, CancellationToken ct)
            => Task.FromResult(new ChatData(TestArticles(), false, DateTimeOffset.UtcNow, "test policies"));
    }

    private static async Task<HttpResponseMessage> Post(HttpClient client, string path, object body)
    {
        var csrf = await client.GetFromJsonAsync<JsonElement>("/api/auth/csrf");
        using var request = new HttpRequestMessage(HttpMethod.Post, path) { Content = JsonContent.Create(body) };
        request.Headers.Add("X-CSRF-TOKEN", csrf.GetProperty("token").GetString());
        return await client.SendAsync(request);
    }

    private sealed class StubGenerator(GeneratedAnswer? answer) : IChatAnswerGenerator
    {
        public int Calls { get; private set; }
        public Task<GeneratedAnswer?> GenerateAsync(string question, IReadOnlyList<KnowledgeArticle> sources, CancellationToken ct)
        { Calls++; return Task.FromResult(answer); }
    }

    private sealed class ProviderStub(HttpStatusCode status, string response) : HttpMessageHandler
    {
        public string? Payload { get; private set; }
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            Payload = await request.Content!.ReadAsStringAsync(ct);
            return new(status) { Content = new StringContent(response) };
        }
    }
}
