using System.ComponentModel.DataAnnotations;

namespace be_da.Application.DTOs;
public sealed record ChatSource(string Id, string Title, string Reference, string Excerpt);
