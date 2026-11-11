using be_da.Application.Interfaces.Services;
using be_da.Presentation.Controllers;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace be_da.Presentation.Swagger;
public sealed class AuthDocumentationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        if (context.MethodInfo.DeclaringType != typeof(AuthController))
            return;
        // Keep status documentation compact; Execute still displays the real response body.
        foreach (var response in operation.Responses.Values)
            response.Content.Clear();
    }
}
