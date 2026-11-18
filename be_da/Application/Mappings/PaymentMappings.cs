using TKSC.Domain.Exceptions;
using System.Linq.Expressions;
using TKSC.Application.DTOs;
using TKSC.Domain.Entities;

namespace TKSC.Application.Mappings;
public static class PaymentMappings
{
    public static OrderSummary ToSummary(SocialBookingOrder order) => new(order.Id, order.OrderCode, order.TotalAmount, order.Status.ToLowerInvariant(), order.ExpiresAt, order.Payments.OrderByDescending(p => p.Id).Select(p => new PaymentSummary(p.Id, p.ReferenceCode, p.Status.ToLowerInvariant(), p.AmountVnd)).ToList(), order.Tickets.Select(t => new TicketSummary(t.Id, t.OrderId, t.PlayerId, t.SessionId, t.AmountVnd, t.Status.ToLowerInvariant(), t.TicketCode, order.Status.ToLowerInvariant() == "paid" && t.Status.ToLowerInvariant() == "confirmed", null, null, null, order.Status.ToLowerInvariant(), order.ExpiresAt, order.Payments.OrderByDescending(p => p.Id).Select(p => p.Status.ToLowerInvariant()).FirstOrDefault(), null, null, null, null, t.CheckedInBy != null || t.Status.ToLowerInvariant() == "checkedin", null, null) { PayerId = order.PayerId }).ToList());
    public static Expression<Func<Ticket, TicketSummary>> TicketProjection => t => new(t.Id, t.OrderId, t.PlayerId, t.SessionId, t.AmountVnd, t.Status.ToLower(), t.TicketCode, t.Order.Status.ToLower() == "paid" && t.Status.ToLower() == "confirmed" && t.Session.Status.ToLower() != "suspended" && t.Session.Status.ToLower() != "cancelled", t.Session.SessionDate, t.Session.TimeSlot.StartTime, t.Session.TimeSlot.EndTime, t.Order.Status.ToLower(), t.Order.ExpiresAt, t.Order.Payments.OrderByDescending(p => p.Id).Select(p => p.Status.ToLower()).FirstOrDefault(), t.Session.Sport.Name, t.Session.Court.Name, t.Session.Court.Venue.Name, t.Player.User.FullName, t.CheckedInBy != null || t.Status.ToLower() == "checkedin", t.Refunds.Any(r => r.Status == "requested") ? "requested" : t.Refunds.Any(r => r.Status == "processing") ? "processing" : t.Refunds.Any(r => r.Status == "failed") ? "failed" : t.Refunds.OrderByDescending(r => r.Id).Select(r => r.Status).FirstOrDefault(), t.Refunds.Where(r => r.Status != "rejected").Sum(r => r.AmountVnd))
    {
        PayerId = t.Order.PayerId,
        PayerName = t.Order.Payer.FullName,
        OrderCode = t.Order.OrderCode,
        PaidForOther = t.Order.PayerId != t.Player.UserId,
        SessionStatus = t.Session.Status.ToLower()
    };
}
