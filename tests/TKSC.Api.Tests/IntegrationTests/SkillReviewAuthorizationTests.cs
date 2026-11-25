using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace TKSC.Api.Tests;

public sealed class SkillReviewAuthorizationTests
{
    [Theory]
    [InlineData("Player", "appeals/1/decision")]
    [InlineData("Referee", "appeals/1/decision")]
    [InlineData("Staff", "appeals/1/decision")]
    [InlineData("Owner", "appeals/1/decision")]
    [InlineData("SystemAdmin", "appeals/1/decision")]
    [InlineData("Manager", "appeals/1/review")]
    [InlineData("Player", "exceptions/1/decision")]
    [InlineData("Staff", "exceptions/1/decision")]
    [InlineData("Owner", "exceptions/1/decision")]
    [InlineData("SystemAdmin", "exceptions/1/decision")]
    [InlineData("Referee", "sessions/1/exceptions")]
    [InlineData("Manager", "assessments/1/appeals")]
    public async Task WrongRolesCannotSubmitReviewOrDecide(string role, string path)
    {
        await using var app = new SkillAuthorizationApp();
        using var client = app.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });
        client.DefaultRequestHeaders.Add("Test-Role", role);
        var response = await client.PostAsJsonAsync($"/api/v1/skill-reviews/{path}", new { reason = "Review", correct = false });
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
    [Theory]
    [InlineData("Player", "sessions/1/exceptions")]
    [InlineData("Player", "assessments/1/appeals")]
    [InlineData("Referee", "appeals/1/review")]
    [InlineData("Referee", "exceptions/1/decision")]
    [InlineData("Manager", "appeals/1/decision")]
    public async Task AuthorizedMutationsStillRequireCsrf(string role, string path)
    {
        await using var app = new SkillAuthorizationApp();
        using var client = app.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });
        client.DefaultRequestHeaders.Add("Test-Role", role);
        client.DefaultRequestHeaders.Add("X-TKSC-Request", "1");
        var response = await client.PostAsJsonAsync($"/api/v1/skill-reviews/{path}", new { reason = "Review", correct = false });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
    [Theory]
    [InlineData("Staff")]
    [InlineData("Owner")]
    [InlineData("SystemAdmin")]
    public async Task OtherRolesCannotReadPrivateRequests(string role)
    {
        await using var app = new SkillAuthorizationApp();
        using var client = app.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });
        client.DefaultRequestHeaders.Add("Test-Role", role);
        foreach (var path in new[] { "appeals", "exceptions" })
            Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync($"/api/v1/skill-reviews/{path}")).StatusCode);
    }
}
