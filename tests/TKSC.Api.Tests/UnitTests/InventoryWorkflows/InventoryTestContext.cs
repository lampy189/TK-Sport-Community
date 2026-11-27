using System.Security.Claims;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace TKSC.Api.Tests.InventoryWorkflows;

internal static class InventoryTestContext
{
    public static ControllerContext ForActor(string? actor = "22")
    {
        var claims = actor is null ? Array.Empty<Claim>() : [new Claim(ClaimTypes.NameIdentifier, actor)];
        return new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(claims, "test")) }
        };
    }

    public sealed class Antiforgery(bool valid) : IAntiforgery
    {
        public AntiforgeryTokenSet GetAndStoreTokens(HttpContext context) => new("request", "cookie", "form", "header");
        public AntiforgeryTokenSet GetTokens(HttpContext context) => new("request", "cookie", "form", "header");
        public Task<bool> IsRequestValidAsync(HttpContext context) => Task.FromResult(valid);
        public Task ValidateRequestAsync(HttpContext context) => valid ? Task.CompletedTask : Task.FromException(new AntiforgeryValidationException("Invalid test token."));
        public void SetCookieTokenAndHeader(HttpContext context) { }
    }
}
