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
public sealed record GoodsInvoice(string Code, DateTimeOffset IssuedAt, long IssuedBy, long OrderId, string OrderCode, string? CustomerName, int TotalAmount, string Method, string PaymentReference, IReadOnlyList<GoodsLine> Lines);
