using System;
using System.Collections.Generic;

namespace TKSC.Domain.Entities;

public partial class StockMovement
{
    public long Id { get; set; }

    public long StockItemId { get; set; }

    public string? MovementType { get; set; }

    public int Quantity { get; set; }

    public string? ReferenceType { get; set; }

    public long? ReferenceId { get; set; }

    public long? CreatedBy { get; set; }

    public virtual User? CreatedByNavigation { get; set; }

    public virtual StockItem StockItem { get; set; } = null!;
}
