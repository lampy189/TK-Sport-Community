using System.Security.Claims;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using TKSC.Application.Services;
using TKSC.Controllers;
using Xunit;

namespace TKSC.Api.Tests.UnitTests;

public sealed class TicketAftercareControllerTests
{
    private const long ActorId = 42;
    private const long TicketId = 17;
    private const long SessionId = 31;
    private const long MessageId = 8;
    [Fact]
    public async Task Incidents_RequestsOnlyIssueSessions()
    {
        var resultObject = new object();
        var service = new Mock<ITicketAftercareService>();
        service.Setup(x => x.SessionsAsync(true)).ReturnsAsync(resultObject);

        var result = await CreateController(service).Incidents();

        Assert.Same(resultObject, Assert.IsType<OkObjectResult>(result).Value);
        service.Verify(x => x.SessionsAsync(true), Times.Once);
    }

    [Fact]
    public async Task Sessions_ReturnsAllAftercareSessions()
    {
        var resultObject = new object();
        var service = new Mock<ITicketAftercareService>();
        service.Setup(x => x.SessionsAsync(false)).ReturnsAsync(resultObject);

        var result = await CreateController(service).Sessions();

        Assert.Same(resultObject, Assert.IsType<OkObjectResult>(result).Value);
        service.Verify(x => x.SessionsAsync(false), Times.Once);
    }

    [Fact]
    public async Task Issue_ForwardsReasonCancellationFlagAndActor()
    {
        var input = new TicketAftercareController.IssueRequest("Session issue", true);
        var resultObject = new object();
        var service = new Mock<ITicketAftercareService>();
        service.Setup(x => x.ReportAsync(SessionId, ActorId, input.Reason, input.Cancel)).ReturnsAsync(resultObject);

        var result = await CreateController(service).Issue(SessionId, input);

        Assert.Same(resultObject, Assert.IsType<OkObjectResult>(result).Value);
        service.Verify(x => x.ReportAsync(SessionId, ActorId, "Session issue", true), Times.Once);
    }

    [Fact]
    public async Task Alternatives_ForwardsTicketAndActor()
    {
        var resultObject = new object();
        var service = new Mock<ITicketAftercareService>();
        service.Setup(x => x.OptionsAsync(TicketId, ActorId)).ReturnsAsync(resultObject);

        var result = await CreateController(service).Alternatives(TicketId);

        Assert.Same(resultObject, Assert.IsType<OkObjectResult>(result).Value);
        service.Verify(x => x.OptionsAsync(TicketId, ActorId), Times.Once);
    }

    [Fact]
    public async Task Change_ForwardsTargetSessionAndActor()
    {
        var input = new TicketAftercareController.ChangeRequest(SessionId);
        var resultObject = new object();
        var service = new Mock<ITicketAftercareService>();
        service.Setup(x => x.RequestChangeAsync(TicketId, SessionId, ActorId)).ReturnsAsync(resultObject);

        var result = await CreateController(service).Change(TicketId, input);

        Assert.Same(resultObject, Assert.IsType<OkObjectResult>(result).Value);
        service.Verify(x => x.RequestChangeAsync(TicketId, SessionId, ActorId), Times.Once);
    }

    [Fact]
    public async Task CancelChange_ForwardsChangeAndActor()
    {
        var resultObject = new object();
        var service = new Mock<ITicketAftercareService>();
        service.Setup(x => x.CancelChangeAsync(TicketId, ActorId)).ReturnsAsync(resultObject);

        var result = await CreateController(service).CancelChange(TicketId);

        Assert.Same(resultObject, Assert.IsType<OkObjectResult>(result).Value);
        service.Verify(x => x.CancelChangeAsync(TicketId, ActorId), Times.Once);
    }

    [Fact]
    public async Task Refund_ForwardsTicketAndActor()
    {
        var resultObject = new object();
        var service = new Mock<ITicketAftercareService>();
        service.Setup(x => x.RequestRefundAsync(TicketId, ActorId)).ReturnsAsync(resultObject);

        var result = await CreateController(service).Refund(TicketId);

        Assert.Same(resultObject, Assert.IsType<OkObjectResult>(result).Value);
        service.Verify(x => x.RequestRefundAsync(TicketId, ActorId), Times.Once);
    }

    [Fact]
    public async Task Changes_ReturnsPendingChangeRequests()
    {
        var resultObject = new object();
        var service = new Mock<ITicketAftercareService>();
        service.Setup(x => x.ChangesAsync()).ReturnsAsync(resultObject);

        var result = await CreateController(service).Changes();

        Assert.Same(resultObject, Assert.IsType<OkObjectResult>(result).Value);
        service.Verify(x => x.ChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task CompleteChange_ForwardsSettlementDataAndActor()
    {
        var input = new TicketAftercareController.Settlement("receipt.jpg", "BANK-123");
        var resultObject = new object();
        var service = new Mock<ITicketAftercareService>();
        service.Setup(x => x.CompleteChangeAsync(TicketId, ActorId, input.Evidence, input.BankReference))
            .ReturnsAsync(resultObject);

        var result = await CreateController(service).CompleteChange(TicketId, input);

        Assert.Same(resultObject, Assert.IsType<OkObjectResult>(result).Value);
        service.Verify(x => x.CompleteChangeAsync(TicketId, ActorId, "receipt.jpg", "BANK-123"), Times.Once);
    }

    [Fact]
    public async Task Refunds_ReturnsRefundRequests()
    {
        var resultObject = new object();
        var service = new Mock<ITicketAftercareService>();
        service.Setup(x => x.RefundsAsync()).ReturnsAsync(resultObject);

        var result = await CreateController(service).Refunds();

        Assert.Same(resultObject, Assert.IsType<OkObjectResult>(result).Value);
        service.Verify(x => x.RefundsAsync(), Times.Once);
    }

    [Fact]
    public async Task CompleteRefund_ForwardsSettlementDataAndActor()
    {
        var input = new TicketAftercareController.Settlement("refund-proof.jpg", "BANK-456");
        var resultObject = new object();
        var service = new Mock<ITicketAftercareService>();
        service.Setup(x => x.CompleteRefundAsync(TicketId, ActorId, input.Evidence, input.BankReference))
            .ReturnsAsync(resultObject);

        var result = await CreateController(service).CompleteRefund(TicketId, input);

        Assert.Same(resultObject, Assert.IsType<OkObjectResult>(result).Value);
        service.Verify(x => x.CompleteRefundAsync(TicketId, ActorId, "refund-proof.jpg", "BANK-456"), Times.Once);
    }

    [Fact]
    public async Task Reviews_ReturnsPaymentsRequiringReview()
    {
        var resultObject = new object();
        var service = new Mock<ITicketAftercareService>();
        service.Setup(x => x.ReviewsAsync()).ReturnsAsync(resultObject);

        var result = await CreateController(service).Reviews();

        Assert.Same(resultObject, Assert.IsType<OkObjectResult>(result).Value);
        service.Verify(x => x.ReviewsAsync(), Times.Once);
    }

    [Fact]
    public async Task Approve_ForwardsBankTransactionEvidenceAndActor()
    {
        var input = new TicketAftercareController.FlaggedPaymentApprovalRequest(72, "bank-match.pdf");
        var resultObject = new object();
        var service = new Mock<ITicketAftercareService>();
        service.Setup(x => x.ApprovePaymentAsync(TicketId, input.BankTransactionId, ActorId, input.Evidence))
            .ReturnsAsync(resultObject);

        var result = await CreateController(service).Approve(TicketId, input);

        Assert.Same(resultObject, Assert.IsType<OkObjectResult>(result).Value);
        service.Verify(x => x.ApprovePaymentAsync(TicketId, 72, ActorId, "bank-match.pdf"), Times.Once);
    }

    [Fact]
    public async Task Inbox_ReturnsSupportInbox()
    {
        var resultObject = new object();
        var support = new Mock<ITicketSupportService>();
        support.Setup(x => x.Inbox()).ReturnsAsync(resultObject);

        var result = await CreateController(support: support).Inbox();

        Assert.Same(resultObject, Assert.IsType<OkObjectResult>(result).Value);
        support.Verify(x => x.Inbox(), Times.Once);
    }

    [Theory]
    [InlineData("Player", false)]
    [InlineData("Staff", true)]
    [InlineData("Manager", true)]
    public async Task Messages_ForwardsActorAndStaffAccess(string role, bool expectedStaffAccess)
    {
        var resultObject = new object();
        var support = new Mock<ITicketSupportService>();
        support.Setup(x => x.List(TicketId, ActorId, expectedStaffAccess)).ReturnsAsync(resultObject);
        var controller = CreateController(support: support, role: role);

        var result = await controller.Messages(TicketId);

        Assert.Same(resultObject, Assert.IsType<OkObjectResult>(result).Value);
        Assert.Equal("no-store", controller.Response.Headers.CacheControl);
        support.Verify(x => x.List(TicketId, ActorId, expectedStaffAccess), Times.Once);
    }

    [Fact]
    public async Task SendMessage_ForwardsContentImageActorAndStaffAccess()
    {
        var input = new TicketAftercareController.TicketSupportInput("Need assistance.", "image-data");
        var resultObject = new object();
        var support = new Mock<ITicketSupportService>();
        support.Setup(x => x.Send(TicketId, ActorId, false, input.Content, input.ImageBase64))
            .ReturnsAsync(resultObject);

        var result = await CreateController(support: support).SendMessage(TicketId, input);

        Assert.Same(resultObject, Assert.IsType<OkObjectResult>(result).Value);
        support.Verify(x => x.Send(TicketId, ActorId, false, "Need assistance.", "image-data"), Times.Once);
    }

    [Fact]
    public async Task MessageImage_ReturnsImageAndSetsSecurityHeaders()
    {
        byte[] imageBytes = [1, 2, 3];
        var support = new Mock<ITicketSupportService>();
        support.Setup(x => x.Image(TicketId, MessageId, ActorId, false))
            .ReturnsAsync((imageBytes, "image/png"));
        var controller = CreateController(support: support);

        var result = await controller.MessageImage(TicketId, MessageId);

        var file = Assert.IsType<FileContentResult>(result);
        Assert.Same(imageBytes, file.FileContents);
        Assert.Equal("image/png", file.ContentType);
        Assert.Equal("no-store", controller.Response.Headers.CacheControl);
        Assert.Equal("nosniff", controller.Response.Headers["X-Content-Type-Options"]);
        support.Verify(x => x.Image(TicketId, MessageId, ActorId, false), Times.Once);
    }

    [Fact]
    public async Task Report_ForwardsReportDetailsAndActor()
    {
        var input = new TicketAftercareController.TicketReportInput("payment", "Payment issue", "evidence-image");
        var resultObject = new object();
        var support = new Mock<ITicketSupportService>();
        support.Setup(x => x.Report(TicketId, ActorId, input.Category, input.Content, input.ImageBase64))
            .ReturnsAsync(resultObject);

        var result = await CreateController(support: support, role: "Player").Report(TicketId, input);

        Assert.Same(resultObject, Assert.IsType<OkObjectResult>(result).Value);
        support.Verify(x => x.Report(TicketId, ActorId, "payment", "Payment issue", "evidence-image"), Times.Once);
    }

    [Fact]
    public async Task Mutation_WhenAntiforgeryValidationFails_ReturnsBadRequestWithoutCallingService()
    {
        var service = new Mock<ITicketAftercareService>();
        var csrf = InvalidAntiforgery();

        var result = await CreateController(service, csrf)
            .Refund(TicketId);

        var response = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("invalid_csrf_token", GetProperty<string>(response.Value, "code"));
        Assert.Equal("Phiên bảo mật đã thay đổi. Vui lòng thử lại.", GetProperty<string>(response.Value, "message"));
        service.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Mutation_WhenActorClaimIsInvalid_PassesFallbackActorIdToService()
    {
        var resultObject = new object();
        var service = new Mock<ITicketAftercareService>();
        service.Setup(x => x.RequestRefundAsync(TicketId, -1)).ReturnsAsync(resultObject);

        var result = await CreateController(service, actorIdClaim: "invalid").Refund(TicketId);

        Assert.Same(resultObject, Assert.IsType<OkObjectResult>(result).Value);
        service.Verify(x => x.RequestRefundAsync(TicketId, -1), Times.Once);
    }

    [Fact]
    public async Task Mutation_WhenServiceThrows_PropagatesException()
    {
        var service = new Mock<ITicketAftercareService>();
        service.Setup(x => x.RequestRefundAsync(TicketId, ActorId))
            .ThrowsAsync(new InvalidOperationException("refund service unavailable"));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => CreateController(service).Refund(TicketId));

        Assert.Equal("refund service unavailable", exception.Message);
    }

    private static TicketAftercareController CreateController(
        Mock<ITicketAftercareService>? service = null,
        Mock<IAntiforgery>? csrf = null,
        Mock<ITicketSupportService>? support = null,
        string role = "Player",
        string? actorIdClaim = "42")
    {
        var context = new DefaultHttpContext();
        if (actorIdClaim is not null)
            context.User = new ClaimsPrincipal(new ClaimsIdentity(
                [
                    new Claim(ClaimTypes.NameIdentifier, actorIdClaim),
                    new Claim(ClaimTypes.Role, role)
                ],
                "UnitTest"));

        return new TicketAftercareController(
            (service ?? new Mock<ITicketAftercareService>()).Object,
            (support ?? new Mock<ITicketSupportService>()).Object,
            (csrf ?? ValidAntiforgery()).Object)
        {
            ControllerContext = new ControllerContext { HttpContext = context }
        };
    }

    private static Mock<IAntiforgery> ValidAntiforgery()
    {
        var csrf = new Mock<IAntiforgery>();
        csrf.Setup(x => x.ValidateRequestAsync(It.IsAny<HttpContext>())).Returns(Task.CompletedTask);
        return csrf;
    }

    private static Mock<IAntiforgery> InvalidAntiforgery()
    {
        var csrf = new Mock<IAntiforgery>();
        csrf.Setup(x => x.ValidateRequestAsync(It.IsAny<HttpContext>()))
            .ThrowsAsync(new AntiforgeryValidationException("Invalid CSRF token"));
        return csrf;
    }

    private static T? GetProperty<T>(object? value, string name) =>
        (T?)value?.GetType().GetProperty(name)?.GetValue(value);

}
