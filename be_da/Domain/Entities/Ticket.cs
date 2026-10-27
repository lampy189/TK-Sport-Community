using System;
using System.Collections.Generic;

namespace TKSC.Domain.Entities;

public partial class Ticket
{
    public long Id { get; set; }

    public long OrderId { get; set; }

    public long SessionId { get; set; }

    public long PlayerId { get; set; }

    public string? TicketCode { get; set; }

    public string Status { get; set; } = null!;

    public int AmountVnd { get; set; }

    public long? CheckedInBy { get; set; }

    public virtual User? CheckedInByNavigation { get; set; }

    public virtual ICollection<Invitation> Invitations { get; set; } = new List<Invitation>();

    public virtual SocialBookingOrder Order { get; set; } = null!;

    public virtual Player Player { get; set; } = null!;

    public virtual ICollection<Refund> Refunds { get; set; } = new List<Refund>();

    public virtual Session Session { get; set; } = null!;
}
