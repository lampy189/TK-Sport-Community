using System.ComponentModel.DataAnnotations;
using be_da.Domain.Entities;

namespace be_da.Application.DTOs;
public sealed record SportProfileResult(bool Success, string? ErrorCode, string? Message, SportProfileItem? Profile);
