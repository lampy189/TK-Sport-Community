using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using TKSC.Application.DTOs;
using TKSC.Application.Services;
using TKSC.Domain.Entities;
using Xunit;

namespace TKSC.Api.Tests;

public sealed class GoodsPaymentTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 2, 0, 0, 0, TimeSpan.Zero);
    private static (Order Order, Payment Payment, GoodsCheckoutSnapshot Snapshot, VerifiedBankEvent Event, GatewayStatus Remote) Example()
    {
        var line = new OrderItem { Id = 1, ProductId = 2, Product = new Product { Name = "Cầu" }, Quantity = 2, UnitPriceVnd = 25000, SubtotalVnd = 50000 };
        var order = new Order { Id = 3, Status = "PendingPayment", PaymentState = "Pending", TotalAmount = 50000, OrderItems = [line] };
        var payment = new Payment { Id = 4, GoodsOrderId = 3, Method = "payos", Status = "pending", AmountVnd = 50000, GatewayPaymentLinkId = "link" };
        var snapshot = new GoodsCheckoutSnapshot(Now.AddMinutes(15), 50000, [new(2, "Cầu", 2, 25000, 50000)]);
        var bank = new VerifiedBankEvent(4, 50000, "BANK1", "link", "VND", true, "{}");
        return (order, payment, snapshot, bank, new(4, 50000, "PAID", "link", [bank]));
    }

    [Fact]
    public void ExactVerifiedGoodsPaymentIsAccepted()
    {
        var x = Example();
        GoodsPaymentService.ValidateCart(x.Order);
        Assert.True(GoodsPaymentService.CanConfirmTransfer(x.Order, x.Payment, x.Snapshot, x.Event, x.Remote, Now));
    }

    [Theory]
    [InlineData("social")]
    [InlineData("wrongGoods")]
    [InlineData("changedLines")]
    [InlineData("changedTotal")]
    [InlineData("alreadyPaid")]
    [InlineData("invoiced")]
    [InlineData("cancelled")]
    [InlineData("underpaid")]
    [InlineData("overpaid")]
    [InlineData("wrongLink")]
    [InlineData("wrongOrder")]
    [InlineData("failed")]
    [InlineData("currency")]
    [InlineData("gatewayPending")]
    [InlineData("expired")]
    public void UnverifiedOrChangedPaymentNeverMarksOrderPaid(string scenario)
    {
        var x = Example();
        switch (scenario)
        {
            case "social": x.Payment.OrderId = 99; break;
            case "wrongGoods": x.Payment.GoodsOrderId = 99; break;
            case "changedLines": x.Order.OrderItems.First().Quantity = 3; break;
            case "changedTotal": x.Order.TotalAmount++; break;
            case "alreadyPaid": x.Payment.Status = "paid"; break;
            case "invoiced": x.Order.Status = "Invoiced"; break;
            case "cancelled": x.Order.Status = "Cancelled"; break;
            case "underpaid": x.Event = x.Event with { Amount = 49999 }; break;
            case "overpaid": x.Remote = x.Remote with { AmountPaid = 50001 }; break;
            case "wrongLink": x.Event = x.Event with { LinkId = "other" }; break;
            case "wrongOrder": x.Event = x.Event with { OrderCode = 99 }; break;
            case "failed": x.Event = x.Event with { Success = false }; break;
            case "currency": x.Event = x.Event with { Currency = "USD" }; break;
            case "gatewayPending": x.Remote = x.Remote with { Status = "PENDING" }; break;
            case "expired": x.Snapshot = x.Snapshot with { ExpiresAt = Now }; break;
        }
        Assert.False(GoodsPaymentService.CanConfirmTransfer(x.Order, x.Payment, x.Snapshot, x.Event, x.Remote, Now));
    }

    [Theory]
    [InlineData(0, 25000, 0, 0)]
    [InlineData(-1, 25000, -25000, -25000)]
    [InlineData(2, 25000, 1, 1)]
    [InlineData(2, 25000, 50000, 50001)]
    [InlineData(32767, int.MaxValue, 1, 1)]
    public void BadCartTotalsAndIntegerOverflowAreRejected(short quantity, int price, int subtotal, int total)
    {
        var order = new Order { TotalAmount = total, OrderItems = [new() { Quantity = quantity, UnitPriceVnd = price, SubtotalVnd = subtotal }] };
        Assert.Throws<PaymentException>(() => GoodsPaymentService.ValidateCart(order));
    }

    [Theory]
    [InlineData("Player")]
    [InlineData("Referee")]
    [InlineData("Manager")]
    [InlineData("Owner")]
    [InlineData("SystemAdmin")]
    public async Task OtherRolesCannotReadGoodsOrConfirmMoney(string role)
    {
        await using var app = new SkillAuthorizationApp();
        using var client = app.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });
        client.DefaultRequestHeaders.Add("Test-Role", role);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/v1/commerce/goods-orders/1")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsJsonAsync("/api/v1/commerce/goods-orders/1/confirm-cash", new { receivedAmount = 50000 })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsync("/api/v1/commerce/goods-orders/1/invoice", null)).StatusCode);
    }

    [Theory]
    [InlineData("payment-instructions")]
    [InlineData("confirm-transfer")]
    [InlineData("confirm-cash")]
    [InlineData("invoice")]
    public async Task StaffCannotMutateWithoutCsrf(string action)
    {
        await using var app = new SkillAuthorizationApp();
        using var client = app.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });
        client.DefaultRequestHeaders.Add("Test-Role", "Staff");
        client.DefaultRequestHeaders.Add("X-TKSC-Request", "1");
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync($"/api/v1/commerce/goods-orders/1/{action}", new { receivedAmount = 50000 })).StatusCode);
    }
}
