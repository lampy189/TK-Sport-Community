using System;
using System.Collections.Generic;

namespace TKSC.Domain.Entities;

public partial class StockCount
{
    public long Id { get; set; }

    public long StockItemId { get; set; }

    public int CountedQuantity { get; set; }

    public int? SystemQuantity { get; set; }

    public int? Difference { get; set; }

    public long CountedBy { get; set; }

    public long? ApprovedBy { get; set; }

    public virtual User? ApprovedByNavigation { get; set; }

    public virtual User CountedByNavigation { get; set; } = null!;

    public virtual StockItem StockItem { get; set; } = null!;
}
