using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using be_da.Application.Abstractions;
using be_da.Application.Stock;
using be_da.Domain.Entities;
using Xunit;

namespace TKSC.Api.Tests;

public sealed class StockTests
{
    private static readonly Account Staff = new(20, "staff@example.test", "Staff One", "Staff", "Active", "sub-20", false);
    private static readonly Account Player = new(30, "player@example.test", "Player One", "Player", "Active", "sub-30", false);

    [Fact]
    public async Task Products_RequireStaffOrManager()
    {
        await using var app = new StockApp();
        using var anonymous = app.Client();
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync("/api/stock/products")).StatusCode);

        using var player = app.Client();
        await DevLogin(player, Player.Email);
        Assert.Equal(HttpStatusCode.Forbidden, (await player.GetAsync("/api/stock/products")).StatusCode);
    }

    [Fact]
    public async Task CreateProduct_PostsOpeningBalanceForStaff()
    {
        await using var app = new StockApp();
        using var client = app.Client();
        await DevLogin(client, Staff.Email);

        var response = await Send(client, HttpMethod.Post, "/api/stock/products",
            new { sku = " cau-as30 ", name = "Cầu lông AS30", category = "Equipment", unitPriceVnd = 25000, reorderLevel = 5, initialQuantity = 12 });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var product = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("CAU-AS30", product.GetProperty("sku").GetString());
        Assert.Equal(12, product.GetProperty("quantityOnHand").GetInt32());
        Assert.False(product.GetProperty("isLowStock").GetBoolean());
        Assert.Equal(Staff.Id, app.StockStore.LastOperator);
    }

    [Theory]
    [InlineData("A", "Tên", "Equipment", 1000)]
    [InlineData("SKU-1", " ", "Equipment", 1000)]
    [InlineData("SKU-1", "Tên", "Unknown", 1000)]
    [InlineData("SKU-1", "Tên", "Equipment", -1)]
    public async Task CreateProduct_RejectsInvalidFields(string sku, string name, string category, int price)
    {
        await using var app = new StockApp();
        using var client = app.Client();
        await DevLogin(client, Staff.Email);

        var response = await Send(client, HttpMethod.Post, "/api/stock/products",
            new { sku, name, category, unitPriceVnd = price, reorderLevel = 0, initialQuantity = 0 });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Empty(app.StockStore.Products);
    }

    [Fact]
    public async Task Movement_RejectsIssueAboveOnHand_AndPostsReceipt()
    {
        await using var app = new StockApp();
        app.StockStore.Products[1] = new StockProduct(1, "WATER", "Nước suối", "Drink", 10000, true, 3, 5);
        using var client = app.Client();
        await DevLogin(client, Staff.Email);

        var issue = await Send(client, HttpMethod.Post, "/api/stock/products/1/movements", new { type = "Issue", quantity = 4 });
        Assert.Equal(HttpStatusCode.Conflict, issue.StatusCode);
        Assert.Equal(3, app.StockStore.Products[1].QuantityOnHand);

        var receipt = await Send(client, HttpMethod.Post, "/api/stock/products/1/movements", new { type = "Receipt", quantity = 20 });
        Assert.Equal(HttpStatusCode.OK, receipt.StatusCode);
        Assert.Equal(23, (await receipt.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("quantityOnHand").GetInt32());

        var zero = await Send(client, HttpMethod.Post, "/api/stock/products/1/movements", new { type = "Receipt", quantity = 0 });
        Assert.Equal(HttpStatusCode.BadRequest, zero.StatusCode);
    }

    [Fact]
    public async Task UnsafeRequests_RequireCsrf()
    {
        await using var app = new StockApp();
        app.StockStore.Products[1] = new StockProduct(1, "WATER", "Nước suối", "Drink", 10000, true, 3, 5);
        using var client = app.Client();
        await DevLogin(client, Staff.Email);

        var response = await client.PostAsJsonAsync("/api/stock/products/1/movements", new { type = "Receipt", quantity = 1 });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(3, app.StockStore.Products[1].QuantityOnHand);
    }

    private static async Task DevLogin(HttpClient client, string email)
    {
        var res = await Send(client, HttpMethod.Post, "/api/auth/dev-login", new { Email = email });
        res.EnsureSuccessStatusCode();
    }

    private static async Task<HttpResponseMessage> Send(HttpClient client, HttpMethod method, string path, object body)
    {
        var token = await client.GetFromJsonAsync<JsonElement>("/api/auth/csrf");
        using var request = new HttpRequestMessage(method, path);
        request.Headers.Add("X-CSRF-TOKEN", token.GetProperty("token").GetString());
        request.Content = JsonContent.Create(body);
        return await client.SendAsync(request);
    }

    private sealed class StockApp : WebApplicationFactory<Program>
    {
        public FakeSessionAccounts AccountStore { get; } = new();
        public FakeStockStore StockStore { get; } = new();

        public StockApp()
        {
            foreach (var account in new[] { Staff, Player })
            {
                AccountStore.Accounts[account.Email] = account;
                AccountStore.AccountsById[account.Id] = account;
            }
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Authentication:Google:ClientId"] = "test-client",
                ["Authentication:Google:ClientSecret"] = "test-secret"
            }));
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IAccountStore>();
                services.AddSingleton<IAccountStore>(AccountStore);
                services.RemoveAll<IStockStore>();
                services.AddSingleton<IStockStore>(StockStore);
                services.PostConfigure<GoogleOptions>(GoogleDefaults.AuthenticationScheme,
                    options => options.Backchannel = new HttpClient(new GoogleStub(() => true)));
            });
        }

        public HttpClient Client() => CreateClient(new WebApplicationFactoryClientOptions
        { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false, HandleCookies = true });
    }
}

internal sealed class FakeStockStore : IStockStore
{
    public Dictionary<long, StockProduct> Products { get; } = new();
    public long? LastOperator { get; private set; }

    public Task<IReadOnlyList<StockProduct>> GetProductsAsync(StockProductFilter filter, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<StockProduct>>(Products.Values.ToList());

    public Task<StockProduct?> GetProductAsync(long productId, CancellationToken cancellationToken) =>
        Task.FromResult(Products.GetValueOrDefault(productId));

    public Task<StockResult> CreateProductAsync(CreateProductRequest request, long staffUserId, CancellationToken cancellationToken)
    {
        var sku = StockRules.NormalizeSku(request.Sku)!;
        if (Products.Values.Any(p => p.Sku == sku)) return Task.FromResult(StockResult.Fail("SKU_EXISTS", "exists"));
        LastOperator = staffUserId;
        var product = new StockProduct(Products.Count + 1, sku, request.Name!.Trim(), request.Category,
            request.UnitPriceVnd, true, request.InitialQuantity, request.ReorderLevel);
        Products[product.Id] = product;
        return Task.FromResult(StockResult.Ok(product));
    }

    public Task<StockResult> UpdateProductAsync(long productId, UpdateProductRequest request, CancellationToken cancellationToken)
    {
        if (!Products.TryGetValue(productId, out var product)) return Task.FromResult(StockResult.Fail("PRODUCT_NOT_FOUND", "missing"));
        product = product with { Name = request.Name!.Trim(), Category = request.Category, UnitPriceVnd = request.UnitPriceVnd,
            ReorderLevel = request.ReorderLevel, IsActive = request.IsActive };
        Products[productId] = product;
        return Task.FromResult(StockResult.Ok(product));
    }

    public Task<StockResult> RecordMovementAsync(long productId, StockMovementRequest request, long staffUserId, CancellationToken cancellationToken)
    {
        if (!Products.TryGetValue(productId, out var product)) return Task.FromResult(StockResult.Fail("PRODUCT_NOT_FOUND", "missing"));
        var delta = request.Type == StockRules.Issue ? -request.Quantity : request.Quantity;
        if (product.QuantityOnHand + delta < 0) return Task.FromResult(StockResult.Fail("INSUFFICIENT_STOCK", "insufficient"));
        LastOperator = staffUserId;
        Products[productId] = product = product with { QuantityOnHand = product.QuantityOnHand + delta };
        return Task.FromResult(StockResult.Ok(product));
    }

    public Task<IReadOnlyList<StockMovementEntry>?> GetMovementsAsync(long productId, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<StockMovementEntry>?>(Products.ContainsKey(productId) ? [] : null);
}
