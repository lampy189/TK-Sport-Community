using System.Text.Json;
using be_da.Application.DTOs;
using be_da.Application.Interfaces.Repositories;
using be_da.Application.Services;
using be_da.Application.Services.Chatbot;
using Xunit;
using Xunit.Abstractions;

namespace TKSC.Api.Tests.UnitTests.Community;

public sealed class CommunityReportCases(ITestOutputHelper output)
{
    [Fact]
    public async Task UTC001_SeparatePayment() => await Invite(12, "separate", OperationOutcome.Success);

    [Fact]
    public async Task UTC002_RejectsInviterPayment() => await Invite(12, "inviter", OperationOutcome.Invalid);

    [Fact]
    public async Task UTC003_RejectsInvalidFriend() => await Invite(0, "separate", OperationOutcome.Invalid);

    [Fact]
    public async Task UTC004_RejectsSplitPayment() => await Invite(12, "split_half", OperationOutcome.Invalid);

    [Fact]
    public async Task UTC005_AcceptsWithCapacity() => await Reply(true, true, OperationOutcome.Success);

    [Fact]
    public async Task UTC006_Declines() => await Reply(false, true, OperationOutcome.Success);

    [Fact]
    public async Task UTC007_RejectsFullSession() => await Reply(true, false, OperationOutcome.Conflict);

    [Fact]
    public void UTC008_ZaloUrl() => Url("https://zalo.me/0987654321", "zalo", true);

    [Fact]
    public void UTC009_MessengerShortUrl() => Url("https://m.me/tksportcommunity", "messenger", true);

    [Fact]
    public void UTC010_MessengerThreadUrl() => Url("https://www.messenger.com/t/tksport", "messenger", true);

    [Fact]
    public void UTC011_RejectsHttpUrl() => Url("http://zalo.me/0987654321", "zalo", false);

    [Fact]
    public void UTC012_RejectsQueryString() => Url("https://zalo.me/0987654321?src=fb", "zalo", false);

    [Fact]
    public void UTC013_MinimalZaloPath() => Url("https://zalo.me/a", "zalo", true);

    [Fact]
    public void UTC014_ContactWithoutLinks() => Contact("Toi muon dat san toi mai", null, null, true);

    [Fact]
    public void UTC015_ContactWithZalo() => Contact("Cho toi Zalo cua san de lien he", "https://zalo.me/0987654321", null, true);

    [Fact]
    public void UTC016_ContactWithBothLinks() => Contact("Lien he san dat lich", "https://zalo.me/0987654321", "https://m.me/tksc", true);

    [Fact]
    public void UTC017_PolicyQuestionNoContact() => Contact("Quy dinh va chinh sach huy ve cua san the nao", "https://zalo.me/0987654321", null, false);

    private async Task Invite(long player, string payment, OperationOutcome expected)
    {
        var repository = CommunityStoreProbe<IInvitationRepository>.Create();
        var probe = (CommunityStoreProbe<IInvitationRepository>)(object)repository;
        probe.Override = (method, _) => method == "SendAsync"
            ? Task.FromResult(OperationResult.Success(new { Id = 301, Status = "pending" })) : null;
        var service = new InvitationService(repository, CommunityStoreProbe<ISessionStore>.Create());
        var request = new InviteSessionRequest(player, payment);
        output.WriteLine($"Input (InvitationService.SendAsync): userId=10, sessionId=101, {request}");
        output.WriteLine($"Expected: {expected}");
        var result = await service.SendAsync(10, 101, request, default);
        Write(result);
        Assert.Equal(expected, result.Outcome);
        var data = JsonSerializer.SerializeToElement(result.Value);
        if (expected == OperationOutcome.Success)
        {
            Assert.Equal(new object?[] { 10L, 101L, request }, Assert.Single(probe.Calls).Args.Take(3));
            Assert.Equal(301, data.GetProperty("Id").GetInt32());
            Assert.Equal("pending", data.GetProperty("Status").GetString());
        }
        else
        {
            Assert.Empty(probe.Calls);
            Assert.Equal("invalid_invitation", data.GetProperty("code").GetString());
        }
    }

    private async Task Reply(bool accept, bool hasCapacity, OperationOutcome expected)
    {
        var repository = CommunityStoreProbe<IInvitationRepository>.Create();
        var invitations = (CommunityStoreProbe<IInvitationRepository>)(object)repository;
        invitations.Override = (method, _) => method == "DeclineAsync"
            ? Task.FromResult(OperationResult.Success(new { Id = 301, Status = "declined" })) : null;
        var store = CommunityStoreProbe<ISessionStore>.Create();
        var sessions = (CommunityStoreProbe<ISessionStore>)(object)store;
        sessions.Override = (method, _) => method != "AcceptInvitationAsync" ? null
            : Task.FromResult(hasCapacity
                ? new RegisterSessionResult(true, null, null, TicketId: 801)
                : new RegisterSessionResult(false, "SESSION_FULL", "Buổi chơi đã hết chỗ."));
        var service = new InvitationService(repository, store);
        output.WriteLine($"Input (InvitationService.ReplyAsync): userId=12, id=301, accept={accept}, hasCapacity={hasCapacity}");
        output.WriteLine($"Expected: {expected}");
        var result = await service.ReplyAsync(12, 301, accept, default);
        Write(result);
        Assert.Equal(expected, result.Outcome);
        var data = JsonSerializer.SerializeToElement(result.Value);
        if (!accept)
        {
            Assert.Empty(sessions.Calls);
            Assert.Equal(new object?[] { 12L, 301L }, Assert.Single(invitations.Calls).Args.Take(2));
            Assert.Equal("declined", data.GetProperty("Status").GetString());
        }
        else
        {
            Assert.Empty(invitations.Calls);
            Assert.Equal(new object?[] { 12L, 301L }, Assert.Single(sessions.Calls).Args.Take(2));
            if (hasCapacity)
                Assert.Equal(801, data.GetProperty("TicketId").GetInt64());
            else
                Assert.Equal("SESSION_FULL", data.GetProperty("code").GetString());
        }
    }

    private void Url(string value, string channel, bool allowed)
    {
        var expected = allowed ? value : null;
        output.WriteLine($"Input (BookingContactReply.ValidateUrl): channel={channel}, value={value}");
        output.WriteLine($"Expected: {expected ?? "null"}");
        var actual = BookingContactReply.ValidateUrl(value, channel);
        output.WriteLine($"Actual: {actual ?? "null"}");
        Assert.Equal(expected, actual);
    }

    private void Contact(string question, string? zalo, string? messenger, bool contact)
    {
        var links = new[] { messenger, zalo }.OfType<string>().ToArray();
        output.WriteLine($"Input (BookingContactReply.Create): {JsonSerializer.Serialize(new { question, zalo, messenger })}");
        output.WriteLine(contact ? $"Expected: contact; links={JsonSerializer.Serialize(links)}" : "Expected: null");
        var actual = BookingContactReply.Create(question, new ChatbotOptions { ZaloUrl = zalo, MessengerUrl = messenger });
        output.WriteLine($"Actual: {JsonSerializer.Serialize(actual is null ? null : new { actual.Mode, actual.BookingContact })}");
        if (!contact)
            Assert.Null(actual);
        else
        {
            Assert.Equal("contact", actual?.Mode);
            Assert.Contains(question, actual!.BookingContact!.Draft);
            Assert.Equal(links, actual.BookingContact.Links.Select(link => link.Url));
        }
    }

    private void Write(OperationResult result) =>
        output.WriteLine($"Actual: {result.Outcome} {JsonSerializer.Serialize(result.Value)}");
}
