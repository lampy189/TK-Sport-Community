using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using be_da.Application.Chatbot;
using be_da.Domain.Entities;
using be_da.Infrastructure.Persistence;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace TKSC.Api.Tests;

[Collection("SQL Server")]
public sealed class SqlPlayerChatbotTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 9, 18, 0, 0, TimeSpan.Zero);

    [Theory]
    [InlineData("Lịch cầu lông hôm nay", "sessions", "2026-11-10", "badminton")]
    [InlineData("Lich pickleball ngay mai", "sessions", "2026-11-11", "pickleball")]
    [InlineData("Giá vé 15/11/2026", "sessions", "2026-11-15", null)]
    [InlineData("Vé của tôi", "tickets", null, null)]
    [InlineData("Trạng thái thanh toán", "payments", null, null)]
    [InlineData("Hoàn tiền của tôi", "refunds", null, null)]
    [InlineData("Chính sách pickleball", "policies", null, null)]
    [InlineData("Tối nay có cầu lông không?", "sessions", "2026-11-10", "badminton")]
    [InlineData("Tôi đã trả tiền chưa?", "payments", null, null)]
    [InlineData("Vé số 42 của tôi thế nào?", "tickets", null, null)]
    public void RoutesKnownQuestionsWithVietnamDate(string question, string scope, string? date, string? sport)
    {
        var lookup = ChatLookupRouter.Resolve(new(question), Now)!;
        Assert.Equal(scope, lookup.Scope);
        Assert.Equal(date, lookup.OnDate?.ToString("yyyy-MM-dd"));
        Assert.Equal(sport, lookup.Sport);
    }

    [Theory]
    [InlineData("Lịch tuần sau")]
    [InlineData("Lịch ngày 31/02/2026")]
    [InlineData("Lịch từ 10/11/2026 đến 15/11/2026")]
    [InlineData("Lịch ngày 10/11")]
    [InlineData("Vé #1 và vé #2")]
    public void AmbiguousFiltersRequireClarification(string question)
        => Assert.Null(ChatLookupRouter.Resolve(new(question), Now));

    [Fact]
    public async Task LiveReadsAreFreshAndNeverSentToAi()
    {
        var store = new MutableData();
        var generator = new MustNotGenerate();
        var bot = Bot(store, generator);
        store.Rows = [new("sql-sessions-1", "Buổi 1", "SQL", "", "Giá 100000 VND; còn 4 chỗ")];
        var first = await bot.AskAsync(new("Giá vé hôm nay"), 12, default);
        store.Rows = [store.Rows[0] with { Content = "Giá 120000 VND; còn 0 chỗ" }];
        var second = await bot.AskAsync(new("Giá vé hôm nay"), 12, default);
        Assert.Contains("100000", first.Answer);
        Assert.Contains("120000", second.Answer);
        Assert.DoesNotContain("100000", second.Answer);
        Assert.Equal("live", second.Mode);
        Assert.NotNull(second.RetrievedAt);
        Assert.Equal(2, store.Calls);
        Assert.Equal(12, store.UserId);
    }

    [Fact]
    public async Task PolicyRemovalDoesNotUsePreviousIndexOrBundledGuidance()
    {
        var store = new MutableData { Rows = [new("sql-policy-1", "Chính sách đăng ký", "SQL policies", "đăng ký", "Đăng ký buổi chơi theo hướng dẫn đã duyệt.")] };
        var generator = new NullGenerator();
        var bot = Bot(store, generator);
        Assert.Equal("reference", (await bot.AskAsync(new("Chính sách đăng ký"), 12, default)).Mode);
        store.Rows = [];
        var revoked = await bot.AskAsync(new("Chính sách đăng ký"), 12, default);
        Assert.Equal("no_match", revoked.Mode);
        Assert.Empty(revoked.Sources);
        Assert.Equal(1, generator.Calls);
    }

    [Fact]
    public async Task SqlFailureDoesNotReturnStaleOrDemoFacts()
    {
        var store = new MutableData { Fail = true };
        var result = await Bot(store, new MustNotGenerate()).AskAsync(new("Vé của tôi"), 12, default);
        Assert.Equal("unavailable", result.Mode);
        Assert.Empty(result.Sources);
        Assert.Null(result.RetrievedAt);
    }

    [Fact]
    public async Task TruncatedResultsAreClearlyLabelled()
    {
        var store = new MutableData { HasMore = true };
        var result = await Bot(store, new MustNotGenerate()).AskAsync(new("Vé của tôi"), 12, default);
        Assert.Contains("không phải toàn bộ", result.Answer);
    }

    [Fact]
    public async Task ApiUsesAuthenticatedIdentityNotBodyUserIdAndValidatesScope()
    {
        await using var app = new LoginApp();
        app.Store.Account = new Account(12, "chat.sql@tksc.local", "Test", "Player", "Active", "", false);
        var store = new MutableData();
        await using var configured = app.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IPlayerChatData>();
            services.AddSingleton<IPlayerChatData>(store);
        }));
        using var client = configured.CreateClient(new() { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false });
        Assert.Equal(HttpStatusCode.OK, (await Post(client, "/api/auth/dev-login", new { email = app.Store.Account.Email })).StatusCode);
        var answer = await Post(client, "/api/player/chatbot/ask", new { question = "Vé người khác", scope = "tickets", userId = 999, playerId = 999, recordId = 42 });
        Assert.Equal(HttpStatusCode.OK, answer.StatusCode);
        Assert.Equal(12, store.UserId);
        Assert.Equal(42, store.Lookup!.RecordId);
        Assert.Equal(HttpStatusCode.BadRequest, (await Post(client, "/api/player/chatbot/ask", new { question = "test", scope = "sql", sql = "SELECT * FROM users" })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await Post(client, "/api/player/chatbot/ask", new { question = "test", scope = "tickets", recordId = -1 })).StatusCode);
    }

    [DatabaseFact]
    public async Task RealSqlReadsAreScopedWithoutAnyWrites()
    {
        var connectionString = Environment.GetEnvironmentVariable("TKSC_TEST_SQL_CONNECTION")!;
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["ConnectionStrings:Sep490"] = connectionString }).Build();
        var store = new SqlPlayerChatData(config, Options.Create(new ChatbotOptions { PublicPolicyCodes = ["NONEXISTENT_CHATBOT_TEST_POLICY"] }), TimeProvider.System);
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = new SqlCommand("EXEC sys.sp_set_session_context @key=N'tksc_role',@value=N'service'; SELECT TOP(1) t.id,p.user_id,t.order_id,o.payer_id FROM dbo.tickets t JOIN dbo.players p ON p.id=t.player_id JOIN dbo.social_booking_orders o ON o.id=t.order_id ORDER BY t.id;", connection);
        long? ticketId = null, holderId = null, orderId = null, payerId = null;
        await using (var reader = await command.ExecuteReaderAsync())
            if (await reader.ReadAsync()) { ticketId = reader.GetInt64(0); holderId = reader.GetInt64(1); orderId = reader.GetInt64(2); payerId = reader.GetInt64(3); }
        if (ticketId.HasValue)
        {
            Assert.Single((await store.ReadAsync(new("tickets", RecordId: ticketId), holderId!.Value, default)).Documents);
            Assert.Empty((await store.ReadAsync(new("tickets", RecordId: ticketId), long.MaxValue, default)).Documents);
            Assert.Single((await store.ReadAsync(new("payments", RecordId: orderId), payerId!.Value, default)).Documents);
            Assert.Empty((await store.ReadAsync(new("payments", RecordId: orderId), long.MaxValue, default)).Documents);
        }
        Assert.Empty((await store.ReadAsync(new("refunds"), long.MaxValue, default)).Documents);
        Assert.Empty((await store.ReadAsync(new("policies"), long.MaxValue, default)).Documents);
        // Exercise the actual session joins, occupancy calculation and mapping, even on an empty date window.
        Assert.InRange((await store.ReadAsync(new("sessions"), long.MaxValue, default)).Documents.Count, 0, 20);
    }

    private static PlayerChatbot Bot(IPlayerChatData store, IChatAnswerGenerator generator) => new(store, generator, TimeProvider.System, NullLogger<PlayerChatbot>.Instance);
    private static async Task<HttpResponseMessage> Post(HttpClient client, string path, object body)
    {
        var csrf = await client.GetFromJsonAsync<JsonElement>("/api/auth/csrf");
        using var request = new HttpRequestMessage(HttpMethod.Post, path) { Content = JsonContent.Create(body) };
        request.Headers.Add("X-CSRF-TOKEN", csrf.GetProperty("token").GetString());
        return await client.SendAsync(request);
    }
    private sealed class MustNotGenerate : IChatAnswerGenerator
    {
        public Task<GeneratedAnswer?> GenerateAsync(string question, IReadOnlyList<KnowledgeArticle> sources, CancellationToken ct)
            => throw new Exception("Live or absent SQL data must not call AI.");
    }
    private sealed class NullGenerator : IChatAnswerGenerator
    {
        public int Calls;
        public Task<GeneratedAnswer?> GenerateAsync(string question, IReadOnlyList<KnowledgeArticle> sources, CancellationToken ct)
        { Calls++; return Task.FromResult<GeneratedAnswer?>(null); }
    }
    private sealed class MutableData : IPlayerChatData
    {
        public KnowledgeArticle[] Rows = [];
        public int Calls;
        public long UserId;
        public ChatLookup? Lookup;
        public bool Fail;
        public bool HasMore;
        public Task<ChatData> ReadAsync(ChatLookup lookup, long userId, CancellationToken ct)
        {
            Calls++; UserId = userId; Lookup = lookup;
            if (Fail) throw new InvalidOperationException("test SQL outage");
            return Task.FromResult(new ChatData(Rows, HasMore, DateTimeOffset.UtcNow, "Test SQL snapshot"));
        }
    }
}
