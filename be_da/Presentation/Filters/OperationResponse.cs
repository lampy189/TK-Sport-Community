using be_da.Application.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace be_da.Presentation.Filters;
public static class OperationResponse
{
    public static IActionResult ToResponse(this OperationResult result) => result.Outcome switch
    {
        OperationOutcome.Success => new OkObjectResult(result.Value),
        OperationOutcome.Invalid => new BadRequestObjectResult(result.Value),
        OperationOutcome.Missing => new NotFoundObjectResult(result.Value),
        OperationOutcome.Conflict => new ConflictObjectResult(result.Value),
        OperationOutcome.Denied => result.Value is null ? new ForbidResult() : new ObjectResult(result.Value)
        {
            StatusCode = 403
        },
        OperationOutcome.Created => new ObjectResult(result.Value)
        {
            StatusCode = 201
        },
        OperationOutcome.Completed => new NoContentResult(),
        _ => throw new InvalidOperationException("Unknown operation outcome.")};
}
