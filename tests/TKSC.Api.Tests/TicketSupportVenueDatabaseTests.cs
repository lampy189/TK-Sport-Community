using Microsoft.EntityFrameworkCore;
using TKSC.Application.Services;
using TKSC.Domain.Entities;
using TKSC.Infrastructure.DbContext;
using Xunit;

namespace TKSC.Api.Tests;

[Collection("SQL Server")]
public sealed class TicketSupportVenueDatabaseTests
{
    [DatabaseFact]
    public async Task PlayerContextNotifiesOnlyTheTicketVenueAndRollsBack()
    {
        await using var db = new Sep490Context(new DbContextOptionsBuilder<Sep490Context>()
            .UseSqlServer(Environment.GetEnvironmentVariable("TKSC_TEST_SQL_CONNECTION")).Options);
        await db.Database.OpenConnectionAsync();
        await db.Database.ExecuteSqlRawAsync("EXEC sys.sp_set_session_context @key=N'tksc_role',@value=N'service';");
        await using var tx = await db.Database.BeginTransactionAsync();
        try
        {
            var tag = Guid.NewGuid().ToString("N");
            var roles = await db.Roles.ToDictionaryAsync(r => r.Code.ToLower());
            var venue = new Venue { Name = "Report scope " + tag, IsActive = true };
            var other = new Venue { Name = "Other report scope " + tag, IsActive = true };
            db.Venues.AddRange(venue, other);
            await db.SaveChangesAsync();
            User Reviewer(string suffix, string role, long? venueId) => new()
            {
                Email = $"{suffix}-{tag}@example.invalid", FullName = suffix,
                RoleId = roles[role].Id, Status = "Active", VenueId = venueId
            };
            var player = new Player { User = Reviewer("holder", "player", null) };
            var staff = Reviewer("staff", "staff", venue.Id);
            var manager = Reviewer("manager", "manager", venue.Id);
            var otherStaff = Reviewer("other-staff", "staff", other.Id);
            var unassigned = Reviewer("unassigned-manager", "manager", null);
            db.Users.AddRange(staff, manager, otherStaff, unassigned);
            db.Players.Add(player);
            await db.SaveChangesAsync();
            var sport = await db.Sports.FirstAsync();
            var slot = await db.TimeSlots.FirstAsync();
            var session = new Session
            {
                Court = new Court { VenueId = venue.Id, SportId = sport.Id, Name = "Report court", IsActive = true },
                SportId = sport.Id, TimeSlotId = slot.Id, SessionDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(2)),
                Band = "B3", Capacity = 8, Status = "Published", CreatedBy = manager.Id
            };
            var order = new SocialBookingOrder { OrderCode = "REPORT-" + tag, PayerId = player.UserId, Status = "pending" };
            var ticket = new Ticket { PlayerId = player.Id, Session = session, Order = order, Status = "reserved" };
            db.Tickets.Add(ticket);
            await db.SaveChangesAsync();
            var ticketId = ticket.Id;
            var actorId = player.UserId;
            var expected = new[] { staff.Id, manager.Id }.OrderBy(id => id).ToArray();
            db.ChangeTracker.Clear();
            // Players bypass the venue RLS filter, so the service must scope recipients itself.
            await db.Database.ExecuteSqlRawAsync("EXEC sys.sp_set_session_context @key=N'tksc_role',@value=N'Player';");
            await new TicketSupportService(db, TimeProvider.System)
                .Report(ticketId, actorId, "court", "The court is damaged and cannot be used.", null);
            var recipients = await db.Notifications.Where(n => n.ReferenceType == "ticket_player_message" && n.ReferenceId == ticketId)
                .Select(n => n.UserId).OrderBy(id => id).ToArrayAsync();
            Assert.Equal(expected, recipients);
        }
        finally
        {
            await tx.RollbackAsync();
            await db.Database.ExecuteSqlRawAsync("EXEC sys.sp_set_session_context @key=N'tksc_role',@value=N'service'; EXEC sys.sp_set_session_context @key=N'tksc_venue',@value=NULL;");
        }
    }
}
