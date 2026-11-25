using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using be_da.Infrastructure.ExternalServices;
using TKSC.Application.Services.Notifications;
using TKSC.Domain.Entities;
using TKSC.Infrastructure.Persistence.Contexts;
using Xunit;

namespace TKSC.Api.Tests;

public sealed class FirebaseNotificationDatabaseTests
{
    [Fact]
    public async Task PushDeduplicatesTokensAndUsesExactTicketAndOrderLinks()
    {
        await using var db = new Sep490Context(new DbContextOptionsBuilder<Sep490Context>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        db.Users.Add(new User { Id = 7, FullName = "Test", Email = "test@example.test", Status = "Active" });
        db.UserFcmTokens.AddRange(
            new UserFcmToken { Id = 1, UserId = 7, FcmToken = "same" },
            new UserFcmToken { Id = 2, UserId = 7, FcmToken = "same" },
            new UserFcmToken { Id = 3, UserId = 8, FcmToken = "other" });
        await db.SaveChangesAsync();
        var transport = new NotificationDeliveryTests.PushTransport();
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Email:Username"] = "sender@example.test",
            ["Email:Password"] = "test-only",
            ["Email:WebUrl"] = "https://staging.example.test",
            ["Firebase:Enabled"] = "true"
        }).Build();
        var service = new FirebaseNotificationService(db, transport, config, NullLogger<FirebaseNotificationService>.Instance);
        Assert.Equal("sent", await service.DeliverAsync(7, "Title", "Body", new Dictionary<string, string> { ["ticketId"] = "42" }, default));
        Assert.Single(transport.Payloads);
        Assert.EndsWith("?view=tickets&ticketId=42", transport.Payloads[0]["url"]);
        await service.DeliverAsync(7, "Title", "Body", new Dictionary<string, string> { ["goodsOrderId"] = "82" }, default);
        Assert.EndsWith("?view=goods&goodsOrderId=82", transport.Payloads[1]["url"]);
        Assert.Equal("suppressed", await service.DeliverAsync(99, "Title", "Body", null, default));
    }
}
