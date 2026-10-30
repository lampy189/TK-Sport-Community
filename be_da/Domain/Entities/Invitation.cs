using System;
using System.Collections.Generic;

namespace TKSC.Domain.Entities;

public partial class Invitation
{
    public long Id { get; set; }

    public long? OrderId { get; set; }

    public long InviterId { get; set; }

    public long InviteeId { get; set; }

    public long SessionId { get; set; }

    public long? TicketId { get; set; }

    public string? Status { get; set; }

    public virtual Player Invitee { get; set; } = null!;

    public virtual Player Inviter { get; set; } = null!;

    public virtual SocialBookingOrder? Order { get; set; }

    public virtual Session Session { get; set; } = null!;

    public virtual Ticket? Ticket { get; set; }
}
