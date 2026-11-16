using Microsoft.AspNetCore.Diagnostics;
using TKSC.Application.DTOs;

namespace TKSC.Controllers.Middlewares;

public sealed class PaymentExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception error, CancellationToken token)
    {
        var status = error switch {
            PaymentException e => e.StatusCode,
            GatewayException e => e.InvalidSignature ? 400 : 503,
            _ => 0
        };
        if (status == 0) return false;
        context.Response.StatusCode = status;
        await context.Response.WriteAsJsonAsync(new { message = error.Message }, token);
        return true;
    }
}
