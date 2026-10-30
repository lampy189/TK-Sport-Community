using System;
using System.Collections.Generic;

namespace TKSC.Domain.Entities;

public partial class Message
{
    public long Id { get; set; }

    public long SenderId { get; set; }

    public long RecipientId { get; set; }

    public string? Content { get; set; }

    public bool? IsRead { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public virtual User Recipient { get; set; } = null!;

    public virtual User Sender { get; set; } = null!;
}
