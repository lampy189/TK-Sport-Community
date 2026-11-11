using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using be_da.Application.Chatbot;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace TKSC.Api.Tests;

public sealed class GuestChatbotTests
{
    [Fact]
    public async Task GuestCanReadPublicInformationAndContactVenueWithoutSigningIn()
    {
        await using var app = new LoginApp();
        var data = new GuestData();
        await using var configured = app.WithWebHostBuilder(b => b.ConfigureTestServices(s =>
        { s.RemoveAll<IPlayerChatData>(); s.AddSingleton<IPlayerChatData>(data); }));
        using var client = configured.CreateClient(new() { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false });
        var sessions = await Post(client, new { question = "Lịch cầu lông hôm nay" });
        Assert.Equal(HttpStatusCode.OK, sessions.StatusCode);
        Assert.Equal("live", (await sessions.Content.ReadFromJsonAsync<ChatAnswer>())!.Mode);
        Assert.Equal(0, data.UserId);
        Assert.Equal("sessions", data.Scope);
        Assert.True(sessions.Headers.CacheControl!.NoStore);
        var policy = await Post(client, new { question = "Chính sách đặt sân" });
        Assert.Equal(HttpStatusCode.OK, policy.StatusCode);
        Assert.Equal("policies", data.Scope);
        var contact = await Post(client, new { question = "Đặt sân qua Zalo" });
        Assert.Equal("contact", (await contact.Content.ReadFromJsonAsync<ChatAnswer>())!.Mode);
        Assert.Equal(2, data.Calls);
    }

    [Theory]
    [InlineData("tickets", "Vé của tôi")]
    [InlineData("payments", "Tôi đã thanh toán chưa?")]
    [InlineData("refunds", "Hoàn tiền của tôi")]
    public async Task GuestCannotReadPrivateDataEvenWithForgedIdentity(string scope, string question)
    {
        await using var app = new LoginApp();
        var data = new GuestData();
        await using var configured = app.WithWebHostBuilder(b => b.ConfigureTestServices(s =>
        { s.RemoveAll<IPlayerChatData>(); s.AddSingleton<IPlayerChatData>(data); }));
        using var client = configured.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        foreach (var requestedScope in new[] { "auto", scope })
        {
            var response = await Post(client, new { question, scope = requestedScope, userId = 12, playerId = 12, recordId = 1 });
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var answer = (await response.Content.ReadFromJsonAsync<ChatAnswer>())!;
            Assert.Equal("login_required", answer.Mode);
            Assert.Empty(answer.Sources);
        }
        Assert.Equal(0, data.Calls);
    }

    [Fact]
    public async Task GuestStillRequiresCsrfAndHasRequestLimit()
    {
        await using var app = new LoginApp();
        using var client = app.Client();
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/chatbot/ask", new { question = "Đặt sân" })).StatusCode);
        for (var i = 0; i < 9; i++)
            Assert.Equal(HttpStatusCode.OK, (await Post(client, new { question = "Đặt sân" })).StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests, (await Post(client, new { question = "Đặt sân" })).StatusCode);
        await using var privateApp = new LoginApp();
        using var privateClient = privateApp.Client();
        Assert.Equal(HttpStatusCode.Unauthorized, (await privateClient.PostAsJsonAsync("/api/player/chatbot/ask", new { question = "Vé của tôi" })).StatusCode);
    }

    private static async Task<HttpResponseMessage> Post(HttpClient client, object body)
    {
        var csrf = await client.GetFromJsonAsync<JsonElement>("/api/auth/csrf");
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/chatbot/ask") { Content = JsonContent.Create(body) };
        request.Headers.Add("X-CSRF-TOKEN", csrf.GetProperty("token").GetString());
        return await client.SendAsync(request);
    }
    private sealed class GuestData : IPlayerChatData
    {
        public int Calls;
        public long UserId;
        public string? Scope;
        public Task<ChatData> ReadAsync(ChatLookup lookup, long userId, CancellationToken ct)
        {
            Calls++; UserId = userId; Scope = lookup.Scope;
            Assert.Contains(lookup.Scope, new[] { "sessions", "policies" });
            return Task.FromResult(new ChatData([], false, DateTimeOffset.UtcNow, "Public data"));
        }
    }
}
