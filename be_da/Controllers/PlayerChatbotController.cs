using be_da.Application.Chatbot;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Claims;

namespace be_da.Controllers;

[ApiController]
[Route("api/player/chatbot")]
[Authorize(Roles = "Player")]
public sealed class PlayerChatbotController(PlayerChatbot chatbot, IAntiforgery antiforgery) : ControllerBase
{
    [HttpPost("ask")]
    [EnableRateLimiting("player-chatbot")]
    [RequestSizeLimit(8192)]
    [ProducesResponseType(typeof(ChatAnswer), StatusCodes.Status200OK)]
    public async Task<IActionResult> Ask(ChatQuestion request, CancellationToken ct)
    {
        try { await antiforgery.ValidateRequestAsync(HttpContext); }
        catch (AntiforgeryValidationException) { return BadRequest(new { code = "invalid_csrf_token" }); }
        if (string.IsNullOrWhiteSpace(request.Question) || request.Question.Trim().Length < 2)
            return BadRequest(new { code = "invalid_question", message = "Nhập câu hỏi từ 2 đến 1.200 ký tự." });
        if (!long.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId) || userId <= 0) return Unauthorized();
        Response.Headers.CacheControl = "no-store";
        return Ok(await chatbot.AskAsync(request with { Question = request.Question.Trim() }, userId, ct));
    }
}
