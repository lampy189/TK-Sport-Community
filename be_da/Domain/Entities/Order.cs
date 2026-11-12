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

    public long? BuyerId { get; set; }

    // Center of the sale (upgrade 011); the database fills it from the connection context when not set.
    public long? VenueId { get; set; }

    // Upgrade 017: stock hold of the order (NULL for orders created before reservations existed).
    public DateTimeOffset? CreatedAt { get; set; }

    public DateTimeOffset? ExpiresAt { get; set; }

    public string? ReservationState { get; set; }

    public long? CancelledBy { get; set; }

    public DateTimeOffset? CancelledAt { get; set; }

    public long? DeliveredBy { get; set; }

    public DateTimeOffset? DeliveredAt { get; set; }

    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();

    public virtual User Staff { get; set; } = null!;
}
