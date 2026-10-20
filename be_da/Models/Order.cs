using System;
using System.Collections.Generic;

namespace be_da.Models;

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

    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();

    public virtual User Staff { get; set; } = null!;
}
