using be_da.Application.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace TKSC.Presentation.Middlewares;
// Runs before [ApiController]'s automatic 400 so malformed bodies (missing field, wrong type,
// empty body) answer in the same { code, message } shape as the business validation.
[AttributeUsage(AttributeTargets.Class)]
public sealed class ValidatedRequestAttribute : ActionFilterAttribute
{
    public ValidatedRequestAttribute() => Order = -3000;
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        if (context.ModelState.IsValid)
            return;
        context.Result = new BadRequestObjectResult(new { code = "INVALID_REQUEST", message = "Dữ liệu gửi lên thiếu trường bắt buộc hoặc sai định dạng." });
    }
}
