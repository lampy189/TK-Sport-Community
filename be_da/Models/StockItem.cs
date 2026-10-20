using System;
using System.Collections.Generic;

namespace be_da.Models;

public partial class StockItem
{
    public long Id { get; set; }

    public long ProductId { get; set; }

    public int QuantityOnHand { get; set; }

    public int ReorderLevel { get; set; }

    public virtual Product Product { get; set; } = null!;

    public virtual ICollection<StockCount> StockCounts { get; set; } = new List<StockCount>();

    public virtual ICollection<StockMovement> StockMovements { get; set; } = new List<StockMovement>();
}
