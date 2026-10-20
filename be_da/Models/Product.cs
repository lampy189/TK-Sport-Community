using System;
using System.Collections.Generic;

namespace be_da.Models;

public partial class Product
{
    public long Id { get; set; }

    public string Sku { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string? Category { get; set; }

    public int UnitPriceVnd { get; set; }

    public bool? IsActive { get; set; }

    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

    public virtual StockItem? StockItem { get; set; }
}
