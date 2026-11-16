namespace TKSC.Domain.Payments;

public static class LateEntryRules
{
    public const string CodePrefix = "LATE-";
    public static bool IsLate(string? code) => code?.StartsWith(CodePrefix, StringComparison.Ordinal) == true;
    public static DateTimeOffset End(DateOnly date, TimeOnly time) => new(date.ToDateTime(time), TimeSpan.FromHours(7));
    public static bool Running(DateTimeOffset start, DateTimeOffset end, string status, DateTimeOffset now) =>
        now >= start && now < end && status.ToLowerInvariant() is "open" or "published" or "ongoing";
    public static DateTimeOffset HoldExpiry(DateTimeOffset now, DateTimeOffset end) => now.AddMinutes(5) < end ? now.AddMinutes(5) : end;
    // Quote in whole elapsed minutes so an offer remains stable while the player confirms it.
    // The original price remains the source of truth; the maximum reduction is 50%.
    public static LateEntryPrice? Quote(DateTimeOffset start, DateTimeOffset end, string status, DateTimeOffset now, int basePriceVnd)
    {
        if (!Running(start, end, status, now) || end<=start || basePriceVnd is < 2000 or > 100000000) return null;
        var duration=(decimal)(end-start).TotalMinutes;
        var elapsed=(decimal)Math.Floor((now-start).TotalMinutes);
        var fraction=Math.Max(0.5m,1m-elapsed/duration);
        var price=(int)decimal.Ceiling(basePriceVnd*fraction);
        // The quoted amount is the revision: any changed payable price needs confirmation.
        return new(price,fraction*100m,price);
    }
}

public sealed record LateEntryPrice(int PriceVnd, decimal PricePercent, long Revision);
