using TKSC.Application.DTOs;
using TKSC.Domain.Exceptions;
using TKSC.Application.Interfaces.Repositories;
using TKSC.Application.DTOs.Persistence;
using System.Data;
using System.Text.Json;
using TKSC.Domain.Assessments;
using TKSC.Domain.Entities;
using TKSC.Application.Services;

namespace be_da.Application.Interfaces.Services;
public interface ISkillReviewService
{
    Task<object> RequestException(long sessionId, long actor, ReviewInput input, CancellationToken ct);
    Task<object> Exceptions(long actor, string role, long? sessionId, CancellationToken ct);
    Task<object> DecideException(long id, long actor, string role, ExceptionDecision input, CancellationToken ct);
    Task<object> RequestAppeal(long assessmentId, long actor, ReviewInput input, CancellationToken ct);
    Task<object> Appeals(long actor, string role, CancellationToken ct);
    Task<object> ReviewAppeal(long id, long actor, ReviewInput input, CancellationToken ct);
    Task<object> DecideAppeal(long id, long actor, AppealDecision input, CancellationToken ct);
}
