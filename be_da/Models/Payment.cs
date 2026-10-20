using System;
using System.Collections.Generic;

namespace be_da.Models;

public partial class Payment
{
    public long Id { get; set; }

    public long? OrderId { get; set; }

    public long? GoodsOrderId { get; set; }

    public long? PayerId { get; set; }

    public int AmountVnd { get; set; }

    public string? Method { get; set; }

    public string Status { get; set; } = null!;

    public string ReferenceCode { get; set; } = null!;

    public long? ConfirmedBy { get; set; }

    public virtual ICollection<BankTransaction> BankTransactions { get; set; } = new List<BankTransaction>();

    public virtual User? ConfirmedByNavigation { get; set; }

    public virtual Order? GoodsOrder { get; set; }

    public virtual SocialBookingOrder? Order { get; set; }

    public virtual User? Payer { get; set; }

    public virtual ICollection<Refund> Refunds { get; set; } = new List<Refund>();
}
