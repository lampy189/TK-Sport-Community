using System.Text.Json.Serialization;

namespace be_da.Application.DTOs;
public sealed record SupplySession(long Id, DateOnly SessionDate, string TimeLabel, string SportName, string CourtName, string Status, int OutstandingItems, int ReconciledItems);
