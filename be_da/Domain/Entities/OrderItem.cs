using System;
using System.Collections.Generic;

namespace TKSC.Domain.Entities;

public partial class OrderItem
{
    public long Id { get; set; }

    public long OrderId { get; set; }

    public long ProductId { get; set; }

    public short Quantity { get; set; }

    public int UnitPriceVnd { get; set; }

    public int SubtotalVnd { get; set; }

    public virtual Order Order { get; set; } = null!;

    public virtual Product Product { get; set; } = null!;
}
