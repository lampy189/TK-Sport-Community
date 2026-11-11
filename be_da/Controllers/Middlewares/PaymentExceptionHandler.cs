using Microsoft.AspNetCore.Diagnostics;
using TKSC.Application.DTOs;

namespace TKSC.Controllers.Middlewares;

public sealed class PaymentExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception error, CancellationToken token)
    {
<<<<<<< HEAD
=======
        var sql = error as Microsoft.Data.SqlClient.SqlException
            ?? error.InnerException as Microsoft.Data.SqlClient.SqlException;
        if (sql?.Number is 33504 or >= 51031 and <= 51034)
        {
            context.Response.StatusCode = sql.Number == 33504 ? 403 : 409;
            await context.Response.WriteAsJsonAsync(new { message = sql.Number == 33504
                ? "Giao dịch nằm ngoài Sport Center được phân công."
                : "Chưa thể hạch toán: cần đối soát cơ sở, khoản thu gốc hoặc số tiền hoàn." }, token);
            return true;
        }
>>>>>>> 0d40ca35efaf07c5d4887d6f8febc66c57f5578c
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
