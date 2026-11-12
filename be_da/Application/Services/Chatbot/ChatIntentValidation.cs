using be_da.Application.DTOs;
using be_da.Application.Interfaces.External;
using be_da.Application.Interfaces.Repositories;
using System.Globalization;

namespace be_da.Application.Services.Chatbot;
public static class ChatIntentValidation
{
    public static bool TryValidate(ChatIntent intent, out ChatLookup? lookup)
    {
        lookup = null;
        if (string.IsNullOrWhiteSpace(intent.Question) || intent.Question.Length > 1200 || intent.Clarification?.Length > 400 || intent.RecordId is <= 0 || intent.Sport is not (null or "badminton" or "pickleball"))
            return false;
        DateOnly? date = null;
        TimeOnly? from = null, before = null;
        if (intent.Date is not null)
        {
            if (!DateOnly.TryParseExact(intent.Date, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d))
                return false;
            date = d;
        }

        if (intent.StartFrom is not null)
        {
            if (!TimeOnly.TryParseExact(intent.StartFrom, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var t))
                return false;
            from = t;
        }

        if (intent.StartBefore is not null)
        {
            if (!TimeOnly.TryParseExact(intent.StartBefore, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var t))
                return false;
            before = t;
        }

        if (from.HasValue && before.HasValue && from >= before)
            return false;
        if (intent.Intent != "sessions" && (from.HasValue || before.HasValue))
            return false;
        if (intent.Intent == "payments" && intent.Sport is not null)
            return false;
        if (intent.Intent is "policies" or "contact" or "greeting" or "clarify" or "unsupported")
        {
            if (date.HasValue || intent.Sport is not null || intent.RecordId.HasValue)
                return false;
            if (intent.Intent == "clarify" && string.IsNullOrWhiteSpace(intent.Clarification))
                return false;
        }
        else if (intent.Intent is not ("sessions" or "tickets" or "payments" or "refunds"))
            return false;
        lookup = new(intent.Intent, date, intent.Sport, intent.RecordId, from, before);
        return true;
    }
}
