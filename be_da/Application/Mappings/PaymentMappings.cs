using System.Linq.Expressions;
using TKSC.Application.DTOs;
using TKSC.Domain.Entities;

namespace TKSC.Application.Mappings;

public static class PaymentMappings
{
    public static OrderSummary ToSummary(SocialBookingOrder order) => new(
        order.Id, order.OrderCode, order.TotalAmount, order.Status.ToLowerInvariant(), order.ExpiresAt,
        order.Payments.OrderByDescending(p => p.Id)
            .Select(p => new PaymentSummary(p.Id, p.ReferenceCode, p.Status, p.AmountVnd)).ToList(),
        order.Tickets.Select(t => new TicketSummary(t.Id, t.OrderId, t.PlayerId, t.SessionId,
            t.AmountVnd, t.Status.ToLower(), t.TicketCode, order.Status == "paid" && t.Status == "confirmed",
            null, null, null)).ToList());

    public static Expression<Func<Ticket, TicketSummary>> TicketProjection => t => new(
        t.Id, t.OrderId, t.PlayerId, t.SessionId, t.AmountVnd, t.Status.ToLower(), t.TicketCode,
        t.Order.Status == "paid" && t.Status == "confirmed", t.Session.SessionDate,
        t.Session.TimeSlot.StartTime, t.Session.TimeSlot.EndTime);
}
