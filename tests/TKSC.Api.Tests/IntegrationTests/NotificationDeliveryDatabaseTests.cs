using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using be_da.Infrastructure.ExternalServices;
using TKSC.Application.Services.Notifications;
using TKSC.Domain.Entities;
using TKSC.Infrastructure.Persistence.Contexts;
using TKSC.Infrastructure.Persistence.Repositories;
using Xunit;

namespace TKSC.Api.Tests;

[Collection("SQL Server")]
public class NotificationDeliveryDatabaseTests
{
    [DatabaseFact]
    public async Task RetryChecksOwnershipAndAuditAndExpiredTokensAreRemoved()
    {
        await using var db = new Sep490Context(new DbContextOptionsBuilder<Sep490Context>().UseSqlServer(Environment.GetEnvironmentVariable("TKSC_TEST_SQL_CONNECTION")).Options);
        await using var tx = await db.Database.BeginTransactionAsync();
        try
        {
            var user = new User { FullName = "Notification probe", Email = $"notification-{Guid.NewGuid():N}@example.invalid", Status = "Active", RoleId = await db.Roles.Where(r => r.Code == "Player").Select(r => r.Id).FirstAsync() };
            db.Users.Add(user);
            await db.SaveChangesAsync();
            var email = new Notification { UserId = user.Id, Channel = "email", ReferenceType = "ticket_paid", Status = "failed" };
            db.Notifications.Add(email);
            db.UserFcmTokens.Add(new UserFcmToken { UserId = user.Id, FcmToken = "expired-test-token", UpdatedAt = DateTimeOffset.UtcNow });
            await db.SaveChangesAsync();
            var repository = new NotificationDeliveryRepository(db);
            Assert.Equal(404, await repository.RetryAsync(-1, email.Id, default));
            Assert.Equal(202, await repository.RetryAsync(user.Id, email.Id, default));
            Assert.Equal(409, await repository.RetryAsync(user.Id, email.Id, default));
            email.Status = "failed";
            await db.SaveChangesAsync();
            Assert.Equal(409, await repository.RetryAsync(user.Id, email.Id, default));
            Assert.False((await repository.EmailsAsync(user.Id, default)).Single().CanRetry);
            var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["Email:WebUrl"] = "https://staging.example.test" }).Build();
            var service = new FirebaseNotificationService(db, new NotificationDeliveryTests.PushTransport(), config, NullLogger<FirebaseNotificationService>.Instance);
            Assert.Equal("no_device", await service.DeliverAsync(user.Id, "Test", "Test", null, default));
            Assert.False(await db.UserFcmTokens.AnyAsync(t => t.UserId == user.Id));
        }
        finally
        {
            await tx.RollbackAsync();
        }
    }
}
