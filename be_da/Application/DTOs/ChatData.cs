using System.ComponentModel.DataAnnotations;

namespace be_da.Application.DTOs;
public sealed record ChatData(IReadOnlyList<KnowledgeArticle> Documents, bool HasMore, DateTimeOffset RetrievedAt, string Description);
