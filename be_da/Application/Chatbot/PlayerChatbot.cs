using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace be_da.Application.Chatbot;

public sealed class PlayerChatbot(IPlayerChatData data, IChatAnswerGenerator generator, TimeProvider clock, ILogger<PlayerChatbot> logger,
    IOptions<ChatbotOptions>? options = null, IChatIntentInterpreter? interpreter = null)
{
    public async Task<ChatAnswer> AskAsync(ChatQuestion request, long userId, CancellationToken ct)
    {
        ChatLookup? lookup = null;
        ChatContext? context = null;
        string? notice = null;
        var question = request.Question;
        var automatic = request.Scope is null or "auto";
        if (automatic && interpreter is not null)
        {
            var intent = await interpreter.InterpretAsync(request, clock.GetUtcNow(), ct);
            if (intent is not null)
            {
                if (!ChatIntentValidation.TryValidate(intent, out lookup))
                    return new("Tôi chưa diễn giải được câu hỏi một cách chắc chắn. Bạn có thể nói rõ thêm điều muốn biết không?", "clarification", [], Context: request.Context);
                question = intent.Question;
                context = new(question, intent.Clarification);
                if (intent.Intent == "clarify") return new(intent.Clarification!, "clarification", [], Context: context);
                if (intent.Intent == "greeting") return new("Chào bạn! Tôi có thể giúp tìm buổi chơi, kiểm tra thông tin của bạn hoặc hướng dẫn liên hệ sân. Bạn đang cần hỗ trợ điều gì?", "conversation", [], Context: request.Context);
                if (intent.Intent == "unsupported") return new("Tôi hỗ trợ thông tin TKSC và dữ liệu của chính bạn. Tôi chưa thể thực hiện yêu cầu này, truy cập thông tin người khác hay thay đổi dữ liệu. Bạn muốn tìm hiểu gì về buổi chơi hoặc tài khoản của mình?", "no_match", []);
                if (intent.Intent == "contact") return BookingContactReply.Create(question, options?.Value ?? new(), true)! with { Context = context };
            }
            else
            {
                notice = "Chế độ hiểu câu hỏi bằng AI hiện chưa sẵn sàng; câu trả lời này chỉ dùng nhận diện cơ bản.";
                if (request.Context is not null)
                    return new("Tôi đang không kết nối được chức năng hiểu hội thoại nên chưa thể nối câu hỏi này với nội dung trước. Vui lòng thử lại sau.", "unavailable", [], Context: request.Context, Notice: notice);
            }
        }
        if (lookup is null && automatic)
        {
            var contact = BookingContactReply.Create(request.Question, options?.Value ?? new ChatbotOptions());
            if (contact is not null) return contact with { Notice = notice };
        }
        lookup ??= ChatLookupRouter.Resolve(request, clock.GetUtcNow());
        if (lookup is null)
            return new("Tôi chưa hiểu rõ bạn cần thông tin gì. Bạn có thể nói thêm về điều đang gặp phải không?", "no_match", [], Notice: notice);
        var result = await AnswerAsync(lookup, question, userId, ct);
        return result with { Context = context, Notice = notice };
    }

    private async Task<ChatAnswer> AnswerAsync(ChatLookup lookup, string question, long userId, CancellationToken ct)
    {
        if (userId <= 0 && lookup.Scope is not ("sessions" or "policies"))
            return new("Bạn cần đăng nhập bằng tài khoản Player để tôi kiểm tra vé, thanh toán hoặc hoàn tiền của bạn. Bạn vẫn có thể hỏi lịch, giá, chỗ trống, chính sách công khai hoặc liên hệ đặt sân mà không cần đăng nhập.", "login_required", [], Scope: lookup.Scope);
        ChatData snapshot;
        try { snapshot = await data.ReadAsync(lookup, userId, ct); }
        catch (Exception ex) when (ex is SqlException or InvalidOperationException)
        {
            logger.LogWarning("Chatbot SQL source unavailable ({ErrorType}).", ex.GetType().Name);
            return new("Chưa đọc được dữ liệu SQL Server. Vui lòng thử lại sau; tôi không dùng dữ liệu demo hoặc bản cũ để thay thế.", "unavailable", [], Scope: lookup.Scope);
        }
        if (lookup.Scope != "policies")
        {
            var details = snapshot.Documents.Count == 0
                ? "Không tìm thấy bản ghi trong phạm vi tra cứu và quyền của bạn."
                : string.Join("\n\n", snapshot.Documents.Select(d => d.Title + ":\n" + d.Content));
            if (snapshot.HasMore) details += "\n\nĐây là 20 kết quả đầu tiên, không phải toàn bộ dữ liệu. Bạn có thể viết thêm ngày, môn hoặc mã vé/đơn vào câu hỏi để tôi tìm cụ thể hơn.";
            // Exact financial/availability facts are rendered directly; never sent to or rewritten by an LLM.
            return new(snapshot.Description + "\n\n" + details, "live", snapshot.Documents.Select(Source).ToArray(), snapshot.RetrievedAt, lookup.Scope);
        }
        var documents = new PlayerKnowledgeIndex(snapshot.Documents).Search(question);
        if (documents.Count == 0)
            return new("Chưa tìm thấy nội dung phù hợp trong chính sách SQL đã được duyệt, còn hiệu lực và được công khai cho Player. Tôi không suy ra quy định từ dữ liệu demo.", "no_match", [], snapshot.RetrievedAt, lookup.Scope);

        var generated = await generator.GenerateAsync(question, documents, ct);
        if (generated?.SourceIds is { Length: 0 })
            return new("Các tài liệu tìm được chưa đủ để trả lời câu hỏi này. Vui lòng kiểm tra màn hình chức năng tương ứng hoặc liên hệ nhân viên TKSC.", "no_match", [], snapshot.RetrievedAt, lookup.Scope);
        if (generated is not null && !string.IsNullOrWhiteSpace(generated.Answer) && generated.Answer.Length <= 6000
            && generated.SourceIds is { Length: > 0 }
            && generated.SourceIds.All(id => documents.Any(d => d.Id == id)))
        {
            var cited = documents.Where(d => generated.SourceIds.Contains(d.Id)).ToArray();
            return new(generated.Answer, "generated", cited.Select(Source).ToArray(), snapshot.RetrievedAt, lookup.Scope);
        }
        return new("Tôi đang hiển thị trích dẫn hướng dẫn vì chưa tạo được câu trả lời AI. Bạn có thể tham khảo:\n\n"
            + string.Join("\n\n", documents.Select(d => d.Title + ":\n" + d.Content)),
            "reference", documents.Select(Source).ToArray(), snapshot.RetrievedAt, lookup.Scope);
    }

    private static ChatSource Source(KnowledgeArticle d) => new(d.Id, d.Title, d.Reference, d.Content);
}
