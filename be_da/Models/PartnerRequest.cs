using System;
using System.Collections.Generic;

namespace be_da.Models;

public partial class PartnerRequest
{
    public long Id { get; set; }

    public long SessionId { get; set; }

    public long RequesterId { get; set; }

    public long PartnerId { get; set; }

    public string? Status { get; set; }

    public virtual Player Partner { get; set; } = null!;

    public virtual Player Requester { get; set; } = null!;

    public virtual Session Session { get; set; } = null!;
}
