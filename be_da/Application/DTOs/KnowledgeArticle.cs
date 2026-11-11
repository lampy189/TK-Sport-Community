using System.ComponentModel.DataAnnotations;

namespace be_da.Application.DTOs;
public sealed record KnowledgeArticle(string Id, string Title, string Reference, string Keywords, string Content);
