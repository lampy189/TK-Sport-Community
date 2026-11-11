using System.ComponentModel.DataAnnotations;

namespace be_da.Application.Chatbot;

public sealed record ChatQuestion(
    [Required, StringLength(1200, MinimumLength = 2)] string Question,
    [RegularExpression("^(auto|sessions|tickets|payments|refunds|policies)$")] string Scope = "auto",
    DateOnly? OnDate = null,
    [RegularExpression("^(badminton|pickleball)$")] string? Sport = null,
    [Range(1, long.MaxValue)] long? RecordId = null,
    ChatContext? Context = null);
public sealed record ChatContext([Required, StringLength(1200)] string Question, [StringLength(400)] string? Clarification = null);
public sealed record KnowledgeArticle(string Id, string Title, string Reference, string Keywords, string Content);
public sealed record ChatSource(string Id, string Title, string Reference, string Excerpt);
public sealed record ChatAnswer(string Answer, string Mode, IReadOnlyList<ChatSource> Sources,
    DateTimeOffset? RetrievedAt = null, string? Scope = null, BookingContact? BookingContact = null,
    ChatContext? Context = null, string? Notice = null);
public sealed record BookingLink(string Label, string Url);
public sealed record BookingContact(string Draft, IReadOnlyList<BookingLink> Links);
public sealed record GeneratedAnswer(string Answer, string[] SourceIds);

public interface IChatAnswerGenerator
{
    Task<GeneratedAnswer?> GenerateAsync(string question, IReadOnlyList<KnowledgeArticle> sources, CancellationToken ct);
}

public sealed class ChatbotOptions
{
    public string ApiKey { get; set; } = "";
    public string Model { get; set; } = "gpt-5-mini";
    // Explicit publication allowlist: approval alone does not make an internal policy public.
    public string[] PublicPolicyCodes { get; set; } = [];
    public string MessengerUrl { get; set; } = "";
    public string ZaloUrl { get; set; } = "";
}

public sealed record ChatLookup(string Scope, DateOnly? OnDate = null, string? Sport = null, long? RecordId = null,
    TimeOnly? StartFrom = null, TimeOnly? StartBefore = null);
public sealed record ChatIntent(string Intent, string? Date, string? Sport, long? RecordId,
    string Question, string? Clarification, string? StartFrom = null, string? StartBefore = null);
public interface IChatIntentInterpreter
{
    Task<ChatIntent?> InterpretAsync(ChatQuestion request, DateTimeOffset now, CancellationToken ct);
}
public sealed record ChatData(IReadOnlyList<KnowledgeArticle> Documents, bool HasMore, DateTimeOffset RetrievedAt, string Description);
public interface IPlayerChatData
{
    Task<ChatData> ReadAsync(ChatLookup lookup, long userId, CancellationToken ct);
}
