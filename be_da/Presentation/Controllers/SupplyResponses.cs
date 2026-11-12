using be_da.Application.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace be_da.Presentation.Controllers;
// One mapping of supply error codes to HTTP status for Staff and Referee endpoints.
internal static class SupplyResponses
{
    public static IActionResult ToResponse(ControllerBase controller, SupplyResult result)
    {
        if (result.Success)
            return controller.Ok(result.Details);
        var body = new { code = result.ErrorCode, message = result.Message };
        return result.ErrorCode switch
        {
            "SESSION_NOT_FOUND" or "PRODUCT_NOT_FOUND" or "ALLOCATION_NOT_FOUND" or "REQUEST_NOT_FOUND" or "NOT_ASSIGNED" => controller.NotFound(body),
            "VALIDATION_FAILED" or "UNIT_NOT_FOUND" or "QUANTITY_TOO_LARGE" => controller.BadRequest(body),
            _ => controller.Conflict(body)
        };
    }
}
