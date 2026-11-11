using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace be_da.Application.Chatbot;

// Routing only selects predefined read operations. No model-generated SQL or user-supplied identity.
public static class ChatLookupRouter
{
    public static ChatLookup? Resolve(ChatQuestion request, DateTimeOffset now)
    {
        var q = Normalize(request.Question);
        var scope = request.Scope ?? "auto";
        if (scope == "auto")
        {
            if (Has(q, "quy dinh", "chinh sach", "huong dan", "lam sao", "cach ", "dieu kien", "ky nang", "dupr", "khao sat")) scope = "policies";
            else if (Has(q, "hoan tien", "refund")) scope = "refunds";
            else if (Has(q, "thanh toan", "chuyen khoan", "don hang", "don social", "don #", "tra tien", "dong tien", "don so")) scope = "payments";
            else if (Has(q, "ve cua", "ve toi", "ve #", "ma ve", "da dat", "da dang ky", "ve so")) scope = "tickets";
            else if (Has(q, "lich", "buoi choi", "cho trong", "con cho", "con bao nhieu cho", "gia ve", "gia san", "het cho", "cau long", "pickleball", "badminton", "bao nhieu tien")) scope = "sessions";
            else return null;
        }
        if (scope == "policies")
            return request.OnDate is not null || request.Sport is not null || request.RecordId is not null ? null : new("policies");
        var today = DateOnly.FromDateTime(now.ToOffset(TimeSpan.FromHours(7)).DateTime);
        var date = request.OnDate;
        if (date is null)
        {
            var matches = Regex.Matches(q, @"\b(?:\d{4}-\d{1,2}-\d{1,2}|\d{1,2}/\d{1,2}/\d{4})\b");
            if (matches.Count > 1) return null;
            if (matches.Count == 1)
            {
                if (!DateOnly.TryParseExact(matches[0].Value, ["yyyy-M-d", "d/M/yyyy"], CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed)) return null;
                date = parsed;
            }
            else if (Has(q, "hom nay", "toi nay", "chieu nay", "sang nay")) date = today;
            else if (Has(q, "ngay mai", "hom sau")) date = today.AddDays(1);
            else if (Has(q, "hom qua")) date = today.AddDays(-1);
            else if (Regex.IsMatch(q, @"\b\d{1,2}/\d{1,2}\b") || Has(q, "tuan", "thang", "ngay kia")) return null;
        }
        var sport = request.Sport;
        if (sport is null)
        {
            var badminton = Has(q, "cau long", "badminton");
            var pickleball = Has(q, "pickleball");
            if (badminton != pickleball) sport = badminton ? "badminton" : "pickleball";
        }
        var recordId = request.RecordId;
        if (recordId is null)
        {
            var ids = Regex.Matches(q, @"(?:#|\bid\s*|\bso\s+)(\d+)\b");
            if (ids.Count > 1) return null;
            if (ids.Count == 1)
            {
                if (!long.TryParse(ids[0].Groups[1].Value, out var parsed) || parsed <= 0) return null;
                recordId = parsed;
            }
        }
        if (scope == "payments" && sport is not null) return null;
        return new(scope, date, sport, recordId);
    }

    private static bool Has(string text, params string[] words) => words.Any(text.Contains);
    private static string Normalize(string text) => new(text.ToLowerInvariant().Replace('đ', 'd').Normalize(NormalizationForm.FormD)
        .Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark).ToArray());
}
