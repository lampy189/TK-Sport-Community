using TKSC.Domain.Exceptions;
using TKSC.Application.Interfaces.Repositories;
using TKSC.Application.DTOs.Persistence;
using TKSC.Domain.Entities;

namespace TKSC.Application.DTOs;
public sealed record GoodsStockLine(long ProductId, string Name, int Quantity);
