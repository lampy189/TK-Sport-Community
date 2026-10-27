using System;
using System.Collections.Generic;

namespace TKSC.Domain.Entities;

public partial class SocialBookingOrder
{
    public long Id { get; set; }

    public string OrderCode { get; set; } = null!;

    public long PayerId { get; set; }

    public int TotalAmount { get; set; }

    public string Status { get; set; } = null!;

    public DateTimeOffset? ExpiresAt { get; set; }

    public virtual ICollection<Invitation> Invitations { get; set; } = new List<Invitation>();

    public virtual User Payer { get; set; } = null!;

    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();

    public virtual ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
}
