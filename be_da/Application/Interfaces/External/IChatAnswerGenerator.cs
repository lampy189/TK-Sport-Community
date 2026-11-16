using be_da.Application.DTOs;
using System.ComponentModel.DataAnnotations;

namespace be_da.Application.Interfaces.External;
public interface IChatAnswerGenerator
{
    Task<GeneratedAnswer?> GenerateAsync(string question, IReadOnlyList<KnowledgeArticle> sources, CancellationToken ct);
}
