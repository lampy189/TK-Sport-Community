using System;
using System.Collections.Generic;

namespace TKSC.Domain.Entities;

public partial class BankTransaction
{
    public long Id { get; set; }

    public string ExternalId { get; set; } = null!;

    public int AmountVnd { get; set; }

    public string? ReferenceCode { get; set; }

    public long? MatchedPaymentId { get; set; }

    public string? MatchStatus { get; set; }

    public string? RawPayload { get; set; }

    public virtual Payment? MatchedPayment { get; set; }
}
