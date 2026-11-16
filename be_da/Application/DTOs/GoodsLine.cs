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
public sealed record GoodsLine(long ProductId, string Name, short Quantity, int UnitPriceVnd, int SubtotalVnd);
