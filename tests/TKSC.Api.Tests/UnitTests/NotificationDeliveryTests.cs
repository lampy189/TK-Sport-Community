using be_da.Application.Interfaces.External;
using be_da.Infrastructure.ExternalServices;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using TKSC.Application.DTOs;
using TKSC.Application.Interfaces.External;
using TKSC.Application.Interfaces.Repositories;
using TKSC.Application.Services;
using TKSC.Application.Services.Notifications;
using TKSC.Domain.Entities;
using TKSC.Infrastructure.Persistence.Repositories;
using Xunit;

namespace TKSC.Api.Tests;

public class NotificationDeliveryTests
{
    private static IConfiguration Config(string url = "https://staging.example.test") => new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?> {
        ["Email:Username"] = "sender@example.test", ["Email:Password"] = "test-only", ["Email:WebUrl"] = url,
        ["Firebase:Enabled"] = "true"
    }).Build();

    [Theory]
    [InlineData("ticket_refund_requested")]
    [InlineData("ticket_difference_refund")]
    public async Task EmailContainsExactEnvironmentAndTicketWithoutClaimingRefund(string kind)
    {
        var repo = new MailRepository { Item = new() { Id=1, UserId=7, Channel="email", ReferenceId=42, ReferenceType=kind, Status="pending", Title=TicketNotifications.Title(kind,42) } };
        var sender = new MailSender();
        var service = new TicketEmailService(repo,sender,Config(),NullLogger<TicketEmailService>.Instance);
        await service.DeliverAsync(1,default);
        Assert.Equal("sent",repo.Item.Status);
        Assert.Contains("https://staging.example.test/player/home?view=tickets&ticketId=42",sender.Mail!.Body);
        Assert.Contains("chưa được hoàn",sender.Mail.Subject);
        await service.DeliverAsync(1,default);
        Assert.Equal(1,sender.Calls);
    }
    [Fact]
    public async Task MailFailuresAreBoundedAndDoNotChangeBusinessData()
    {
        var repo = new MailRepository(); var sender = new MailSender { Fail=true };
        var service = new TicketEmailService(repo,sender,Config(),NullLogger<TicketEmailService>.Instance);
        for(var i=0;i<7;i++) await service.DeliverAsync(1,default);
        Assert.Equal("failed",repo.Item.Status); Assert.Equal(5,sender.Calls);
    }
    [Theory]
    [InlineData("")]
    [InlineData("http://production.example.test")]
    [InlineData("https://user:password@example.test")]
    [InlineData("https://example.test?redirect=evil")]
    public async Task InvalidEnvironmentNeverSendsMail(string url)
    {
        var sender=new MailSender(); var repo=new MailRepository();
        await new TicketEmailService(repo,sender,Config(url),NullLogger<TicketEmailService>.Instance).DeliverAsync(1,default);
        Assert.Equal(0,sender.Calls); Assert.Equal("retry1",repo.Item.Status);
    }
    private sealed class MailRepository : ITicketEmailRepository
    {
        public Notification Item = new() { Id=1,UserId=7,Channel="email",ReferenceId=42,Status="pending" };
        public Task<List<long>> PendingAsync(CancellationToken ct)=>Task.FromResult(new List<long>{1});
        public async Task WithLockedAsync(long id,Func<Notification?,Task<bool>> action,CancellationToken ct,CancellationToken saveToken)=>await action(Item);
        public Task<User> UserAsync(long id,CancellationToken ct)=>Task.FromResult(new User{Id=id,FullName="Test",Email="test@example.test",Status="Active"});
        public Task<TicketEmailDetails?> TicketAsync(long? id,CancellationToken ct)=>Task.FromResult<TicketEmailDetails?>(null);
    }
    private sealed class MailSender : ITicketEmailSender
    {
        public OutgoingEmail? Mail; public bool Fail; public int Calls;
        public Task SendAsync(OutgoingEmail mail,CancellationToken ct) { Calls++; if(Fail)throw new IOException("test failure"); Mail=mail; return Task.CompletedTask; }
    }
    internal sealed class PushTransport : IFirebasePushTransport
    {
        public List<Dictionary<string,string>> Payloads = [];
        public Task<bool> SendAsync(string token,Dictionary<string,string> data,CancellationToken ct) { Payloads.Add(data); return Task.FromResult(token!="expired-test-token"); }
    }
}
