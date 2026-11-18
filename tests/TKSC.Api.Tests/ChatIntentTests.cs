using be_da.Application.DTOs;
using be_da.Application.Interfaces.External;
using be_da.Application.Interfaces.Repositories;
using System.Net;
using System.Text.Json;
using be_da.Application.Services.Chatbot;
using be_da.Infrastructure.ExternalServices;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace TKSC.Api.Tests;

public sealed class ChatIntentTests
{
    [Fact]
    public async Task SemanticIntentTakesPriorityEvenWithoutLegacyKeywords()
    {
        var data = new CaptureData();
        var interpreter = new Interpreter(new("payments", null, null, null, "Kiểm tra tiền thanh toán đơn social của tôi", null));
        var result = await Bot(data, interpreter).AskAsync(new("bên mình nhận dc khoản em gửi chưa ạ"), 12, default);
        Assert.Equal("payments", data.Lookup!.Scope);
        Assert.Equal(12, data.UserId);
        Assert.Equal("live", result.Mode);
        Assert.Equal(interpreter.Result!.Question, result.Context!.Question);
    }

    [Fact]
    public async Task FollowUpReceivesPreviousIntentAndQueriesNewDate()
    {
        var data = new CaptureData();
        var interpreter = new Interpreter(new("sessions", "2026-11-11", "badminton", null, "Lịch cầu lông ngày 11/11/2026 từ 18:00", null, "18:00", null));
        var context = new ChatContext("Tìm buổi cầu lông ngày 10/11/2026 từ 18:00");
        var result = await Bot(data, interpreter).AskAsync(new("còn mai thì sao?", Context: context), 0, default);
        Assert.Equal(context, interpreter.Request!.Context);
        Assert.Equal(new DateOnly(2026, 11, 11), data.Lookup!.OnDate);
        Assert.Equal(new TimeOnly(18, 0), data.Lookup.StartFrom);
        Assert.Equal("badminton", data.Lookup.Sport);
        Assert.Equal(0, data.UserId);
        Assert.Equal("sessions", result.Scope);
    }

    [Fact]
    public async Task AiCannotGrantGuestAccessToPrivateSql()
    {
        var data = new CaptureData();
        var interpreter = new Interpreter(new("tickets", null, null, 88, "Kiểm tra vé 88", null));
        var result = await Bot(data, interpreter).AskAsync(new("suất em mua dùng được chứ"), 0, default);
        Assert.Equal("login_required", result.Mode);
        Assert.Equal(0, data.Calls);
    }

    [Fact]
    public async Task ClarificationKeepsOnlyQuestionContextAndDoesNotReadSql()
    {
        var data = new CaptureData();
        var interpreter = new Interpreter(new("clarify", null, null, null, "Tìm lịch cầu lông lúc 7 giờ", "Bạn muốn chơi lúc 7 giờ sáng hay 7 giờ tối?"));
        var result = await Bot(data, interpreter).AskAsync(new("7h có kèo nào ko"), 0, default);
        Assert.Equal("clarification", result.Mode);
        Assert.Equal(interpreter.Result!.Clarification, result.Answer);
        Assert.NotNull(result.Context!.Clarification);
        Assert.Equal(0, data.Calls);
    }

    [Theory]
    [InlineData("SELECT * FROM users", "2026-11-11", null)]
    [InlineData("sessions", "2026-02-31", null)]
    [InlineData("sessions", null, "football")]
    public async Task InvalidModelOutputCannotReachSql(string intent, string? date, string? sport)
    {
        var data = new CaptureData();
        var result = await Bot(data, new Interpreter(new(intent, date, sport, null, "test", null))).AskAsync(new("test"), 12, default);
        Assert.Equal("clarification", result.Mode);
        Assert.Equal(0, data.Calls);
    }

    [Fact]
    public async Task SemanticContactDoesNotNeedLiteralBookingKeywords()
    {
        var data = new CaptureData();
        var result = await Bot(data, new Interpreter(new("contact", null, null, null, "Xin liên hệ cơ sở để thuê sân", null)))
            .AskAsync(new("nói chuyện với bên mình ở đâu để thuê vậy"), 0, default);
        Assert.Equal("contact", result.Mode);
        Assert.NotNull(result.BookingContact);
        Assert.Equal(0, data.Calls);
    }

    [Fact]
    public async Task MissingAiIsExplicitAndDoesNotGuessFollowUpContext()
    {
        var data = new CaptureData();
        var bot = Bot(data, new Interpreter(null));
        var first = await bot.AskAsync(new("Vé của tôi"), 12, default);
        Assert.NotNull(first.Notice);
        var calls = data.Calls;
        var second = await bot.AskAsync(new("còn hôm sau", Context: new("Lịch ngày 10/11/2026")), 12, default);
        Assert.Equal("unavailable", second.Mode);
        Assert.Equal(calls, data.Calls);
    }

    [Fact]
    public async Task ProviderRequestUsesStructuredOutputAndVietnamTimeButNoDatabaseRows()
    {
        var handler = new ProviderHandler("""{"intent":"sessions","date":"2026-11-11","sport":"badminton","recordId":null,"question":"Lịch cầu lông ngày 11/11/2026","clarification":null,"startFrom":null,"startBefore":null}""");
        using var http = new HttpClient(handler);
        var interpreter = new OpenAiChatIntentInterpreter(http, Options.Create(new ChatbotOptions { ApiKey = "fake-test-key" }), NullLogger<OpenAiChatIntentInterpreter>.Instance);
        var result = await interpreter.InterpretAsync(new("còn mai?", Context: new("Lịch cầu lông hôm nay")), new(2026, 11, 10, 0, 0, 0, TimeSpan.Zero), default);
        Assert.Equal("sessions", result!.Intent);
        using var json = JsonDocument.Parse(handler.Body!);
        Assert.False(json.RootElement.GetProperty("store").GetBoolean());
        Assert.Equal("json_schema", json.RootElement.GetProperty("response_format").GetProperty("type").GetString());
        using var user = JsonDocument.Parse(json.RootElement.GetProperty("messages")[1].GetProperty("content").GetString()!);
        Assert.Contains("+07:00", user.RootElement.GetProperty("vietnamNow").GetString());
        Assert.Equal(3, user.RootElement.EnumerateObject().Count());
        Assert.Equal("Lịch cầu lông hôm nay", user.RootElement.GetProperty("context").GetProperty("Question").GetString());
        Assert.DoesNotContain("fake-test-key", handler.Body);
    }

    [Fact]
    public async Task MalformedProviderReplyFallsBackWithoutThrowing()
    {
        using var http = new HttpClient(new ProviderHandler("not-json"));
        var interpreter = new OpenAiChatIntentInterpreter(http, Options.Create(new ChatbotOptions { ApiKey = "fake-test-key" }), NullLogger<OpenAiChatIntentInterpreter>.Instance);
        Assert.Null(await interpreter.InterpretAsync(new("test"), DateTimeOffset.UtcNow, default));
    }

    private static PlayerChatbot Bot(IPlayerChatData data, IChatIntentInterpreter interpreter)
        => new(data, new NoGenerator(), TimeProvider.System, NullLogger<PlayerChatbot>.Instance, interpreter: interpreter);
    private sealed class NoGenerator : IChatAnswerGenerator
    {
        public Task<GeneratedAnswer?> GenerateAsync(string q, IReadOnlyList<KnowledgeArticle> s, CancellationToken ct) => throw new Exception("Unexpected generation.");
    }
    private sealed class CaptureData : IPlayerChatData
    {
        public ChatLookup? Lookup; public long UserId; public int Calls;
        public Task<ChatData> ReadAsync(ChatLookup lookup, long userId, CancellationToken ct)
        { Lookup = lookup; UserId = userId; Calls++; return Task.FromResult(new ChatData([], false, DateTimeOffset.UtcNow, "Test snapshot")); }
    }
    private sealed class Interpreter(ChatIntent? result) : IChatIntentInterpreter
    {
        public ChatIntent? Result = result; public ChatQuestion? Request;
        public Task<ChatIntent?> InterpretAsync(ChatQuestion request, DateTimeOffset now, CancellationToken ct)
        { Request = request; return Task.FromResult(Result); }
    }
    private sealed class ProviderHandler(string content) : HttpMessageHandler
    {
        public string? Body;
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            Body = await request.Content!.ReadAsStringAsync(ct);
            return new(HttpStatusCode.OK) { Content = new StringContent(JsonSerializer.Serialize(new { choices = new[] { new { finish_reason = "stop", message = new { content } } } })) };
        }
    }
}
