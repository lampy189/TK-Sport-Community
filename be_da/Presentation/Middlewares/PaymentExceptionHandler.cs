using TKSC.Domain.Exceptions;
using be_da.Application.Interfaces.Services;
using Microsoft.AspNetCore.Diagnostics;
using TKSC.Application.DTOs;
using TKSC.Application.Interfaces.Repositories;

namespace TKSC.Presentation.Middlewares;
public sealed class PaymentExceptionHandler(IPersistenceFailureClassifier failures) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception error, CancellationToken token)
    {
        var failure = failures.Classify(error);
        if (failure is not null)
        {
            context.Response.StatusCode = failure == PersistenceFailure.OutsideAssignedCenter ? 403 : 409;
            await context.Response.WriteAsJsonAsync(new { message = failure == PersistenceFailure.OutsideAssignedCenter ? "Giao dịch nằm ngoài Sport Center được phân công." : "Chưa thể hạch toán: cần đối soát cơ sở, khoản thu gốc hoặc số tiền hoàn." }, token);
            return true;
        }

        var status = error switch
        {
            PaymentException e => e.StatusCode,
            GatewayException e => e.InvalidSignature ? 400 : 503,
            _ => 0
        };
        if (status == 0)
            return false;
        context.Response.StatusCode = status;
        await context.Response.WriteAsJsonAsync(new { message = error.Message }, token);
        return true;
    }
}
