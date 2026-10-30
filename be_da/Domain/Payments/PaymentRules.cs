namespace TKSC.Domain.Payments;

public static class PaymentRules
{
    public static string Decide(int expectedAmount, long receivedAmountVnd, bool success,
        DateTimeOffset? expiresAt, DateTimeOffset now, bool ticketsEligible)
    {
        if (receivedAmountVnd != expectedAmount) return "review_required";
        if (!success) return "failed";
        if (expiresAt is null || expiresAt <= now || !ticketsEligible) return "review_required";
        return "paid";
    }
}
