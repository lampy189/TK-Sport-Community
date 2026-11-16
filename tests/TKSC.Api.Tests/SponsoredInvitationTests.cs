using System.Security.Claims;
using be_da.Application.DTOs;
using be_da.Application.Interfaces.Services;
using be_da.Presentation.Controllers;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using TKSC.Application.Mappings;
using TKSC.Domain.Entities;
using Xunit;

namespace TKSC.Api.Tests;
public sealed class SponsoredInvitationTests
{
    [Fact]
    public async Task AcceptanceUsesAuthenticatedInviteeAndReturnsIndividualHold()
    {
        var service = new Sessions();
        var controller = Controller(service, new Csrf(), 17);
        var result = Assert.IsType<OkObjectResult>(await controller.AcceptSponsoredInvitation(3, 9, default));
        Assert.Same(service.Result, result.Value);
        Assert.Equal((17L, 3L, 9L), service.Call);
    }

    [Fact]
    public async Task MissingIdentityCannotAccept()
    {
        var service = new Sessions();
        Assert.IsType<UnauthorizedResult>(await Controller(service, new Csrf(), null).AcceptSponsoredInvitation(3, 9, default));
        Assert.Null(service.Call);
    }

    [Fact]
    public async Task InvalidCsrfCannotCreateHold()
    {
        var service = new Sessions();
        Assert.IsType<BadRequestObjectResult>(await Controller(service, new Csrf { Reject = true }, 17).AcceptSponsoredInvitation(3, 9, default));
        Assert.Null(service.Call);
    }

    [Theory]
    [InlineData("INVITATION_NOT_FOUND", 404)]
    [InlineData("SESSION_NOT_FOUND", 404)]
    [InlineData("SESSION_FULL", 409)]
    [InlineData("TIME_CONFLICT", 409)]
    [InlineData("SKILL_INELIGIBLE", 409)]
    [InlineData("INVITATION_NOT_PENDING", 409)]
    public async Task RejectedAcceptanceDoesNotReturnSuccess(string code, int expected)
    {
        var service = new Sessions { Result = new(false, code, "Rejected") };
        var result = Assert.IsAssignableFrom<ObjectResult>(await Controller(service, new Csrf(), 17).AcceptSponsoredInvitation(3, 9, default));
        Assert.Equal(expected, result.StatusCode);
    }

    [Fact]
    public void OrderSummarySeparatesHolderFromPayerAndPreservesPriceAndDeadline()
    {
        var deadline = DateTimeOffset.UtcNow.AddMinutes(10);
        var order = new SocialBookingOrder { Id = 4, PayerId = 7, OrderCode = "PAY02", TotalAmount = 60000, Status = "Pending", ExpiresAt = deadline };
        order.Tickets.Add(new Ticket { Id = 5, OrderId = 4, PlayerId = 17, SessionId = 3, AmountVnd = 60000, Status = "Pending" });
        var ticket = Assert.Single(PaymentMappings.ToSummary(order).Tickets);
        Assert.Equal(17, ticket.PlayerId);
        Assert.Equal(7, ticket.PayerId);
        Assert.Equal(60000, ticket.AmountVnd);
        Assert.Equal(deadline, ticket.ExpiresAt);
        Assert.False(ticket.Valid);
    }

    private static SessionController Controller(Sessions service, Csrf csrf, long? userId)
    {
        var context = new DefaultHttpContext();
        if (userId.HasValue) context.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, userId.Value.ToString())], "test"));
        return new(service, csrf) { ControllerContext = new ControllerContext { HttpContext = context } };
    }
    private sealed class Csrf : IAntiforgery
    {
        public bool Reject { get; init; }
        public Task ValidateRequestAsync(HttpContext context) => Reject ? Task.FromException(new AntiforgeryValidationException("Invalid")) : Task.CompletedTask;
        public AntiforgeryTokenSet GetAndStoreTokens(HttpContext context) => throw new NotSupportedException();
        public AntiforgeryTokenSet GetTokens(HttpContext context) => throw new NotSupportedException();
        public Task<bool> IsRequestValidAsync(HttpContext context) => Task.FromResult(!Reject);
        public void SetCookieTokenAndHeader(HttpContext context) => throw new NotSupportedException();
    }
    private sealed class Sessions : ISessionService
    {
        public RegisterSessionResult Result { get; init; } = new(true, null, "Held", OrderId: 4, TicketId: 5, AmountVnd: 60000);
        public (long, long, long)? Call { get; private set; }
        public Task<RegisterSessionResult> AcceptSponsoredInvitationAsync(long userId, long sessionId, long invitationId, CancellationToken ct)
        { Call = (userId, sessionId, invitationId); return Task.FromResult(Result); }
        public bool IsDateRangeValid(DateOnly? from, DateOnly? to) => true;
        public Task<IReadOnlyList<TimeSlotSummary>> GetTimeSlotsAsync(CancellationToken ct) => throw new NotSupportedException();
        public Task<IReadOnlyList<VenueSummary>> GetVenuesAsync(CancellationToken ct) => throw new NotSupportedException();
        public Task<IReadOnlyList<SessionSummary>> GetSessionsAsync(SessionFilter filter, long? userId, CancellationToken ct) => throw new NotSupportedException();
        public Task<SessionDetails?> GetSessionByIdAsync(long id, long? userId, CancellationToken ct) => throw new NotSupportedException();
        public Task<RegisterSessionResult> RegisterSessionAsync(long userId, long id, CancellationToken ct) => throw new NotSupportedException();
        public Task<RegisterSessionResult> RegisterLateEntryAsync(long userId, long id, long revision, CancellationToken ct) => throw new NotSupportedException();
    }
}
