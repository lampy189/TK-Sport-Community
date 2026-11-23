using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TKSC.Application.DTOs;
using TKSC.Application.Services;
using TKSC.Domain.Entities;
using TKSC.Domain.Exceptions;
using TKSC.Infrastructure.Persistence.Contexts;
using TKSC.Infrastructure.Persistence.Repositories;
using Xunit;

namespace TKSC.Api.Tests;
[Collection("SQL Server")]
public class RecoveryWorkflowDatabaseTests
{
    [DatabaseFact]
    public async Task RefundAndIssueWorkflowAreScopedIndependentIdempotentAndRollback()
    {
        var scope=new MutableTestDataScope();
        await using var db = new Sep490Context(new DbContextOptionsBuilder<Sep490Context>().UseSqlServer(Environment.GetEnvironmentVariable("TKSC_TEST_SQL_CONNECTION")).Options,scope);
        await db.Database.OpenConnectionAsync();
        await db.Database.ExecuteSqlRawAsync("EXEC sys.sp_set_session_context @key=N'tksc_role',@value=N'service';");
        await using var tx = await db.Database.BeginTransactionAsync();
        try
        {
            var tag = Guid.NewGuid().ToString("N");
            var roles = await db.Roles.ToDictionaryAsync(r => r.Code.ToLower());
            var venue = new Venue
            {
                Name = "Recovery rollback " + tag,
                IsActive = true
            };
            var other = new Venue
            {
                Name = "Foreign recovery " + tag,
                IsActive = true
            };
            db.Venues.AddRange(venue, other);
            await db.SaveChangesAsync();
            User Person(string label, string role, long? center) => new()
            {
                Email = $"{label}-{tag}@example.invalid",
                FullName = label,
                RoleId = roles[role].Id,
                Status = "Active",
                VenueId = center
            };
            var manager = Person("Manager", "manager", venue.Id);
            var staff = Person("Staff", "staff", venue.Id);
            var owner = Person("Owner", "owner", null);
            var payer = Person("External payer", "player", other.Id);
            var player = new Player
            {
                User = Person("Holder", "player", venue.Id)
            };
            db.Users.AddRange(manager, staff, owner, payer);
            db.Players.Add(player);
            await db.SaveChangesAsync();
            var sport = await db.Sports.FirstAsync();
            var slot = await db.TimeSlots.FirstAsync();
            var session = new Session
            {
                Court = new()
                {
                    VenueId = venue.Id,
                    SportId = sport.Id,
                    Name = "Recovery",
                    IsActive = true
                },
                SportId = sport.Id,
                TimeSlotId = slot.Id,
                SessionDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(3)),
                Band = "B3",
                Capacity = 8,
                Status = "Published",
                CreatedBy = manager.Id
            };
            var order = new SocialBookingOrder
            {
                OrderCode = "RECOVERY-" + tag,
                PayerId = payer.Id,
                Status = "paid",
                TotalAmount = 100
            };
            var ticket = new Ticket
            {
                PlayerId = player.Id,
                Session = session,
                Order = order,
                AmountVnd = 80,
                Status = "cancelled"
            };
            db.Tickets.Add(ticket);
            await db.SaveChangesAsync();
            var payment = new Payment
            {
                OrderId = order.Id,
                PayerId = payer.Id,
                AmountVnd = 100,
                Status = "paid",
                Method = "test",
                ReferenceCode = "RECOVERY-" + tag
            };
            db.Payments.Add(payment);
            await db.SaveChangesAsync();
            db.TicketFunding.Add(new() { TicketId = ticket.Id, PaymentId = payment.Id, AmountVnd = 80 });
            var refund = new Refund
            {
                PaymentId = payment.Id,
                TicketId = ticket.Id,
                RecipientId = payer.Id,
                AmountVnd = 80,
                InitiatedBy = manager.Id,
                Status = "requested"
            };
            db.Refunds.Add(refund);
            db.TicketSupportMessages.Add(new() { TicketId = ticket.Id, SenderId = payer.Id, Content = "Payer report from another center", CreatedAt = DateTimeOffset.UtcNow });
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();
            await db.Database.ExecuteSqlInterpolatedAsync($"EXEC sys.sp_set_session_context @key=N'tksc_role',@value=N'Manager'; EXEC sys.sp_set_session_context @key=N'tksc_venue',@value={venue.Id};");
            var aftercare = new TicketAftercareService(new TicketAftercareRepository(db, TimeProvider.System), TimeProvider.System);
            var listed = JsonSerializer.Serialize(await aftercare.RefundsAsync());
            Assert.Contains(refund.Id.ToString(), listed);
            var support = new TicketSupportRepository(db, TimeProvider.System);
            Assert.Single(await support.ListTicketSupportMessagesListAsync(ticket.Id));
            // Owner review is necessary because the initiating manager cannot approve their own refund.
            var self = await Assert.ThrowsAsync<PaymentException>(() => aftercare.DecideRefundAsync(refund.Id, manager.Id, new(true, 80, payer.Id, "Checking own refund must be denied")));
            Assert.Equal(403, self.StatusCode);
            await db.Database.ExecuteSqlRawAsync("EXEC sys.sp_set_session_context @key=N'tksc_role',@value=N'Owner'; EXEC sys.sp_set_session_context @key=N'tksc_venue',@value=NULL;");
            await aftercare.DecideRefundAsync(refund.Id, owner.Id, new(true, 80, payer.Id, "Owner independently checks source and payer"));
            await db.Database.ExecuteSqlInterpolatedAsync($"EXEC sys.sp_set_session_context @key=N'tksc_role',@value=N'Staff'; EXEC sys.sp_set_session_context @key=N'tksc_venue',@value={venue.Id};");
            var payout = new RefundPayoutInput(80, payer.Id, "OUT-" + tag, "Outgoing bank transfer matches recipient and allocation", DateTimeOffset.UtcNow.AddMinutes(-1));
            await aftercare.RecordRefundPayoutAsync(refund.Id, staff.Id, payout);
            await aftercare.RecordRefundPayoutAsync(refund.Id, staff.Id, payout);
            await Assert.ThrowsAsync<PaymentException>(() => aftercare.CompleteRefundAsync(refund.Id, staff.Id, "Self verification is not allowed", payout.BankReference));
            await db.Database.ExecuteSqlInterpolatedAsync($"EXEC sys.sp_set_session_context @key=N'tksc_role',@value=N'Manager'; EXEC sys.sp_set_session_context @key=N'tksc_venue',@value={venue.Id};");
            await aftercare.CompleteRefundAsync(refund.Id, manager.Id, "Independent confirmation of outgoing bank transfer", payout.BankReference);
            await aftercare.CompleteRefundAsync(refund.Id, manager.Id, "Independent confirmation of outgoing bank transfer", payout.BankReference);
            Assert.Equal(1, await db.AuditLogs.CountAsync(a => a.EntityType == "Refund" && a.EntityId == refund.Id && a.Action == "refund_completed"));
            var repo = new IncidentRepository(db);
            var workflow = new IncidentWorkflowService(repo, TimeProvider.System);
            var claimed = (IncidentState)await workflow.Act(staff.Id, "inbox", ticket.Id, new("claim", "Staff takes responsibility for reported issue", 0));
            await Assert.ThrowsAsync<PaymentException>(() => workflow.Act(manager.Id, "inbox", ticket.Id, new("resolve", "Stale decision must not overwrite assignment", 0)));
            var closed = (IncidentState)await workflow.Act(staff.Id, "inbox", ticket.Id, new("resolve", "Original payer received refund and issue is resolved", claimed.Version));
            db.TicketSupportMessages.Add(new() { TicketId = ticket.Id, SenderId = payer.Id, Content = "New report after issue was closed", CreatedAt = closed.At.AddSeconds(1) });
            await db.SaveChangesAsync();
            Assert.Equal("open", (await repo.State("inbox", ticket.Id)).Status);
            var incoming = "SURPLUS-" + tag;
            var paid = await db.Payments.SingleAsync(p => p.Id == payment.Id);
            paid.Status = "review_required";
            paid.Method = "payos";
            paid.GatewayPaymentLinkId = "LINK";
            var original = new BankTransaction
            {
                MatchedPaymentId = payment.Id,
                ExternalId = "payos:ORIGINAL-" + tag,
                AmountVnd = 100,
                MatchStatus = "matched"
            };
            var surplus = new BankTransaction
            {
                MatchedPaymentId = payment.Id,
                ExternalId = "payos:" + incoming,
                AmountVnd = 50,
                MatchStatus = "review_required"
            };
            db.BankTransactions.AddRange(original, surplus);
            await db.SaveChangesAsync();
            var gateway = new TicketRecoveryTests.Gateway
            {
                Remote = new(payment.Id, 150, "PAID", "LINK", [new(payment.Id, 100, "ORIGINAL-" + tag, "LINK", "VND", true, "fixture"), new(payment.Id, 50, incoming, "LINK", "VND", true, "fixture")])
            };
            Assert.Contains(payment.Id.ToString(), JsonSerializer.Serialize(await aftercare.ReviewsAsync()));
            var returns = new IncidentWorkflowService(repo, TimeProvider.System, gateway);
            var returnCase = (OperationalCase)await returns.Request(staff.Id, new("reviews", payment.Id, "return_unallocated", "Duplicate transfer is not allocated to issued tickets", BankTransactionId: surplus.Id));
            await returns.Decide(manager.Id, returnCase.Id, new(true, "Verified duplicate transfer against payOS bank data"));
            await returns.RecordReturn(staff.Id, returnCase.Id, new(50, payer.Id, "RETURN-" + tag, "Outgoing transfer evidence for duplicate receipt", DateTimeOffset.UtcNow.AddMinutes(-1)));
            Assert.Equal((await repo.Actor(manager.Id)).VenueId, (await repo.Resource("reviews", payment.Id))!.VenueId);
            Assert.Equal("reviews", (await repo.Case(returnCase.Id))!.Kind);
            Assert.Equal(payment.Id, (await repo.Case(returnCase.Id))!.SubjectId);
            await returns.VerifyReturn(manager.Id, returnCase.Id, "Independent outgoing transfer verified against original payer");
            Assert.Equal((await repo.Actor(manager.Id)).VenueId, (await repo.Resource("reviews", payment.Id))!.VenueId);
            Assert.Equal("reviews", (await repo.Case(returnCase.Id))!.Kind);
            Assert.Equal(payment.Id, (await repo.Case(returnCase.Id))!.SubjectId);
            await returns.VerifyReturn(manager.Id, returnCase.Id, "Retry after successful independent verification");
            Assert.Equal("refunded", (await db.BankTransactions.SingleAsync(b => b.Id == surplus.Id)).MatchStatus);
            Assert.Equal("paid", (await db.Payments.SingleAsync(p => p.Id == payment.Id)).Status);
            await returns.Close(manager.Id, returnCase.Id, "Duplicate receipt returned, source payment reconciled");
            var sku = "ROLL-" + tag;
            await db.Database.ExecuteSqlInterpolatedAsync($"INSERT dbo.products(sku,name,unit_price_vnd,is_active,venue_id) VALUES({sku},{sku},100,1,{venue.Id});");
            var product = await db.Products.SingleAsync(p => p.Sku == sku);
            db.StockItems.Add(new() { ProductId = product.Id, QuantityOnHand = 10 });
            await db.SaveChangesAsync();
            var proposal = (OperationalCase)await workflow.Request(staff.Id, new("venues", venue.Id, "stock_adjustment", "Physical count confirms two damaged items to remove", product.Id, -2));
            await workflow.Decide(manager.Id, proposal.Id, new(true, "Independent inventory count agrees with proposal"));
            await workflow.Execute(staff.Id, proposal.Id, "Apply approved correction with count evidence");
            await workflow.Execute(staff.Id, proposal.Id, "Retry same approved inventory correction");
            Assert.Equal(8, (await db.StockItems.SingleAsync(x => x.ProductId == product.Id)).QuantityOnHand);
            Assert.Equal(1, await db.StockMovements.CountAsync(m => m.ReferenceType == "OperationalException" && m.ReferenceId == proposal.Id));
            await workflow.Close(manager.Id, proposal.Id, "Verified stock balance and immutable movement");
            await db.Database.ExecuteSqlInterpolatedAsync($"EXEC sys.sp_set_session_context @key=N'tksc_role',@value=N'Staff'; EXEC sys.sp_set_session_context @key=N'tksc_venue',@value={other.Id};");
            scope.Role="Staff";scope.VenueId=other.Id;
            Assert.Null(await repo.Resource("refunds", refund.Id));
        }
        finally
        {
            await tx.RollbackAsync();
            await db.Database.ExecuteSqlRawAsync("EXEC sys.sp_set_session_context @key=N'tksc_role',@value=N'service'; EXEC sys.sp_set_session_context @key=N'tksc_venue',@value=NULL;");
        }
    }
}
