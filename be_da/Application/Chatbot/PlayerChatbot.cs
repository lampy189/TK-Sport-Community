using Microsoft.Data.SqlClient;

namespace be_da.Application.Chatbot;

public sealed class PlayerChatbot(IPlayerChatData data, IChatAnswerGenerator generator, TimeProvider clock, ILogger<PlayerChatbot> logger)
{
    public async Task<ChatAnswer> AskAsync(ChatQuestion request, long userId, CancellationToken ct)
    {
        var lookup = ChatLookupRouter.Resolve(request, clock.GetUtcNow());
        if (lookup is null)
            return new("Bạn muốn biết thông tin gì về buổi chơi hoặc tài khoản của mình? Hãy viết rõ câu hỏi, kèm ngày hoặc mã vé/đơn nếu có, để tôi tìm đúng dữ liệu. Tôi hiện tra cứu từng ngày cụ thể, chưa tổng hợp theo khoảng ngày.", "no_match", []);
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
        var documents = new PlayerKnowledgeIndex(snapshot.Documents).Search(request.Question);
        if (documents.Count == 0)
            return new("Chưa tìm thấy nội dung phù hợp trong chính sách SQL đã được duyệt, còn hiệu lực và được công khai cho Player. Tôi không suy ra quy định từ dữ liệu demo.", "no_match", [], snapshot.RetrievedAt, lookup.Scope);

        var generated = await generator.GenerateAsync(request.Question, documents, ct);
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
