using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Net;
using System.Net.Http.Json;
using TKSC.Application.DTOs;
using TKSC.Application.Interfaces.Services;
using Xunit;

namespace TKSC.Api.Tests;
public class IncidentWorkflowAuthorizationTests
{
    [Theory]
    [InlineData("Staff", 200)]
    [InlineData("Manager", 200)]
    [InlineData("Owner", 200)]
    [InlineData("Player", 403)]
    [InlineData("Referee", 403)]
    [InlineData("SystemAdmin", 403)]
    public async Task StatesAreOnlyAvailableToOperationalRoles(string role, int expected)
    {
        await using var root = new SkillAuthorizationApp();
        await using var app = root.WithWebHostBuilder(b => b.ConfigureServices(s =>
        {
            s.RemoveAll<IIncidentWorkflowService>();
            s.AddSingleton<IIncidentWorkflowService, WorkflowStub>();
        }));
        using var client = app.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new("https://localhost") });
        client.DefaultRequestHeaders.Add("Test-Role", role);
        Assert.Equal((HttpStatusCode)expected, (await client.GetAsync("/api/issue-workflow")).StatusCode);
    }

    [Theory]
    [InlineData("Staff")]
    [InlineData("Manager")]
    [InlineData("Owner")]
    public async Task IssueMutationsAlwaysRequireCsrf(string role)
    {
        await using var root = new SkillAuthorizationApp();
        await using var app = root.WithWebHostBuilder(b => b.ConfigureServices(s =>
        {
            s.RemoveAll<IIncidentWorkflowService>();
            s.AddSingleton<IIncidentWorkflowService, WorkflowStub>();
        }));
        using var client = app.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new("https://localhost") });
        client.DefaultRequestHeaders.Add("Test-Role", role);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/issue-workflow/inbox/41", new IncidentAction("claim", "A valid reason for claiming", 0))).StatusCode);
    }

    public class WorkflowStub : IIncidentWorkflowService
    {
        public Task<object> States(long actor) => Task.FromResult<object>(Array.Empty<object>());
        public Task<object> Detail(long actor, string k, long id) => throw new Exception("Not called");
        public Task<object> Act(long a, string k, long id, IncidentAction i) => throw new Exception("CSRF must run first");
        public Task<object> Cases(long actor) => Task.FromResult<object>(Array.Empty<object>());
        public Task<object> Request(long actor, OperationalRequest input) => throw new Exception("Not called");
        public Task<object> Decide(long actor, long id, OperationalDecision input) => throw new Exception("Not called");
        public Task<object> RecordReturn(long actor, long id, RefundPayoutInput input) => throw new Exception("Not called");
        public Task<object> VerifyReturn(long actor, long id, string reason) => throw new Exception("Not called");
        public Task<object> Execute(long actor, long id, string reason) => throw new Exception("Not called");
        public Task<object> Close(long actor, long id, string reason) => throw new Exception("Not called");
    }
}
