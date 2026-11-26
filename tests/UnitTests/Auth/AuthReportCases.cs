using System.Text.Json;
using be_da.Application.Interfaces.Services;
using be_da.Application.Services;
using be_da.Application.Services.Accounts;
using be_da.Domain.Entities;
using be_da.Domain.Exceptions;
using be_da.Presentation.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace TKSC.Api.Tests.UnitTests.Auth;

public sealed class AuthReportCases(ITestOutputHelper output)
{
    [Fact]
    public async Task UTC001_CreatesPlayer()
    {
        await SignIn("10987654321", "player1@gmail.com", "Nguyen Van A", true, "player");
    }

    [Fact]
    public async Task UTC002_ExistingStaff()
    {
        await SignIn("20987654321", "staff1@tksc.vn", "Tran Thi B", true, "staff", "Staff", 1, accountId: 2);
    }

    [Fact]
    public async Task UTC003_AdminPortalRejectsManager()
    {
        await SignIn("30987654321", "manager1@tksc.vn", "Le Van C", true, "admin", "Manager", 2, "portal_access_denied");
    }

    [Fact]
    public async Task UTC004_ExistingReferee()
    {
        await SignIn("35987654321", "referee1@tksc.vn", "Pham D", true, "staff", "Referee", 1, accountId: 4);
    }

    [Fact]
    public async Task UTC005_AdminPortalRejectsOwner()
    {
        await SignIn("38987654321", "owner@tksc.vn", "Tran Minh Duc", true, "admin", "Owner", error: "portal_access_denied");
    }

    [Fact]
    public async Task UTC006_RejectsUnverifiedEmail()
    {
        await SignIn("40987654321", "unverified@gmail.com", "Hoang E", false, "player", error: "invalid_google_identity");
    }

    [Fact]
    public async Task UTC007_RejectsEmptySubject()
    {
        await SignIn("", "player2@gmail.com", "Vu F", true, "player", error: "invalid_google_identity");
    }

    [Fact]
    public async Task UTC008_Accepts255CharacterSubject()
    {
        await SignIn(new string('1', 255), "player3@gmail.com", "Do G", true, "player");
    }

    [Fact]
    public async Task UTC009_Rejects256CharacterSubject()
    {
        await SignIn(new string('1', 256), "player4@gmail.com", "Bui H", true, "player", error: "invalid_google_identity");
    }

    [Fact]
    public async Task UTC010_Rejects321CharacterEmail()
    {
        await SignIn("60987654321", new string('a', 315) + "@tk.vn", "Duong K", true, "player", error: "invalid_google_identity");
    }

    [Fact]
    public async Task UTC011_AdminPortalRejectsPlayer()
    {
        await SignIn("80987654321", "player5@gmail.com", "Ly L", true, "admin", "Player", error: "portal_access_denied");
    }

    [Fact]
    public async Task UTC012_PlayerCanSelectFirstCenter()
    {
        await Resolve("Player", null, 1, 1);
    }

    [Fact]
    public async Task UTC013_PlayerCanSelectSecondCenter()
    {
        await Resolve("Player", null, 2, 2);
    }

    [Fact]
    public async Task UTC014_StaffCanSelectAssignedFirstCenter()
    {
        await Resolve("Staff", 1, 1, 1);
    }

    [Fact]
    public async Task UTC015_StaffCanSelectAssignedSecondCenter()
    {
        await Resolve("Staff", 2, 2, 2);
    }

    [Fact]
    public async Task UTC016_StaffCannotSelectUnassignedCenter()
    {
        await Resolve("Staff", 2, 1, error: "center_access_denied");
    }

    [Fact]
    public void UTC017_StaffIsPersonnel()
    {
        output.WriteLine("Input (CenterSelection.Personnel): role=Staff");
        output.WriteLine("Expected: true");
        var result = CenterSelection.Personnel("Staff");
        output.WriteLine($"Actual: {result}");
        Assert.True(result);
    }

    [Fact]
    public async Task UTC018_StaffPortalAllowsManager()
    {
        await SignIn("manager-subject", "manager@example.test", "Manager", true, "staff", "Manager", 2);
    }

    [Fact]
    public async Task UTC019_StaffPortalAllowsOwner()
    {
        await SignIn("owner-subject", "owner@example.test", "Owner", true, "staff", "Owner");
    }

    [Fact]
    public async Task UTC020_AdminPortalAllowsSystemAdmin()
    {
        await SignIn("admin-subject", "admin@example.test", "Admin", true, "admin", "SystemAdmin");
    }

    [Fact]
    public async Task UTC021_RejectsUnlinkedStaffPortal()
    {
        await SignIn("new-subject", "player@example.test", "Player", true, "staff", error: "portal_access_denied");
    }

    [Fact]
    public async Task UTC022_RejectsInactiveAccount()
    {
        await SignIn("staff-subject", "staff@example.test", "Staff", true, "staff", "Staff", 1, "account_unavailable", status: "Inactive");
    }

    [Fact]
    public async Task UTC023_RejectsDeletedAccount()
    {
        await SignIn("staff-subject", "staff@example.test", "Staff", true, "staff", "Staff", 1, "account_unavailable", deleted: true);
    }

    [Fact]
    public async Task UTC024_RejectsUnavailableCenter()
    {
        await Resolve("Player", null, 999, error: "center_unavailable");
    }

    [Fact]
    public async Task UTC025_RejectsMissingPersonnelAssignment()
    {
        await Resolve("Manager", null, 1, error: "center_access_denied");
    }

    [Fact]
    public async Task UTC026_DefaultsStaffToAssignedCenter()
    {
        await Resolve("Staff", 2, null, 2);
    }

    [Fact]
    public async Task UTC027_DisabledCenterSelectionReturnsNull()
    {
        await Resolve("Player", null, 1, enabled: false);
    }

    private async Task SignIn(string subject, string email, string name, bool verified, string portal,
        string? linkedRole = null, long? venue = null, string? error = null, string status = "Active",
        bool deleted = false, long accountId = 1)
    {
        var store = new FakeAccounts
        {
            Account = linkedRole is null ? null : new Account(accountId, email, name, linkedRole, status, subject, deleted, venue)
        };
        var service = new GoogleSignIn(store);
        var expectedRole = linkedRole ?? "Player";
        output.WriteLine($"Input (GoogleSignIn.ExecuteAsync): {JsonSerializer.Serialize(new
        {
            subject = Show(subject), email = Show(email), name, verified, portal, linkedRole, venue, status, deleted
        })}");
        output.WriteLine(error is null ? $"Expected: Account with role {expectedRole}" : $"Expected: SignInRejectedException({error})");
        Account? actual = null;
        var exception = await Record.ExceptionAsync(async () =>
        {
            actual = await service.ExecuteAsync(subject, email, name, verified, default, portal);
        });
        if (error is not null)
        {
            CheckRejected(exception, error);
            Assert.Equal(0, store.Created);
            return;
        }

        Assert.Null(exception);
        Assert.NotNull(actual);
        output.WriteLine($"Actual: {JsonSerializer.Serialize(new { actual.Id, actual.Email, actual.Role, actual.CanSignIn, actual.VenueId })}");
        Assert.Equal(accountId, actual.Id);
        Assert.Equal(expectedRole, actual.Role);
        Assert.Equal(email, actual.Email);
        Assert.Equal(subject, actual.GoogleSubject);
        Assert.True(actual.CanSignIn);
        Assert.Equal(venue, actual.VenueId);
        Assert.Equal(linkedRole is null ? 1 : 0, store.Created);
    }

    private async Task Resolve(string role, long? assigned, long? requested, long? expected = null,
        string? error = null, bool enabled = true)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(
            new Dictionary<string, string?> { ["SportCenters:Enabled"] = enabled.ToString() }).Build();
        using var provider = new ServiceCollection()
            .AddSingleton<ISessionService>(new SessionService(new FakeSessionStore())).BuildServiceProvider();
        var selector = new CenterSelection(provider, configuration);
        var account = new Account(1, "tester@example.test", "Test", role, "Active", "test-subject", false, assigned);
        output.WriteLine($"Input (CenterSelection.Resolve): {JsonSerializer.Serialize(new { role, assigned, requested, enabled })}");
        output.WriteLine(error is null ? $"Expected: {JsonSerializer.Serialize(expected)}" : $"Expected: SignInRejectedException({error})");
        long? actual = null;
        var exception = await Record.ExceptionAsync(async () => { actual = await selector.Resolve(account, requested, default); });
        if (error is not null)
        {
            CheckRejected(exception, error);
            return;
        }

        Assert.Null(exception);
        output.WriteLine($"Actual: {JsonSerializer.Serialize(actual)}");
        Assert.Equal(expected, actual);
    }

    private void CheckRejected(Exception? actual, string code)
    {
        output.WriteLine(actual is SignInRejectedException rejected
            ? $"Actual: SignInRejectedException({rejected.Code})"
            : $"Actual: {actual?.GetType().Name ?? "No exception"}");
        Assert.Equal(code, Assert.IsType<SignInRejectedException>(actual).Code);
    }

    private static string Show(string value) => value.Length > 70 ? $"{value[..8]}... ({value.Length} characters)" : value;
}
