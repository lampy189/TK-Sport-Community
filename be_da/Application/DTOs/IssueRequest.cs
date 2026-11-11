using be_da.Application.DTOs;

namespace be_da.Application.DTOs;
public record IssueRequest(string? Reason, bool Cancel);
