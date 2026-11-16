using TKSC.Domain.Exceptions;
using be_da.Application.Interfaces.Services;
using TKSC.Application.Interfaces.Repositories;
using TKSC.Application.DTOs.Persistence;
using System.Data;
using System.Text.Json;
using TKSC.Application.DTOs;
using TKSC.Application.Interfaces.Services;
using TKSC.Application.Interfaces.External;
using TKSC.Domain.Entities;

namespace TKSC.Application.DTOs;
public sealed record GoodsOrderView(long Id, string OrderCode, string? CustomerName, int TotalAmount, string Status, string? PaymentState, string? HandoverState, IReadOnlyList<GoodsLine> Lines, string? PaymentMethod, string? ReferenceCode, string? CheckoutUrl, DateTimeOffset? ExpiresAt, GoodsInvoice? Invoice, string? ReservationState = null, DateTimeOffset? HoldExpiresAt = null, bool CanCancel = false, DateTimeOffset? DeliveredAt = null, DateTimeOffset? CancelledAt = null);
