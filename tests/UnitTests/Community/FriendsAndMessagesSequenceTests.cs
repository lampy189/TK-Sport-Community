using System.Reflection;
using System.Text.Json;
using be_da.Application.DTOs;
using be_da.Application.Interfaces.Repositories;
using be_da.Application.Services;
using Xunit;
using Xunit.Abstractions;

namespace TKSC.Api.Tests.UnitTests.Community;

public sealed class FriendsAndMessagesSequenceTests(ITestOutputHelper output)
{
    [Fact]
    public async Task UTC018_FindsMember()
    {
        output.WriteLine("V09 SearchMembers | Expected: Success; member 2 returned; query and limit forwarded");
        var f = new Fixture(output);
        var r = await f.Search("Player Two", 7);
        Assert.Equal(OperationOutcome.Success, r.Outcome);
        Assert.Equal(2, Assert.Single(Assert.IsAssignableFrom<IReadOnlyList<MemberSearchResult>>(r.Value)).PlayerId);
        Assert.Equal(new object?[] { 1L, "Player Two", 7 }, f.Args("SearchMembersAsync").Take(3));
    }

    [Fact]
    public async Task UTC019_ReturnsEmpty()
    {
        output.WriteLine("V09 SearchMembers | Expected: Success; empty list");
        var f = new Fixture(output);
        f.Store.Override = (m, _) => m == "SearchMembersAsync" ? Task.FromResult<IReadOnlyList<MemberSearchResult>>([]) : null;
        var r = await f.Search("Missing", 20);
        Assert.Equal(OperationOutcome.Success, r.Outcome);
        Assert.Empty(Assert.IsAssignableFrom<IReadOnlyList<MemberSearchResult>>(r.Value));
    }

    [Fact]
    public async Task UTC020_CreatesAndNotifies()
    {
        output.WriteLine("V09 SendFriendRequest | Expected: Success; incoming request and friend_request push to member 2");
        var f = new Fixture(output);
        var r = await f.SendRequest(2);
        Assert.Equal(OperationOutcome.Success, r.Outcome);
        var pair = Assert.IsType<FriendshipActionResult>(r.Value);
        Assert.NotNull(pair.FriendshipId);
        Assert.True(Assert.Single(await f.Friends.GetPendingRequestsAsync(2, default)).IsIncoming);
        f.AssertPush(2, "friend_request", "friendshipId", pair.FriendshipId.ToString()!);
    }

    [Fact]
    public async Task UTC021_RejectsAlreadyFriends()
    {
        output.WriteLine("V09 SendFriendRequest | Expected: Conflict already_friends; no push");
        var f = new Fixture(output);
        f.Friends.AddAcceptedFriend(100, 1, 2);
        f.AssertError(await f.SendRequest(2), OperationOutcome.Conflict, "already_friends");
        Assert.Empty(f.Notifications.Sent);
    }

    [Fact]
    public async Task UTC022_RejectsPendingRequest()
    {
        output.WriteLine("V09 SendFriendRequest | Expected: Conflict friend_request_pending; no push");
        var f = new Fixture(output);
        f.Friends.AddPendingRequest(100, 2, 1);
        f.AssertError(await f.SendRequest(2), OperationOutcome.Conflict, "friend_request_pending");
        Assert.Empty(f.Notifications.Sent);
    }

    [Fact]
    public async Task UTC023_RejectsInvalidTarget()
    {
        output.WriteLine("V09 SendFriendRequest | Expected: Invalid; store and push are not called");
        var f = new Fixture(output);
        f.AssertError(await f.SendRequest(0), OperationOutcome.Invalid, "invalid_target_player_id");
        Assert.Empty(f.Store.Calls);
        Assert.Empty(f.Notifications.Sent);
    }

    [Fact]
    public async Task UTC024_SkipsPushWithoutMappedUser()
    {
        output.WriteLine("V09 SendFriendRequest | Expected: Success; request saved; no push");
        var f = new Fixture(output);
        f.Store.Override = (m, _) => m == "GetUserIdByPlayerIdAsync" ? Task.FromResult<long?>(null) : null;
        var r = await f.SendRequest(2);
        Assert.Equal(OperationOutcome.Success, r.Outcome);
        Assert.Single(await f.Friends.GetPendingRequestsAsync(2, default));
        Assert.Empty(f.Notifications.Sent);
    }

    [Fact]
    public async Task UTC025_AcceptsAndNotifiesRequester()
    {
        output.WriteLine("V09 RespondFriendRequest | Expected: Success; friendship accepted; friend_accepted push to requester 1");
        var f = new Fixture(output);
        f.Friends.AddPendingRequest(100, 1, 2);
        var r = await f.Respond(true);
        Assert.Equal(OperationOutcome.Success, r.Outcome);
        Assert.True(await f.Friends.AreFriendsAsync(1, 2, default));
        f.AssertPush(1, "friend_accepted", "friendshipId", "100");
    }

    [Fact]
    public async Task UTC026_DeclinesWithoutPush()
    {
        output.WriteLine("V09 RespondFriendRequest | Expected: Success; no friendship or push; pending request removed");
        var f = new Fixture(output);
        f.Friends.AddPendingRequest(100, 1, 2);
        Assert.Equal(OperationOutcome.Success, (await f.Respond(false)).Outcome);
        Assert.False(await f.Friends.AreFriendsAsync(1, 2, default));
        Assert.Empty(await f.Friends.GetPendingRequestsAsync(2, default));
        Assert.Empty(f.Notifications.Sent);
    }

    [Fact]
    public async Task UTC027_RejectsNonRecipient()
    {
        output.WriteLine("V09 RespondFriendRequest | Expected: Denied; request remains pending; no push");
        var f = new Fixture(output);
        f.Friends.AddPendingRequest(100, 1, 2);
        Assert.Equal(OperationOutcome.Denied, (await f.Respond(true, 3)).Outcome);
        Assert.Single(await f.Friends.GetPendingRequestsAsync(2, default));
        Assert.Empty(f.Notifications.Sent);
    }

    [Fact]
    public async Task UTC028_RejectsMissingRequest()
    {
        output.WriteLine("V09 RespondFriendRequest | Expected: Missing request_not_found; no push");
        var f = new Fixture(output);
        f.AssertError(await f.Respond(true), OperationOutcome.Missing, "request_not_found");
        Assert.Empty(f.Notifications.Sent);
    }

    [Fact]
    public async Task UTC029_ReturnsScopedMessages()
    {
        output.WriteLine("V09 GetConversation | Expected: Success; only the selected conversation returned");
        var f = new Fixture(output);
        f.Friends.AddAcceptedFriend(100, 1, 2);
        await f.Friends.SendMessageAsync(1, 2, "Only for B", default);
        await f.Friends.SendMessageAsync(1, 3, "Not this conversation", default);
        var r = await f.Conversation();
        Assert.Equal(OperationOutcome.Success, r.Outcome);
        Assert.Equal("Only for B", Assert.Single(Assert.IsAssignableFrom<IReadOnlyList<ChatMessageItem>>(r.Value)).Content);
        Assert.Equal(new object?[]{1L,2L,10}, f.Args("GetConversationAsync").Take(3));
    }

    [Fact]
    public async Task UTC030_RejectsPendingFriendship()
    {
        output.WriteLine("V09 GetConversation | Expected: Denied; no conversation lookup or message disclosure");
        var f = new Fixture(output);
        f.Friends.AddPendingRequest(100, 1, 2);
        f.AssertError(await f.Conversation(), OperationOutcome.Denied, "not_friends");
        Assert.DoesNotContain(f.Store.Calls, c => c.Method == "GetConversationAsync");
    }

    [Fact]
    public async Task UTC031_SavesTrimmedMessageAndPushes()
    {
        output.WriteLine("V09 SendMessage | Expected: Created; trimmed message saved; one chat_message push to recipient");
        var f = new Fixture(output);
        f.Friends.AddAcceptedFriend(100, 1, 2);
        var r=await f.Send("  Hello B  ");
        Assert.Equal(OperationOutcome.Created, r.Outcome);
        var message=Assert.IsType<ChatMessageItem>(r.Value);
        Assert.Equal("Hello B", message.Content);
        Assert.Equal(message.Id, Assert.Single(await f.Friends.GetConversationAsync(1,2,10,default)).Id);
        f.AssertPush(2, "chat_message", "messageId", message.Id.ToString());
        Assert.Equal("Hello B", Assert.Single(f.Notifications.Sent).Body);
    }

    [Fact]
    public async Task UTC032_RejectsNonFriend()
    {
        output.WriteLine("V09 SendMessage | Expected: Denied not_friends; no save or push");
        var f = new Fixture(output);
        f.AssertError(await f.Send("Hello B"), OperationOutcome.Denied, "not_friends");
        f.AssertNoMessage();
    }

    [Fact]
    public async Task UTC033_RejectsBlank()
    {
        output.WriteLine("V09 SendMessage | Expected: Invalid; no save or push");
        var f = new Fixture(output);
        f.Friends.AddAcceptedFriend(100, 1, 2);
        f.AssertError(await f.Send("   "), OperationOutcome.Invalid, "invalid_message_content");
        f.AssertNoMessage();
    }

    [Fact]
    public async Task UTC034_Accepts2000AndTrimsPreview()
    {
        output.WriteLine("V09 SendMessage | Expected: Created; 2000 characters saved; push preview shortened to 100 plus ellipsis");
        var f = new Fixture(output);
        f.Friends.AddAcceptedFriend(100, 1, 2);
        var r=await f.Send(" "+new string('x',2000)+" ");
        Assert.Equal(OperationOutcome.Created,r.Outcome);
        Assert.Equal(2000, Assert.IsType<ChatMessageItem>(r.Value).Content.Length);
        Assert.Equal(new string('x',100)+"...",Assert.Single(f.Notifications.Sent).Body);
    }

    [Fact]
    public async Task UTC035_Rejects2001()
    {
        output.WriteLine("V09 SendMessage | Expected: Invalid message_too_long; no save or push");
        var f = new Fixture(output);
        f.Friends.AddAcceptedFriend(100, 1, 2);
        f.AssertError(await f.Send(new string('x',2001)), OperationOutcome.Invalid, "message_too_long");
        f.AssertNoMessage();
    }

    [Fact]
    public async Task UTC036_RejectsUnavailableRecipient()
    {
        output.WriteLine("V09 SendMessage | Expected: Missing recipient_not_found; no push");
        var f = new Fixture(output);
        f.Friends.AddAcceptedFriend(100, 1, 2);
        f.Store.Override=(m,_)=>m=="SendMessageAsync"?Task.FromResult<ChatMessageItem?>(null):null;
        f.AssertError(await f.Send("Hello B"),OperationOutcome.Missing,"recipient_not_found");
        Assert.Empty(f.Notifications.Sent);
    }

    [Fact]
    public async Task UTC037_MarksOnlyIncomingFromSelectedSender()
    {
        output.WriteLine("V09 MarkMessagesAsRead | Expected: Completed; only messages from 2 to 1 become read");
        var f = new Fixture(output);
        var incoming=await f.Friends.SendMessageAsync(2,1,"From B",default);
        var outgoing=await f.Friends.SendMessageAsync(1,2,"From A",default);
        var unrelated=await f.Friends.SendMessageAsync(3,1,"From C",default);
        Assert.Equal(OperationOutcome.Completed,(await f.Read()).Outcome);
        var pair=await f.Friends.GetConversationAsync(1,2,10,default);
        Assert.True(pair.Single(m=>m.Id==incoming!.Id).IsRead);
        Assert.False(pair.Single(m=>m.Id==outgoing!.Id).IsRead);
        var third=Assert.Single(await f.Friends.GetConversationAsync(1,3,10,default));
        Assert.Equal(unrelated!.Id,third.Id);
        Assert.False(third.IsRead);
        f.WriteReadState(pair.Append(third));
    }

    [Fact]
    public async Task UTC038_AcceptsWithoutRequesterMapping()
    {
        output.WriteLine("V09 RespondFriendRequest | Expected: Success; friendship accepted; no friend_accepted push");
        var f = new Fixture(output);
        f.Friends.AddPendingRequest(100, 1, 2);
        f.Store.Override = (method, _) => method == "GetFriendsAsync" ? Task.FromResult<IReadOnlyList<FriendItem>>([]) : null;
        Assert.Equal(OperationOutcome.Success, (await f.Respond(true)).Outcome);
        Assert.True(await f.Friends.AreFriendsAsync(1, 2, default));
        Assert.Empty(f.Notifications.Sent);
    }

    [Fact]
    public async Task UTC039_RejectsMissingDecision()
    {
        output.WriteLine("V09 RespondFriendRequest | Expected: Invalid invalid_decision; store not called; request stays pending");
        var f = new Fixture(output);
        f.Friends.AddPendingRequest(100, 1, 2);
        f.AssertError(await f.Respond(null), OperationOutcome.Invalid, "invalid_decision");
        Assert.Empty(f.Store.Calls);
        Assert.Single(await f.Friends.GetPendingRequestsAsync(2, default));
        Assert.Empty(f.Notifications.Sent);
    }

    private sealed class Fixture
    {
        private readonly ITestOutputHelper output;
        private readonly CommunityService service;
        public FakeFriendshipStore Friends { get; } = new();
        public FakeNotificationService Notifications { get; } = new();
        public CommunityStoreProbe<IFriendshipStore> Store { get; }

        public Fixture(ITestOutputHelper output)
        {
            this.output = output;
            var store = CommunityStoreProbe<IFriendshipStore>.Create(Friends);
            Store = (CommunityStoreProbe<IFriendshipStore>)(object)store;
            service = new CommunityService(store, Notifications);
        }

        public IEnumerable<object?> Args(string method) => Store.Calls.Single(call => call.Method == method).Args;

        public Task<OperationResult> Search(string query, int limit) =>
            Trace("SearchMembers", new { userId = 1, query, limit }, () => service.SearchMembers(1, "A", query, limit));

        public Task<OperationResult> SendRequest(long target) =>
            Trace("SendFriendRequest", new { userId = 1, targetPlayerId = target },
                () => service.SendFriendRequest(1, "A", new SendFriendRequest(target), default));

        public Task<OperationResult> Respond(bool? accept, long actor = 2) =>
            Trace("RespondFriendRequest", new { userId = actor, requestId = 100, accept },
                () => service.RespondFriendRequest(actor, "B", 100, new RespondFriendRequest(accept), default));

        public Task<OperationResult> Conversation() =>
            Trace("GetConversation", new { userId = 1, friendPlayerId = 2, limit = 10 },
                () => service.GetConversation(1, "A", 2, 10));

        public Task<OperationResult> Send(string content) =>
            Trace("SendMessage", new { userId = 1, recipientPlayerId = 2, content = ContentPreview(content), trimmedLength = content.Trim().Length },
                () => service.SendMessage(1, "A", new SendMessageRequest(2, content), default));

        public Task<OperationResult> Read() =>
            Trace("MarkMessagesAsRead", new { userId = 1, senderPlayerId = 2 },
                () => service.MarkMessagesAsRead(1, "A", 2, default));

        private static string ContentPreview(string content) =>
            content.Length <= 100 ? content : $"{content[..12]}... ({content.Length} characters)";

        private async Task<OperationResult> Trace(string method, object input, Func<Task<OperationResult>> run)
        {
            output.WriteLine($"Input ({method}): {JsonSerializer.Serialize(input)}");
            var actual = await run();
            object? value = actual.Value is ChatMessageItem msg
                ? new { msg.Id, msg.RecipientPlayerId, Content = ContentPreview(msg.Content), Length = msg.Content.Length }
                : actual.Value;
            output.WriteLine($"Actual: {actual.Outcome} {JsonSerializer.Serialize(value)}");
            output.WriteLine($"Pushes: {JsonSerializer.Serialize(Notifications.Sent.Select(push => new { push.TargetUserId, push.Body, push.Data }))}");
            return actual;
        }

        public void AssertError(OperationResult result, OperationOutcome outcome, string error)
        {
            Assert.Equal(outcome, result.Outcome);
            Assert.Equal(error, JsonSerializer.SerializeToElement(result.Value).GetProperty("code").GetString());
        }

        public void AssertPush(long user, string type, string key, string value)
        {
            var push = Assert.Single(Notifications.Sent);
            Assert.Equal(user, push.TargetUserId);
            Assert.NotNull(push.Data);
            Assert.Equal(type, push.Data["type"]);
            Assert.Equal(value, push.Data[key]);
        }

        public void AssertNoMessage()
        {
            Assert.Empty(Notifications.Sent);
            Assert.DoesNotContain(Store.Calls, call => call.Method == "SendMessageAsync");
        }

        public void WriteReadState(IEnumerable<ChatMessageItem> messages) =>
            output.WriteLine($"Read state: {JsonSerializer.Serialize(messages.Select(m => new { m.Id, m.SenderPlayerId, m.RecipientPlayerId, m.IsRead }))}");
    }
}

public class CommunityStoreProbe<T> : DispatchProxy where T : class
{
    private T? target;
    public Func<string, object?[], object?>? Override { get; set; }
    public List<(string Method, object?[] Args)> Calls { get; } = [];

    public static T Create(T? target = null)
    {
        var proxy = Create<T, CommunityStoreProbe<T>>();
        ((CommunityStoreProbe<T>)(object)proxy).target = target;
        return proxy;
    }

    protected override object? Invoke(MethodInfo? method, object?[]? args)
    {
        ArgumentNullException.ThrowIfNull(method);
        args ??= [];
        Calls.Add((method.Name, args));
        if (Override?.Invoke(method.Name, args) is { } result)
            return result;
        if (target is not null)
            return method.Invoke(target, args);
        throw new InvalidOperationException($"Unexpected store call: {method.Name}");
    }
}
