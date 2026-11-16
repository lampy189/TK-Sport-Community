using be_da.Application.DTOs;

namespace be_da.Application.DTOs;
public record TicketReportInput(string? Category, string? Content, string? ImageBase64);
