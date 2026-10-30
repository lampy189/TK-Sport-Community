using System;
using System.Collections.Generic;

namespace TKSC.Domain.Entities;

public partial class Friendship
{
    public long Id { get; set; }

    public long RequesterId { get; set; }

    public long AddresseeId { get; set; }

    public string Status { get; set; } = null!;

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? RespondedAt { get; set; }

    public long PlayerLowId { get; set; }

    public long PlayerHighId { get; set; }

    public virtual Player Addressee { get; set; } = null!;

    public virtual Player Requester { get; set; } = null!;
}
