using TKSC.Domain.Exceptions;
using TKSC.Application.Interfaces.Repositories;
using TKSC.Application.DTOs.Persistence;
using TKSC.Domain.Entities;

namespace TKSC.Application.DTOs;
public sealed record BuyerGoodsOrder(long Id, string OrderCode, int TotalAmount, string Status, string? PaymentState, string? HandoverState, IReadOnlyList<GoodsLine> Lines, DateTimeOffset? HoldExpiresAt = null, bool CanCancel = false);
