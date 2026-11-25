using System.Net;
using System.Net.Http.Json;
using be_da.Application.DTOs;
using be_da.Application.Interfaces.Repositories;
using be_da.Application.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TKSC.Domain.Exceptions;
using Xunit;

namespace TKSC.Api.Tests;
public sealed class TechnicalSettingsTests
{
    [Theory]
    [InlineData("ConnectionStrings:Sep490", "Valid reason", 0)]
    [InlineData("Email:Enabled", " ", 0)]
    [InlineData("Email:Enabled", "Valid reason", -1)]
    public async Task InvalidSettingCannotReachPersistence(string key, string reason, long revision)
    {
        var store = new Store(); var service = new TechnicalSettingsService(store, new ConfigurationBuilder().Build());
        await Assert.ThrowsAsync<PaymentException>(() => service.SaveAsync(key, new(false, revision, reason), 1, default));
        Assert.Equal(0, store.Writes);
    }
    [Fact] public async Task MissingDeliveryConfigurationCannotEnableDispatch()
    {
        var store = new Store(); var service = new TechnicalSettingsService(store, new ConfigurationBuilder().Build());
        foreach (var key in new[] { "Email:Enabled", "Firebase:Enabled" })
            await Assert.ThrowsAsync<PaymentException>(() => service.SaveAsync(key, new(true, 0, "Activate"), 1, default));
        Assert.Equal(0, store.Writes);
    }
    [Fact] public async Task SavedSettingOverridesConfigurationIncludingExplicitDisable()
    {
        var store = new Store { Settings = [new("Email:Enabled", false, 15)] };
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?> { ["Email:Enabled"] = "true", ["Firebase:Enabled"] = "true" }).Build();
        var service = new TechnicalSettingsService(store, config);
        Assert.False(await service.EnabledAsync("Email:Enabled", default));
        Assert.True(await service.EnabledAsync("Firebase:Enabled", default));
        await service.SaveAsync("Email:Enabled", new(false, 15, "Pause"), 3, default);
        Assert.Equal(1, store.Writes);
        Assert.Equal(15, store.LastRevision);
    }
    [Fact] public async Task IncidentCannotIntroduceUnknownWorkflowState()
    {
        var store = new Store(); var service = new TechnicalSettingsService(store, new ConfigurationBuilder().Build());
        await Assert.ThrowsAsync<PaymentException>(() => service.SaveIncidentAsync(null, new("A", "approved", "B"), 1, default));
        Assert.Equal(0, store.Writes);
    }
    [Theory]
    [InlineData("SystemAdmin", 200)] [InlineData("Owner", 403)] [InlineData("Manager", 403)]
    [InlineData("Staff", 403)] [InlineData("Referee", 403)] [InlineData("Player", 403)]
    public async Task TechnicalConfigurationIsOnlyAccessibleToSystemAdmin(string role, int expected)
    {
        await using var root = new SkillAuthorizationApp();
        await using var app = root.WithWebHostBuilder(b => b.ConfigureServices(s => { s.RemoveAll<ITechnicalStore>(); s.AddSingleton<ITechnicalStore>(new Store()); }));
        using var client = app.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new("https://localhost") });
        client.DefaultRequestHeaders.Add("Test-Role", role);
        Assert.Equal((HttpStatusCode)expected, (await client.GetAsync("/api/technical")).StatusCode);
    }
    [Fact] public async Task TechnicalMutationsRequireCsrf()
    {
        await using var app = new SkillAuthorizationApp();
        using var client = app.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new("https://localhost") });
        client.DefaultRequestHeaders.Add("Test-Role", "SystemAdmin");
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PutAsJsonAsync("/api/technical/settings/Email:Enabled", new TechnicalSettingChange(false, 0, "Pause"))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/technical/incidents", new TechnicalIncidentChange("A", "open", "B"))).StatusCode);
    }
    private sealed class Store : ITechnicalStore
    {
        public IReadOnlyList<TechnicalSetting> Settings = [];
        public int Writes; public long LastRevision;
        public Task<IReadOnlyList<TechnicalSetting>> SettingsAsync(CancellationToken ct) => Task.FromResult(Settings);
        public Task SaveSettingAsync(string key, TechnicalSettingChange request, long actor, CancellationToken ct) { Writes++; LastRevision = request.Revision; return Task.CompletedTask; }
        public Task<object> QueuesAsync(CancellationToken ct) => Task.FromResult<object>(Array.Empty<object>());
        public Task<IReadOnlyList<TechnicalIncident>> IncidentsAsync(CancellationToken ct) => Task.FromResult<IReadOnlyList<TechnicalIncident>>([]);
        public Task SaveIncidentAsync(long? id, TechnicalIncidentChange request, long actor, CancellationToken ct) { Writes++; return Task.CompletedTask; }
    }
}
