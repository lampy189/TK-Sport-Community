using TKSC.Domain.Exceptions;
using be_da.Application.Interfaces.Services;
using TKSC.Application.Interfaces.Repositories;
using TKSC.Application.DTOs.Persistence;
using System.Data;
using System.Text.Json;
using TKSC.Domain.Assessments;
using TKSC.Domain.Entities;

namespace TKSC.Application.DTOs;
public sealed record ReviewInput(string Reason);
