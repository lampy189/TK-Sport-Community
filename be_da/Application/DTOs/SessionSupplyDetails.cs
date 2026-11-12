using System.Text.Json.Serialization;

namespace be_da.Application.DTOs;
public sealed record SessionSupplyDetails(SupplySession Session, IReadOnlyList<SupplyRecipient> Recipients, IReadOnlyList<SupplyAllocation> Allocations, IReadOnlyList<SupplyRequestView>? Requests = null);
