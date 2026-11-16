using be_da.Application.Interfaces.External;
using be_da.Infrastructure.ExternalServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using TKSC.Application.DTOs;
using TKSC.Application.Interfaces.External;
using TKSC.Application.Interfaces.Repositories;
using TKSC.Application.Services;
using TKSC.Application.Services.Notifications;
using TKSC.Domain.Entities;
using TKSC.Infrastructure.Persistence.Contexts;
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
    [Fact]
    public async Task PushDeduplicatesTokensAndUsesExactTicketAndOrderLinks()
    {
        await using var db = new Sep490Context(new DbContextOptionsBuilder<Sep490Context>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        db.Users.Add(new User{Id=7,FullName="Test",Email="test@example.test",Status="Active"});
        db.UserFcmTokens.AddRange(new UserFcmToken{Id=1,UserId=7,FcmToken="same"},new UserFcmToken{Id=2,UserId=7,FcmToken="same"},new UserFcmToken{Id=3,UserId=8,FcmToken="other"});
        await db.SaveChangesAsync();
        var transport=new PushTransport();
        var service=new FirebaseNotificationService(db,transport,Config(),NullLogger<FirebaseNotificationService>.Instance);
        Assert.Equal("sent",await service.DeliverAsync(7,"Title","Body",new Dictionary<string,string>{{"ticketId","42"}},default));
        Assert.Single(transport.Payloads);
        Assert.EndsWith("?view=tickets&ticketId=42",transport.Payloads[0]["url"]);
        await service.DeliverAsync(7,"Title","Body",new Dictionary<string,string>{{"goodsOrderId","82"}},default);
        Assert.EndsWith("?view=goods&goodsOrderId=82",transport.Payloads[1]["url"]);
        Assert.Equal("suppressed",await service.DeliverAsync(99,"Title","Body",null,default));
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

[Collection("SQL Server")]
public class NotificationDeliveryDatabaseTests
{
    [DatabaseFact]
    public async Task RetryChecksOwnershipAndAuditAndExpiredTokensAreRemoved()
    {
        await using var db=new Sep490Context(new DbContextOptionsBuilder<Sep490Context>().UseSqlServer(Environment.GetEnvironmentVariable("TKSC_TEST_SQL_CONNECTION")).Options);
        await using var tx=await db.Database.BeginTransactionAsync();
        try {
            var user=new User{FullName="Notification probe",Email=$"notification-{Guid.NewGuid():N}@example.invalid",Status="Active",RoleId=await db.Roles.Where(r=>r.Code=="Player").Select(r=>r.Id).FirstAsync()};
            db.Users.Add(user); await db.SaveChangesAsync();
            var email=new Notification{UserId=user.Id,Channel="email",ReferenceType="ticket_paid",Status="failed"};
            db.Notifications.Add(email); db.UserFcmTokens.Add(new UserFcmToken{UserId=user.Id,FcmToken="expired-test-token",UpdatedAt=DateTimeOffset.UtcNow}); await db.SaveChangesAsync();
            var repository=new NotificationDeliveryRepository(db);
            Assert.Equal(404,await repository.RetryAsync(-1,email.Id,default));
            Assert.Equal(202,await repository.RetryAsync(user.Id,email.Id,default));
            Assert.Equal(409,await repository.RetryAsync(user.Id,email.Id,default));
            email.Status="failed"; await db.SaveChangesAsync();
            Assert.Equal(409,await repository.RetryAsync(user.Id,email.Id,default));
            Assert.False((await repository.EmailsAsync(user.Id,default)).Single().CanRetry);
            var config=new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?>{{"Email:WebUrl","https://staging.example.test"}}).Build();
            var service=new FirebaseNotificationService(db,new NotificationDeliveryTests.PushTransport(),config,NullLogger<FirebaseNotificationService>.Instance);
            Assert.Equal("no_device",await service.DeliverAsync(user.Id,"Test","Test",null,default));
            Assert.False(await db.UserFcmTokens.AnyAsync(t=>t.UserId==user.Id));
        } finally { await tx.RollbackAsync(); }
    }
}
