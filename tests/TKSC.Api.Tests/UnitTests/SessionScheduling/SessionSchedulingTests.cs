using System.Security.Claims;
using System.Text.Json;
using be_da.Application.DTOs;
using be_da.Application.Interfaces.External;
using be_da.Application.Interfaces.Repositories;
using be_da.Application.Services;
using be_da.Presentation.Controllers;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace TKSC.Api.Tests.SessionScheduling;

public sealed class SessionSchedulingTests
{
    [Theory]
    [InlineData(0, 22, 1, 1, 1)]
    [InlineData(5, 0, 1, 1, 1)]
    [InlineData(5, 22, 0, 1, 1)]
    [InlineData(5, 22, 1, 0, 1)]
    [InlineData(5, 22, 1, 1, 0)]
    public async Task SaveRejectsMissingIdentifiersBeforeRepository(long venue, long actor, long sport, long court, long slot)
    {
        var repo = new Repository();
        var request = ValidRequest() with { SportId = sport, CourtId = court, TimeSlotId = slot };

        var result = await new SessionManagementService(repo).SaveAsync(venue, actor, null, request, default);

        AssertCode(result, OperationOutcome.Invalid, "INVALID_SESSION");
        Assert.Equal(0, repo.Calls);
    }

    [Fact]
    public async Task SaveRejectsPastDateBeforeRepository()
    {
        var repo = new Repository();
        var result = await new SessionManagementService(repo).SaveAsync(5, 22, null,
            ValidRequest() with { SessionDate = DateOnly.MinValue }, default);

        AssertCode(result, OperationOutcome.Invalid, "SESSION_IN_PAST");
        Assert.Equal(0, repo.Calls);
    }

    [Theory]
    [InlineData(0, 0, 0)]
    [InlineData(9, 0, 0)]
    [InlineData(8, -1, 0)]
    [InlineData(8, 0, 101)]
    public async Task SaveRejectsInvalidCapacityPriceOrBand(short capacity, int price, int bandLength)
    {
        var repo = new Repository();
        var request = ValidRequest() with { Capacity = capacity, PriceVnd = price, Band = new string('B', bandLength) };

        var result = await new SessionManagementService(repo).SaveAsync(5, 22, null, request, default);

        AssertCode(result, OperationOutcome.Invalid, "INVALID_SESSION");
        Assert.Equal(0, repo.Calls);
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData("  ", null)]
    [InlineData(" B2 ", "B2")]
    public async Task SaveNormalizesBandAndForwardsCreateOrUpdate(string? band, string? expected)
    {
        var repo = new Repository { Result = OperationResult.Created(new { id = 12 }) };
        var service = new SessionManagementService(repo);
        using var cts = new CancellationTokenSource();

        var result = await service.SaveAsync(5, 22, 12, ValidRequest() with { Band = band }, cts.Token);

        Assert.Same(repo.Result, result);
        Assert.Equal(1, repo.Calls);
        Assert.Equal((5, 22, 12), (repo.VenueId, repo.ActorId, repo.SessionId));
        Assert.Equal(expected, repo.SavedRequest!.Band);
        Assert.Equal(cts.Token, repo.Token);
    }

    [Theory]
    [InlineData(1, 0, 100)]
    [InlineData(8, 100000, 0)]
    public async Task SaveAcceptsCapacityAndBandBoundaries(short capacity, int price, int bandLength)
    {
        var repo = new Repository();
        var request = ValidRequest() with { Capacity = capacity, PriceVnd = price, Band = new string('B', bandLength) };

        var result = await new SessionManagementService(repo).SaveAsync(5, 22, null, request, default);

        Assert.Same(repo.Result, result);
        Assert.Equal(1, repo.Calls);
        Assert.Equal(capacity, repo.SavedRequest!.Capacity);
        Assert.Equal(price, repo.SavedRequest.PriceVnd);
        Assert.Null(repo.SessionId);
    }

    [Theory]
    [InlineData(0, 22, 12, "valid")]
    [InlineData(5, 0, 12, "valid")]
    [InlineData(5, 22, 0, "valid")]
    [InlineData(5, 22, 12, "zero-referee")]
    [InlineData(5, 22, 12, "negative-staff")]
    [InlineData(5, 22, 12, "duplicate-referee")]
    [InlineData(5, 22, 12, "duplicate-staff")]
    public async Task AssignRejectsInvalidIdentifiersAndDuplicates(long venue, long actor, long session, string scenario)
    {
        var repo = new Repository();
        var request = scenario switch
        {
            "zero-referee" => new SessionAssignmentsRequest([0], [3]),
            "negative-staff" => new SessionAssignmentsRequest([2], [-1]),
            "duplicate-referee" => new SessionAssignmentsRequest([2, 2], [3]),
            "duplicate-staff" => new SessionAssignmentsRequest([2], [3, 3]),
            _ => new SessionAssignmentsRequest([2], [3])
        };

        var result = await new SessionManagementService(repo).SetAssignmentsAsync(venue, actor, session, request, default);

        AssertCode(result, OperationOutcome.Invalid, "INVALID_ASSIGNMENTS");
        Assert.Equal(0, repo.Calls);
    }

    [Fact]
    public async Task AssignConvertsNullListsToEmptyAndForwardsRepositoryConflict()
    {
        var repo = new Repository { Result = OperationResult.Conflict(new { code = "PERSONNEL_CONFLICT" }) };
        using var cts = new CancellationTokenSource();

        var result = await new SessionManagementService(repo).SetAssignmentsAsync(5, 22, 12, new(null, null), cts.Token);

        Assert.Same(repo.Result, result);
        Assert.Equal(1, repo.Calls);
        Assert.Equal((5, 22, 12), (repo.VenueId, repo.ActorId, repo.SessionId));
        Assert.Empty(repo.Assignments!.RefereeIds!);
        Assert.Empty(repo.Assignments.StaffIds!);
        Assert.Equal(cts.Token, repo.Token);
    }

    [Fact]
    public async Task AssignForwardsDistinctRefereesAndStaff()
    {
        var repo = new Repository();
        var request = new SessionAssignmentsRequest([2, 4], [3, 5]);

        await new SessionManagementService(repo).SetAssignmentsAsync(5, 22, 12, request, default);

        Assert.Equal(1, repo.Calls);
        Assert.Equal(new long[] { 2, 4 }, repo.Assignments!.RefereeIds);
        Assert.Equal(new long[] { 3, 5 }, repo.Assignments.StaffIds);
    }

    [Theory]
    [InlineData(0, 22, 12)]
    [InlineData(5, 0, 12)]
    [InlineData(5, 22, 0)]
    public async Task StatusRejectsInvalidIdentifiers(long venue, long actor, long session)
    {
        var repo = new Repository();
        var result = await new SessionManagementService(repo).SetStatusAsync(venue, actor, session, true, default);

        AssertCode(result, OperationOutcome.Invalid, "INVALID_SESSION");
        Assert.Equal(0, repo.Calls);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task StatusForwardsOpenFlagAndRepositoryResult(bool open)
    {
        var repo = new Repository { Result = OperationResult.Conflict(new { code = "RESOURCE_CONFLICT" }) };
        using var cts = new CancellationTokenSource();

        var result = await new SessionManagementService(repo).SetStatusAsync(5, 22, 12, open, cts.Token);

        Assert.Same(repo.Result, result);
        Assert.Equal(1, repo.Calls);
        Assert.Equal((5, 22, 12), (repo.VenueId, repo.ActorId, repo.SessionId));
        Assert.Equal(open, repo.Open);
        Assert.Equal(cts.Token, repo.Token);
    }

    [Fact]
    public async Task OptionsForwardVenueAndCancellationToken()
    {
        var repo = new Repository();
        using var cts = new CancellationTokenSource();

        var result = await new SessionManagementService(repo).GetOptionsAsync(5, cts.Token);

        Assert.Same(repo.Options, result);
        Assert.Equal(5, repo.VenueId);
        Assert.Equal(cts.Token, repo.Token);
    }

    [Theory]
    [InlineData("create")]
    [InlineData("update")]
    [InlineData("assign")]
    [InlineData("open")]
    [InlineData("close")]
    public async Task MutationsRejectInvalidCsrfBeforeCallingService(string action)
    {
        var repo = new Repository();
        var controller = Controller(repo, false, 5, "22");

        var response = await Invoke(controller, action);

        Assert.IsType<BadRequestObjectResult>(response);
        Assert.Equal(0, repo.Calls);
    }

    [Theory]
    [InlineData(null, "22")]
    [InlineData(5L, null)]
    [InlineData(5L, "not-a-number")]
    public async Task CreateRequiresCenterAndNumericActor(long? venue, string? actor)
    {
        var repo = new Repository();
        var response = await Controller(repo, true, venue, actor).Create(ValidRequest(), default);

        Assert.IsType<UnauthorizedResult>(response);
        Assert.Equal(0, repo.Calls);
    }

    [Fact]
    public async Task OptionsWithoutCenterReturnsBadRequest()
    {
        var repo = new Repository();
        var response = await Controller(repo, true, null, "22").Options(default);

        Assert.IsType<BadRequestObjectResult>(response);
        Assert.Equal(0, repo.Calls);
    }

    [Fact]
    public async Task CreateReturnsCreatedAndPassesCenterActorAndRequest()
    {
        var payload = new { id = 12 };
        var repo = new Repository { Result = OperationResult.Created(payload) };
        var request = ValidRequest();

        var response = Assert.IsType<CreatedResult>(await Controller(repo, true, 5, "22").Create(request, default));

        Assert.Equal("/api/sessions/manage", response.Location);
        Assert.Same(payload, response.Value);
        Assert.Equal((5, 22, (long?)null), (repo.VenueId, repo.ActorId, repo.SessionId));
        Assert.Equal(request, repo.SavedRequest);
    }

    [Theory]
    [InlineData("update")]
    [InlineData("assign")]
    [InlineData("open")]
    [InlineData("close")]
    public async Task ControllerPreservesRepositoryConflict(string action)
    {
        var payload = new { code = "test_conflict" };
        var repo = new Repository { Result = OperationResult.Conflict(payload) };

        var response = Assert.IsType<ConflictObjectResult>(await Invoke(Controller(repo, true, 5, "22"), action));

        Assert.Same(payload, response.Value);
        Assert.Equal(1, repo.Calls);
        Assert.Equal((5, 22, 12), (repo.VenueId, repo.ActorId, repo.SessionId));
        if (action is "open" or "close") Assert.Equal(action == "open", repo.Open);
    }

    private static SaveManagedSessionRequest ValidRequest() => new(1, 2, 3, DateOnly.MaxValue, "B2", 8, 50000);

    private static void AssertCode(OperationResult result, OperationOutcome outcome, string code)
    {
        Assert.Equal(outcome, result.Outcome);
        using var json = JsonDocument.Parse(JsonSerializer.Serialize(result.Value));
        Assert.Equal(code, json.RootElement.GetProperty("code").GetString());
    }

    private static SessionManagementController Controller(Repository repository, bool csrf, long? venue, string? actor)
    {
        var claims = actor is null ? Array.Empty<Claim>() : [new Claim(ClaimTypes.NameIdentifier, actor)];
        var context = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(claims, "test")) };
        return new SessionManagementController(new SessionManagementService(repository), new Scope(venue), new Antiforgery(csrf))
        {
            ControllerContext = new ControllerContext { HttpContext = context }
        };
    }

    private static Task<IActionResult> Invoke(SessionManagementController controller, string action) => action switch
    {
        "create" => controller.Create(ValidRequest(), default),
        "update" => controller.Update(12, ValidRequest(), default),
        "assign" => controller.Assign(12, new([2], [3]), default),
        "open" => controller.Open(12, default),
        "close" => controller.Close(12, default),
        _ => throw new ArgumentOutOfRangeException(nameof(action))
    };

    private sealed record Scope(long? VenueId) : IDataAccessContext
    {
        public string Role => "Manager";
        public long? RestrictedVenueId => VenueId;
    }

    private sealed class Antiforgery(bool valid) : IAntiforgery
    {
        public AntiforgeryTokenSet GetAndStoreTokens(HttpContext context) => new("request", "cookie", "form", "header");
        public AntiforgeryTokenSet GetTokens(HttpContext context) => new("request", "cookie", "form", "header");
        public Task<bool> IsRequestValidAsync(HttpContext context) => Task.FromResult(valid);
        public Task ValidateRequestAsync(HttpContext context) => valid ? Task.CompletedTask : Task.FromException(new AntiforgeryValidationException("Invalid test token."));
        public void SetCookieTokenAndHeader(HttpContext context) { }
    }

    private sealed class Repository : ISessionManagementRepository
    {
        public int Calls;
        public long VenueId;
        public long ActorId;
        public long? SessionId;
        public CancellationToken Token;
        public SaveManagedSessionRequest? SavedRequest;
        public SessionAssignmentsRequest? Assignments;
        public bool Open;
        public OperationResult Result = OperationResult.Success(new { id = 12 });
        public SessionManagementOptions Options = new([], [], [], []);

        public Task<SessionManagementOptions> GetOptionsAsync(long venueId, CancellationToken ct)
        {
            Calls++; VenueId = venueId; Token = ct;
            return Task.FromResult(Options);
        }

        public Task<IReadOnlyList<ManagedSession>> GetSessionsAsync(long venueId, CancellationToken ct)
        {
            Calls++; VenueId = venueId; Token = ct;
            return Task.FromResult<IReadOnlyList<ManagedSession>>([]);
        }

        public Task<OperationResult> SaveAsync(long venueId, long actorId, long? sessionId, SaveManagedSessionRequest request, CancellationToken ct)
        {
            Calls++; VenueId = venueId; ActorId = actorId; SessionId = sessionId; SavedRequest = request; Token = ct;
            return Task.FromResult(Result);
        }

        public Task<OperationResult> SetAssignmentsAsync(long venueId, long actorId, long sessionId, SessionAssignmentsRequest request, CancellationToken ct)
        {
            Calls++; VenueId = venueId; ActorId = actorId; SessionId = sessionId; Assignments = request; Token = ct;
            return Task.FromResult(Result);
        }

        public Task<OperationResult> SetStatusAsync(long venueId, long actorId, long sessionId, bool open, CancellationToken ct)
        {
            Calls++; VenueId = venueId; ActorId = actorId; SessionId = sessionId; Open = open; Token = ct;
            return Task.FromResult(Result);
        }
    }
}
