using System;
using System.Collections.Generic;

namespace TKSC.Domain.Entities;

public partial class Refund
{
    public long Id { get; set; }

    public long PaymentId { get; set; }

    public long TicketId { get; set; }

    public long RecipientId { get; set; }

    public int? AmountVnd { get; set; }

    public string? Status { get; set; }

    public long InitiatedBy { get; set; }

    public long? ApprovedBy { get; set; }

    public virtual User? ApprovedByNavigation { get; set; }

    public virtual User InitiatedByNavigation { get; set; } = null!;

    public virtual Payment Payment { get; set; } = null!;

    public virtual User Recipient { get; set; } = null!;

    public virtual Ticket Ticket { get; set; } = null!;
}
