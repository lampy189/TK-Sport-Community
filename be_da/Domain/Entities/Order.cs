using System;
using System.Collections.Generic;

namespace TKSC.Domain.Entities;

public partial class Order
{
    public long Id { get; set; }

    public string OrderCode { get; set; } = null!;

    public long StaffId { get; set; }

    public string? CustomerName { get; set; }

    public int TotalAmount { get; set; }

    public string Status { get; set; } = null!;

    public string? PaymentState { get; set; }

    public string? HandoverState { get; set; }

<<<<<<< HEAD
=======
    public long? BuyerId { get; set; }

>>>>>>> 0d40ca35efaf07c5d4887d6f8febc66c57f5578c
    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();

    public virtual User Staff { get; set; } = null!;
}
