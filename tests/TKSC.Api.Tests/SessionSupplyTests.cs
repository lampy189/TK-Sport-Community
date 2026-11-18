using be_da.Application.DTOs;
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
using be_da.Application.Interfaces.Repositories;
using be_da.Application.Validators;
using be_da.Domain.Entities;
using Xunit;

namespace TKSC.Api.Tests;

public sealed class SessionSupplyTests
{
    private static readonly Account Staff = new(20, "staff@example.test", "Staff One", "Staff", "Active", "sub-20", false, VenueId: 1);
    private static readonly Account Manager = new(21, "manager@example.test", "Manager One", "Manager", "Active", "sub-21", false, VenueId: 1);
    private static readonly Account Player = new(30, "player@example.test", "Player One", "Player", "Active", "sub-30", false);
    private static readonly Account Referee = new(40, "referee@example.test", "Referee One", "Referee", "Active", "sub-40", false, VenueId: 1);

    [Fact]
    public async Task Supplies_AreHiddenFromPlayers_AndOnlyStaffWrites()
    {
        await using var app = new SupplyApp();
        using var anonymous = app.Client();
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync("/api/stock/session-supplies")).StatusCode);

        using var player = app.Client();
        await DevLogin(player, Player.Email);
        Assert.Equal(HttpStatusCode.Forbidden, (await player.GetAsync("/api/stock/session-supplies")).StatusCode);

        using var manager = app.Client();
        await DevLogin(manager, Manager.Email);
        Assert.Equal(HttpStatusCode.OK, (await manager.GetAsync("/api/stock/session-supplies/5")).StatusCode);
        var issue = await Send(manager, "/api/stock/session-supplies/5/issue", new { productId = 1, quantity = 2, recipientId = 9 });
        Assert.Equal(HttpStatusCode.Forbidden, issue.StatusCode);
        Assert.Equal(0, app.Store.IssueCalls);
    }

    [Fact]
    public async Task Issue_PassesOperatorAndMapsStoreConflicts()
    {
        await using var app = new SupplyApp();
        using var client = app.Client();
        await DevLogin(client, Staff.Email);

        var ok = await Send(client, "/api/stock/session-supplies/5/issue", new { productId = 1, quantity = 12, recipientId = 9 });
        Assert.Equal(HttpStatusCode.OK, ok.StatusCode);
        Assert.Equal(Staff.Id, app.Store.LastOperator);
        var allocation = (await ok.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("allocations")[0];
        Assert.Equal(12, allocation.GetProperty("issued").GetInt32());
        Assert.Equal(12, allocation.GetProperty("outstanding").GetInt32());

        app.Store.NextFailure = "INSUFFICIENT_STOCK";
        var conflict = await Send(client, "/api/stock/session-supplies/5/issue", new { productId = 1, quantity = 999, recipientId = 9 });
        Assert.Equal(HttpStatusCode.Conflict, conflict.StatusCode);
    }

    [Theory]
    [InlineData(0, 1, 9)]
    [InlineData(1, 0, 9)]
    [InlineData(1, 1, 0)]
    public async Task Issue_RejectsMissingFields(long productId, int quantity, long recipientId)
    {
        await using var app = new SupplyApp();
        using var client = app.Client();
        await DevLogin(client, Staff.Email);

        var response = await Send(client, "/api/stock/session-supplies/5/issue", new { productId, quantity, recipientId });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(0, app.Store.IssueCalls);
    }

    [Fact]
    public async Task Reconcile_ComputesUsedFromReturned_AndRequiresCsrf()
    {
        await using var app = new SupplyApp();
        using var client = app.Client();
        await DevLogin(client, Staff.Email);
        await Send(client, "/api/stock/session-supplies/5/issue", new { productId = 1, quantity = 12, recipientId = 9 });

        var noCsrf = await client.PostAsJsonAsync("/api/stock/session-supplies/5/products/1/reconcile", new { returnedQuantity = 3 });
        Assert.Equal(HttpStatusCode.BadRequest, noCsrf.StatusCode);

        var response = await Send(client, "/api/stock/session-supplies/5/products/1/reconcile", new { returnedQuantity = 3 });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var allocation = (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("allocations")[0];
        Assert.True(allocation.GetProperty("isReconciled").GetBoolean());
        Assert.Equal(9, allocation.GetProperty("used").GetInt32());
        Assert.Equal(0, allocation.GetProperty("outstanding").GetInt32());
    }

    [Fact]
    public async Task Referee_SeesOnlyAssignedSessions_AndReportsUseWithoutClosing()
    {
        await using var app = new SupplyApp();
        using var staff = app.Client();
        await DevLogin(staff, Staff.Email);
        await Send(staff, "/api/stock/session-supplies/5/issue", new { productId = 1, quantity = 12, recipientId = 9 });

        using var referee = app.Client();
        await DevLogin(referee, Referee.Email);
        Assert.Empty((await referee.GetFromJsonAsync<List<JsonElement>>("/api/stock/referee-supplies"))!);
        Assert.Equal(HttpStatusCode.NotFound, (await referee.GetAsync("/api/stock/referee-supplies/5")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await referee.GetAsync("/api/stock/session-supplies")).StatusCode);

        app.Store.AssignedReferees.Add(Referee.Id);
        Assert.Single((await referee.GetFromJsonAsync<List<JsonElement>>("/api/stock/referee-supplies"))!);
        var tooMany = await Send(referee, "/api/stock/referee-supplies/5/products/1/usage", new { usedQuantity = 13 });
        Assert.Equal(HttpStatusCode.Conflict, tooMany.StatusCode);

        var reported = await Send(referee, "/api/stock/referee-supplies/5/products/1/usage", new { usedQuantity = 9 });
        Assert.Equal(HttpStatusCode.OK, reported.StatusCode);
        var allocation = (await reported.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("allocations")[0];
        Assert.Equal(9, allocation.GetProperty("reportedUsed").GetInt32());
        Assert.False(allocation.GetProperty("isReconciled").GetBoolean());
        Assert.Equal(12, allocation.GetProperty("outstanding").GetInt32());
    }

    // Task 13/11/2026: referee request -> Staff issues (or rejects) -> the request cannot be handled twice.
    [Fact]
    public async Task SupplyRequest_RefereeAsks_StaffIssuesOnce()
    {
        await using var app = new SupplyApp();
        app.Store.AssignedReferees.Add(Referee.Id);
        using var referee = app.Client();
        await DevLogin(referee, Referee.Email);
        Assert.Equal(HttpStatusCode.BadRequest, (await Send(referee, "/api/stock/referee-supplies/5/requests", new { productId = 1, quantity = 0 })).StatusCode);
        var created = await Send(referee, "/api/stock/referee-supplies/5/requests", new { productId = 1, quantity = 12, note = "Ca tối" });
        Assert.Equal(HttpStatusCode.OK, created.StatusCode);
        Assert.Equal(Referee.Id, app.Store.LastRequester);
        Assert.Equal(HttpStatusCode.Forbidden, (await Send(referee, "/api/stock/session-supplies/5/requests/1/issue", new { })).StatusCode);

        using var staff = app.Client();
        await DevLogin(staff, Staff.Email);
        var issued = await Send(staff, "/api/stock/session-supplies/5/requests/1/issue", new { });
        Assert.Equal(HttpStatusCode.OK, issued.StatusCode);
        var body = await issued.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Issued", body.GetProperty("requests")[0].GetProperty("status").GetString());
        Assert.Equal(12, body.GetProperty("allocations")[0].GetProperty("issued").GetInt32());
        Assert.Equal(Staff.Id, app.Store.LastOperator);
        Assert.Equal(HttpStatusCode.Conflict, (await Send(staff, "/api/stock/session-supplies/5/requests/1/issue", new { })).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await Send(staff, "/api/stock/session-supplies/5/requests/1/reject", new { note = "x" })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await Send(staff, "/api/stock/session-supplies/5/requests/99/issue", new { })).StatusCode);
    }

    [Fact]
    public async Task SupplyRequest_RejectNeedsReason_AndRefereeCancelsOwnPending()
    {
        await using var app = new SupplyApp();
        app.Store.AssignedReferees.Add(Referee.Id);
        using var referee = app.Client();
        await DevLogin(referee, Referee.Email);
        await Send(referee, "/api/stock/referee-supplies/5/requests", new { productId = 1, quantity = 4 });
        await Send(referee, "/api/stock/referee-supplies/5/requests", new { productId = 1, quantity = 2 });

        using var staff = app.Client();
        await DevLogin(staff, Staff.Email);
        Assert.Equal(HttpStatusCode.BadRequest, (await Send(staff, "/api/stock/session-supplies/5/requests/1/reject", new { note = " " })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await Send(staff, "/api/stock/session-supplies/5/requests/1/reject", new { note = "Hết hàng" })).StatusCode);

        var cancelled = await Send(referee, "/api/stock/referee-supplies/5/requests/2/cancel", new { });
        Assert.Equal(HttpStatusCode.OK, cancelled.StatusCode);
        var statuses = (await cancelled.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("requests").EnumerateArray().Select(r => r.GetProperty("status").GetString()).ToList();
        Assert.Equal(["Rejected", "Cancelled"], statuses);
        Assert.Equal(0, app.Store.IssueCalls);
    }

    [Fact]
    public async Task GoodsHandover_RequiresStaff()
    {
        await using var app = new SupplyApp();
        using var anonymous = app.Client();
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.PostAsync("/api/v1/commerce/goods-orders/1/deliver", null)).StatusCode);

        using var player = app.Client();
        await DevLogin(player, Player.Email);
        Assert.Equal(HttpStatusCode.Forbidden, (await Send(player, "/api/v1/commerce/goods-orders/1/receive", new { })).StatusCode);
    }

    private static async Task DevLogin(HttpClient client, string email)
    {
        var res = await Send(client, "/api/auth/dev-login", new { Email = email });
        res.EnsureSuccessStatusCode();
    }

    private static async Task<HttpResponseMessage> Send(HttpClient client, string path, object body)
    {
        var token = await client.GetFromJsonAsync<JsonElement>("/api/auth/csrf");
        using var request = new HttpRequestMessage(HttpMethod.Post, path);
        request.Headers.Add("X-CSRF-TOKEN", token.GetProperty("token").GetString());
        request.Headers.Add("X-TKSC-Request", "1");
        request.Content = JsonContent.Create(body);
        return await client.SendAsync(request);
    }

    private sealed class SupplyApp : WebApplicationFactory<Program>
    {
        public FakeSessionAccounts AccountStore { get; } = new();
        public FakeSupplyStore Store { get; } = new();

        public SupplyApp()
        {
            foreach (var account in new[] { Staff, Manager, Player, Referee })
            {
                AccountStore.Accounts[account.Email] = account;
                AccountStore.AccountsById[account.Id] = account;
            }
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Authentication:Google:ClientId"] = "test-client",
                ["Authentication:Google:ClientSecret"] = "test-secret",
                ["ConnectionStrings:Sep490"] = "Server=unused;Database=unused"
            }));
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IAccountStore>();
                services.AddSingleton<IAccountStore>(AccountStore);
                services.RemoveAll<ISessionSupplyStore>();
                services.AddSingleton<ISessionSupplyStore>(Store);
                services.PostConfigure<GoogleOptions>(GoogleDefaults.AuthenticationScheme,
                    options => options.Backchannel = new HttpClient(new GoogleStub(() => true)));
            });
        }

        public HttpClient Client() => CreateClient(new WebApplicationFactoryClientOptions
        { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false, HandleCookies = true });
    }
}

internal sealed class FakeSupplyStore : ISessionSupplyStore
{
    private static readonly SupplySession Session = new(5, new DateOnly(2026, 11, 2), "18:00 - 20:00", "Cầu lông", "Court 1", "Published", 0, 0);
    private readonly Dictionary<long, SupplyAllocation> allocations = new();
    public int IssueCalls { get; private set; }
    public long? LastOperator { get; private set; }
    public string? NextFailure { get; set; }

    public HashSet<long> AssignedReferees { get; } = new();

    public Task<IReadOnlyList<SupplySession>> GetSessionsAsync(DateOnly date, long? refereeId, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<SupplySession>>(refereeId is null || AssignedReferees.Contains(refereeId.Value) ? [Session] : []);

    public Task<SessionSupplyDetails?> GetDetailsAsync(long sessionId, CancellationToken cancellationToken) =>
        Task.FromResult<SessionSupplyDetails?>(sessionId == Session.Id ? Details() : null);

    public Task<bool> IsRefereeAssignedAsync(long sessionId, long refereeId, CancellationToken cancellationToken) =>
        Task.FromResult(sessionId == Session.Id && AssignedReferees.Contains(refereeId));

    public Task<SupplyResult> ReportUseAsync(long sessionId, long productId, ReportSupplyUseRequest request, long refereeId, CancellationToken cancellationToken)
    {
        if (!AssignedReferees.Contains(refereeId)) return Task.FromResult(SupplyResult.Fail("NOT_ASSIGNED", "not assigned"));
        if (!allocations.TryGetValue(productId, out var allocation))
            return Task.FromResult(SupplyResult.Fail("ALLOCATION_NOT_FOUND", "missing"));
        if (allocation.IsReconciled) return Task.FromResult(SupplyResult.Fail("ALLOCATION_CLOSED", "closed"));
        if (request.UsedQuantity > allocation.Outstanding)
            return Task.FromResult(SupplyResult.Fail("USE_EXCEEDS_ISSUED", "too many"));
        allocations[productId] = allocation with { ReportedUsed = request.UsedQuantity };
        return Task.FromResult(SupplyResult.Ok(Details()));
    }

    public Task<SupplyResult> IssueAsync(long sessionId, IssueSupplyRequest request, long staffUserId, CancellationToken cancellationToken)
    {
        IssueCalls++;
        if (NextFailure is { } code) return Task.FromResult(SupplyResult.Fail(code, code));
        LastOperator = staffUserId;
        var current = allocations.GetValueOrDefault(request.ProductId);
        allocations[request.ProductId] = new SupplyAllocation(request.ProductId, "SHUTTLE", "Quả cầu lông",
            (current?.Issued ?? 0) + request.Quantity, 0, request.RecipientId, "Referee", false);
        return Task.FromResult(SupplyResult.Ok(Details()));
    }

    public Task<SupplyResult> ReconcileAsync(long sessionId, long productId, ReturnSupplyRequest request, long staffUserId, CancellationToken cancellationToken)
    {
        if (!allocations.TryGetValue(productId, out var allocation))
            return Task.FromResult(SupplyResult.Fail("ALLOCATION_NOT_FOUND", "missing"));
        if (request.ReturnedQuantity > allocation.Outstanding)
            return Task.FromResult(SupplyResult.Fail("RETURN_EXCEEDS_ISSUED", "too many"));
        allocations[productId] = allocation with { Returned = request.ReturnedQuantity, IsReconciled = true };
        return Task.FromResult(SupplyResult.Ok(Details()));
    }

    // Referee requests (task 13/11/2026): pending until Staff issues or rejects, or the referee cancels.
    private readonly Dictionary<long, SupplyRequestView> requests = new();
    public long? LastRequester { get; private set; }

    public Task<SupplyResult> CreateRequestAsync(long sessionId, CreateSupplyRequest request, long refereeId, CancellationToken cancellationToken)
    {
        if (!AssignedReferees.Contains(refereeId)) return Task.FromResult(SupplyResult.Fail("NOT_ASSIGNED", "not assigned"));
        LastRequester = refereeId;
        var id = requests.Count + 1L;
        requests[id] = new SupplyRequestView(id, request.ProductId, "SHUTTLE", "Quả cầu lông", request.Quantity, request.UnitName, request.Quantity, request.Note, "Pending", null, refereeId, "Referee", null, DateTimeOffset.UnixEpoch, null, null);
        return Task.FromResult(SupplyResult.Ok(Details()));
    }

    public Task<SupplyResult> CancelRequestAsync(long sessionId, long requestId, long refereeId, CancellationToken cancellationToken) =>
        Close(requestId, "Cancelled", refereeId, null, ownerId: refereeId);

    public Task<SupplyResult> RejectRequestAsync(long sessionId, long requestId, string note, long staffUserId, CancellationToken cancellationToken) =>
        Close(requestId, "Rejected", staffUserId, note);

    public async Task<SupplyResult> IssueRequestAsync(long sessionId, long requestId, IssueSupplyFromRequest request, long staffUserId, CancellationToken cancellationToken)
    {
        if (!requests.TryGetValue(requestId, out var pending) || (pending.Status != "Pending"))
            return SupplyResult.Fail(pending is null ? "REQUEST_NOT_FOUND" : "REQUEST_HANDLED", "request");
        var quantity = request.Quantity ?? pending.Quantity;
        var issued = await IssueAsync(sessionId, new IssueSupplyRequest(pending.ProductId, quantity, pending.RequestedBy), staffUserId, cancellationToken);
        if (issued.Success)
            requests[requestId] = pending with { Status = "Issued", IssuedQuantity = quantity };
        return issued.Success ? SupplyResult.Ok(Details()) : issued;
    }

    private Task<SupplyResult> Close(long requestId, string status, long actorId, string? note, long? ownerId = null)
    {
        if (!requests.TryGetValue(requestId, out var pending) || (ownerId is not null && pending.RequestedBy != ownerId))
            return Task.FromResult(SupplyResult.Fail("REQUEST_NOT_FOUND", "request"));
        if (pending.Status != "Pending")
            return Task.FromResult(SupplyResult.Fail("REQUEST_HANDLED", "handled"));
        requests[requestId] = pending with { Status = status, DecisionNote = note };
        return Task.FromResult(SupplyResult.Ok(Details()));
    }

    private SessionSupplyDetails Details() =>
        new(Session, [new SupplyRecipient(9, "Referee", "Referee")], allocations.Values.ToList(), requests.Values.ToList());
}
