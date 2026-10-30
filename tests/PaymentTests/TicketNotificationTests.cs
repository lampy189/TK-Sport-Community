using TKSC.Application.Notifications;
using TKSC.Infrastructure.Notifications;
using Xunit;

public class TicketNotificationTests
{
    [Theory]
    [InlineData("Pending", "confirmed", "Pending", "paid", "ticket_paid")]
    [InlineData("confirmed", "cancelled", "paid", "paid", "ticket_refund_requested")]
    [InlineData("Pending", "cancelled", "Pending", "cancelled", "ticket_cancelled")]
    [InlineData("confirmed", "checkedin", "paid", "paid", "ticket_checked_in")]
    [InlineData("reserved", "reserved", "pending", "review_required", "ticket_payment_review")]
    public void CommittedTransitionsHaveCorrectMessages(string before, string after, string oldOrder, string newOrder, string expected)
        => Assert.Equal(expected, TicketNotifications.Event(before, after, oldOrder, newOrder));

    [Theory]
    [InlineData("confirmed", "paid")]
    [InlineData("checkedin", "paid")]
    [InlineData("cancelled", "paid")]
    [InlineData("reserved", "review_required")]
    public void RepeatedWebhooksAndCheckInDoNotGenerateAnotherEvent(string state, string order)
        => Assert.Null(TicketNotifications.Event(state, state, order, order));

    [Fact]
    public void PayerAndHolderEachReceiveWebAndEmailWithoutSelfDuplicates()
    {
        var self = TicketNotifications.Create("ticket_paid", 100, 7, 7).ToList();
        Assert.Equal(2, self.Count);
        Assert.Single(self, n => n.Channel == "web" && n.Status == "unread");
        Assert.Single(self, n => n.Channel == "email" && n.Status == "pending");
        var friends = TicketNotifications.Create("ticket_paid", 100, 7, 8).ToList();
        Assert.Equal(4, friends.Count);
        Assert.All(friends, n => Assert.Equal(100L, n.ReferenceId));
    }
    [Fact]
    public void CancellationDoesNotAnnounceCompletedRefund()
    {
        var title = TicketNotifications.Title("ticket_refund_requested", 10);
        Assert.Contains("chưa được hoàn", title);
        Assert.DoesNotContain("đã hoàn tiền", title);
    }
    [Fact]
    public void FailedMailRetriesAreBoundedAndSentMailIsNeverRetried()
    {
        var status = "pending";
        for (var attempt = 0; attempt < 5; attempt++)
        { Assert.True(TicketEmailWorker.IsPending(status)); status = TicketEmailWorker.NextFailure(status); }
        Assert.Equal("failed", status);
        Assert.False(TicketEmailWorker.IsPending("sent"));
        Assert.False(TicketEmailWorker.IsPending("suppressed"));
        Assert.False(TicketEmailWorker.IsPending(status));
    }
}
