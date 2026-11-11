using be_da.Application.DTOs;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using be_da.Application.Interfaces.Repositories;
using be_da.Application.Validators;
using be_da.Domain.Entities;
using Xunit;

namespace TKSC.Api.Tests.Stock;

// Request validation of the stock module's APIs, through the real pipeline with in-memory stores.
public sealed class StockApiValidationTests
{
    private static readonly Account Staff = new(20, "staff@example.test", "Staff One", "Staff", "Active", "sub-20", false, VenueId: 1);
    private static readonly Account Referee = new(40, "referee@example.test", "Referee One", "Referee", "Active", "sub-40", false, VenueId: 1);
    private static readonly Account Player = new(30, "player@example.test", "Player One", "Player", "Active", "sub-30", false);
    private const string StaffLoggedIn = "Staff signed in with a valid CSRF token";
    private static readonly object ValidProduct = new { sku = "CAU-1", name = "Shuttle", category = "Equipment", unitPriceVnd = 1000, reorderLevel = 1, initialQuantity = 0 };

    private static async Task<(ApiApp App, HttpClient Client)> SignedIn(Account account)
    {
        var app = new ApiApp();
        var client = app.Client();
        var res = await Send(client, HttpMethod.Post, "/api/auth/dev-login", new { email = account.Email });
        res.EnsureSuccessStatusCode();
        return (app, client);
    }

    private static async Task<HttpResponseMessage> Send(HttpClient client, HttpMethod method, string path, object? body = null, string? raw = null)
    {
        var token = await client.GetFromJsonAsync<JsonElement>("/api/auth/csrf");
        using var request = new HttpRequestMessage(method, path);
        request.Headers.Add("X-CSRF-TOKEN", token.GetProperty("token").GetString());
        request.Headers.Add("X-TKSC-Request", "1");
        if (raw is not null) request.Content = new StringContent(raw, Encoding.UTF8, "application/json");
        else if (body is not null) request.Content = JsonContent.Create(body);
        return await client.SendAsync(request);
    }

    private static async Task AssertInvalid(HttpResponseMessage response, string code)
    {
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(code, json.GetProperty("code").GetString());
        Assert.False(string.IsNullOrEmpty(json.GetProperty("message").GetString()));
    }

    [UnitCase("UTC001", "POST /api/stock/products", "Normal", "all fields valid", "Return: 201 Created", StaffLoggedIn)]
    public async Task CreateProduct_Valid()
    {
        var (app, client) = await SignedIn(Staff);
        await using var _ = app;
        var response = await Send(client, HttpMethod.Post, "/api/stock/products", ValidProduct);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [UnitCase("UTC002", "POST /api/stock/products", "Abnormal", "unitPriceVnd missing", "Return: 400 INVALID_REQUEST; store not called", StaffLoggedIn)]
    public async Task CreateProduct_MissingPrice()
    {
        var (app, client) = await SignedIn(Staff);
        await using var _ = app;
        await AssertInvalid(await Send(client, HttpMethod.Post, "/api/stock/products", new { sku = "CAU-1", name = "Shuttle", reorderLevel = 1, initialQuantity = 0 }), "INVALID_REQUEST");
        Assert.Empty(app.Stock.Products);
    }

    [UnitCase("UTC003", "POST /api/stock/products", "Abnormal", "unitPriceVnd=\"abc\" (wrong type)", "Return: 400 INVALID_REQUEST", StaffLoggedIn)]
    public async Task CreateProduct_WrongType()
    {
        var (app, client) = await SignedIn(Staff);
        await using var _ = app;
        await AssertInvalid(await Send(client, HttpMethod.Post, "/api/stock/products", new { sku = "CAU-1", name = "Shuttle", unitPriceVnd = "abc", reorderLevel = 1, initialQuantity = 0 }), "INVALID_REQUEST");
    }

    [UnitCase("UTC004", "POST /api/stock/products", "Abnormal", "empty body", "Return: 400 INVALID_REQUEST", StaffLoggedIn)]
    public async Task CreateProduct_EmptyBody()
    {
        var (app, client) = await SignedIn(Staff);
        await using var _ = app;
        await AssertInvalid(await Send(client, HttpMethod.Post, "/api/stock/products", raw: ""), "INVALID_REQUEST");
    }

    [UnitCase("UTC005", "POST /api/stock/products", "Abnormal", "valid body, no CSRF header", "Return: 400 invalid_csrf_token", "Staff signed in")]
    public async Task CreateProduct_NoCsrf()
    {
        var (app, client) = await SignedIn(Staff);
        await using var _ = app;
        var response = await client.PostAsJsonAsync("/api/stock/products", ValidProduct);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("invalid_csrf_token", (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString());
        Assert.Empty(app.Stock.Products);
    }

    [UnitCase("UTC006", "POST /api/stock/products", "Abnormal", "valid body", "Return: 403 Forbidden", "Player signed in")]
    public async Task CreateProduct_PlayerForbidden()
    {
        var (app, client) = await SignedIn(Player);
        await using var _ = app;
        Assert.Equal(HttpStatusCode.Forbidden, (await Send(client, HttpMethod.Post, "/api/stock/products", ValidProduct)).StatusCode);
    }

    [UnitCase("UTC007", "PUT /api/stock/products/{id}", "Abnormal", "isActive missing", "Return: 400 INVALID_REQUEST (never read as false)", StaffLoggedIn)]
    public async Task UpdateProduct_MissingIsActive()
    {
        var (app, client) = await SignedIn(Staff);
        await using var _ = app;
        await AssertInvalid(await Send(client, HttpMethod.Put, "/api/stock/products/1", new { name = "Shuttle", unitPriceVnd = 1, reorderLevel = 1 }), "INVALID_REQUEST");
    }

    [UnitCase("UTC008", "PUT /api/stock/products/{id}", "Boundary", "id=0", "Return: 404 (route rejects ids below 1)", StaffLoggedIn)]
    public async Task UpdateProduct_ZeroId()
    {
        var (app, client) = await SignedIn(Staff);
        await using var _ = app;
        Assert.Equal(HttpStatusCode.NotFound, (await Send(client, HttpMethod.Put, "/api/stock/products/0", new { name = "Shuttle", unitPriceVnd = 1, reorderLevel = 1, isActive = true })).StatusCode);
    }

    [UnitCase("UTC009", "PUT /api/stock/products/{id}", "Abnormal", "units has two base units", "Return: 400 VALIDATION_FAILED", StaffLoggedIn)]
    public async Task UpdateProduct_BadUnits()
    {
        var (app, client) = await SignedIn(Staff);
        await using var _ = app;
        await AssertInvalid(await Send(client, HttpMethod.Put, "/api/stock/products/1", new
        {
            name = "Shuttle", unitPriceVnd = 1, reorderLevel = 1, isActive = true,
            units = new[] { new { unitName = "A", factorToBase = 1 }, new { unitName = "B", factorToBase = 1 } }
        }), "VALIDATION_FAILED");
    }

    [UnitCase("UTC010", "POST /api/stock/products/{id}/movements", "Abnormal", "quantity missing", "Return: 400 INVALID_REQUEST", StaffLoggedIn)]
    public async Task Movement_MissingQuantity()
    {
        var (app, client) = await SignedIn(Staff);
        await using var _ = app;
        await AssertInvalid(await Send(client, HttpMethod.Post, "/api/stock/products/1/movements", new { type = "Receipt" }), "INVALID_REQUEST");
    }

    [UnitCase("UTC011", "POST /api/stock/products/{id}/movements", "Abnormal", "quantity=1.5", "Return: 400 INVALID_REQUEST", StaffLoggedIn)]
    public async Task Movement_DecimalQuantity()
    {
        var (app, client) = await SignedIn(Staff);
        await using var _ = app;
        await AssertInvalid(await Send(client, HttpMethod.Post, "/api/stock/products/1/movements", raw: """{"type":"Receipt","quantity":1.5}"""), "INVALID_REQUEST");
    }

    [UnitCase("UTC012", "POST /api/stock/products/{id}/movements", "Boundary", "quantity=0", "Return: 400 VALIDATION_FAILED", StaffLoggedIn)]
    public async Task Movement_ZeroQuantity()
    {
        var (app, client) = await SignedIn(Staff);
        await using var _ = app;
        await AssertInvalid(await Send(client, HttpMethod.Post, "/api/stock/products/1/movements", new { type = "Receipt", quantity = 0 }), "VALIDATION_FAILED");
    }

    [UnitCase("UTC013", "GET /api/stock/lookup", "Normal", "code=9930001 (known barcode)", "Return: 200 with item and unit", StaffLoggedIn)]
    public async Task Lookup_Found()
    {
        var (app, client) = await SignedIn(Staff);
        await using var _ = app;
        app.Stock.Lookups["9930001"] = new StockCodeLookup(1, "WATER", "Water", true, 1000, 5, "Bottle", "Carton", 24, "9930001");
        var found = await client.GetFromJsonAsync<JsonElement>("/api/stock/lookup?code=9930001");
        Assert.Equal(24, found.GetProperty("factorToBase").GetInt32());
    }

    [UnitCase("UTC014", "GET /api/stock/lookup", "Abnormal", "code=<script>", "Return: 400 VALIDATION_FAILED", StaffLoggedIn)]
    public async Task Lookup_InvalidCode()
    {
        var (app, client) = await SignedIn(Staff);
        await using var _ = app;
        await AssertInvalid(await client.GetAsync("/api/stock/lookup?code=%3Cscript%3E"), "VALIDATION_FAILED");
    }

    [UnitCase("UTC015", "GET /api/stock/lookup", "Abnormal", "code=NOPE1234 (not in catalogue)", "Return: 404 CODE_NOT_FOUND", StaffLoggedIn)]
    public async Task Lookup_NotFound()
    {
        var (app, client) = await SignedIn(Staff);
        await using var _ = app;
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/api/stock/lookup?code=NOPE1234")).StatusCode);
    }

    [UnitCase("UTC016", "POST /api/stock/receipts/batch", "Normal", "ref=PN-01; one valid line", "Return: 200", StaffLoggedIn)]
    public async Task Batch_Valid()
    {
        var (app, client) = await SignedIn(Staff);
        await using var _ = app;
        var response = await Send(client, HttpMethod.Post, "/api/stock/receipts/batch", new { sourceRef = "PN-01", lines = new[] { new { code = "WATER", quantity = 1 } } });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(Staff.Id, app.Stock.LastOperator);
    }

    [UnitCase("UTC017", "POST /api/stock/receipts/batch", "Abnormal", "sourceRef missing", "Return: 400 VALIDATION_FAILED; store not called", StaffLoggedIn)]
    public async Task Batch_NoRef()
    {
        var (app, client) = await SignedIn(Staff);
        await using var _ = app;
        var response = await Send(client, HttpMethod.Post, "/api/stock/receipts/batch", new { lines = new[] { new { code = "WATER", quantity = 1 } } });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Null(app.Stock.LastOperator);
    }

    [UnitCase("UTC018", "POST /api/stock/receipts/batch", "Abnormal", "a line without quantity", "Return: 400 INVALID_REQUEST", StaffLoggedIn)]
    public async Task Batch_LineMissingQuantity()
    {
        var (app, client) = await SignedIn(Staff);
        await using var _ = app;
        await AssertInvalid(await Send(client, HttpMethod.Post, "/api/stock/receipts/batch", new { sourceRef = "PN-01", lines = new[] { new { code = "WATER" } } }), "INVALID_REQUEST");
    }

    [UnitCase("UTC019", "POST /api/stock/receipts/batch", "Abnormal", "source reference already used by another receipt", "Return: 409 SOURCE_REF_USED", StaffLoggedIn)]
    public async Task Batch_RefUsed()
    {
        var (app, client) = await SignedIn(Staff);
        await using var _ = app;
        app.Stock.NextBatchResult = BatchReceiptResult.Fail("SOURCE_REF_USED", "used");
        var response = await Send(client, HttpMethod.Post, "/api/stock/receipts/batch", new { sourceRef = "PN-01", lines = new[] { new { code = "WATER", quantity = 1 } } });
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [UnitCase("UTC020", "POST /api/stock/session-supplies/{id}/issue", "Abnormal", "recipientId and quantity missing", "Return: 400 INVALID_REQUEST", StaffLoggedIn)]
    public async Task Issue_MissingFields()
    {
        var (app, client) = await SignedIn(Staff);
        await using var _ = app;
        await AssertInvalid(await Send(client, HttpMethod.Post, "/api/stock/session-supplies/5/issue", new { productId = 1 }), "INVALID_REQUEST");
        Assert.Equal(0, app.Supplies.IssueCalls);
    }

    [UnitCase("UTC021", "GET /api/stock/session-supplies", "Abnormal", "date=abc", "Return: 400 INVALID_REQUEST", StaffLoggedIn)]
    public async Task Supplies_BadDate()
    {
        var (app, client) = await SignedIn(Staff);
        await using var _ = app;
        await AssertInvalid(await client.GetAsync("/api/stock/session-supplies?date=abc"), "INVALID_REQUEST");
    }

    [UnitCase("UTC022", "GET /api/stock/session-supplies", "Boundary", "date more than a year away", "Return: 400 VALIDATION_FAILED", StaffLoggedIn)]
    public async Task Supplies_FarDate()
    {
        var (app, client) = await SignedIn(Staff);
        await using var _ = app;
        var far = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(800)).ToString("yyyy-MM-dd");
        await AssertInvalid(await client.GetAsync($"/api/stock/session-supplies?date={far}"), "VALIDATION_FAILED");
    }

    [UnitCase("UTC023", "POST /api/stock/session-supplies/{id}/products/{pid}/reconcile", "Abnormal", "returnedQuantity missing", "Return: 400 INVALID_REQUEST", StaffLoggedIn)]
    public async Task Reconcile_MissingField()
    {
        var (app, client) = await SignedIn(Staff);
        await using var _ = app;
        await AssertInvalid(await Send(client, HttpMethod.Post, "/api/stock/session-supplies/5/products/1/reconcile", new { }), "INVALID_REQUEST");
    }

    [UnitCase("UTC024", "POST /api/stock/referee-supplies/{id}/products/{pid}/usage", "Abnormal", "usedQuantity missing", "Return: 400 INVALID_REQUEST", "Referee signed in")]
    public async Task Usage_MissingField()
    {
        var (app, client) = await SignedIn(Referee);
        await using var _ = app;
        await AssertInvalid(await Send(client, HttpMethod.Post, "/api/stock/referee-supplies/5/products/1/usage", new { }), "INVALID_REQUEST");
    }

    [UnitCase("UTC025", "POST /api/stock/referee-supplies/{id}/products/{pid}/usage", "Boundary", "usedQuantity=-1", "Return: 400 VALIDATION_FAILED", "Referee signed in")]
    public async Task Usage_Negative()
    {
        var (app, client) = await SignedIn(Referee);
        await using var _ = app;
        await AssertInvalid(await Send(client, HttpMethod.Post, "/api/stock/referee-supplies/5/products/1/usage", new { usedQuantity = -1 }), "VALIDATION_FAILED");
    }

    [UnitCase("UTC026", "GET /api/v1/commerce/my-goods-orders", "Abnormal", "no sign-in", "Return: 401 Unauthorized")]
    public async Task MyOrders_Anonymous()
    {
        await using var app = new ApiApp();
        using var client = app.Client();
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/v1/commerce/my-goods-orders")).StatusCode);
    }

    [UnitCase("UTC027", "GET /api/v1/commerce/my-goods-orders", "Abnormal", "signed in as Staff", "Return: 403 Forbidden", StaffLoggedIn)]
    public async Task MyOrders_StaffForbidden()
    {
        var (app, client) = await SignedIn(Staff);
        await using var _ = app;
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/v1/commerce/my-goods-orders")).StatusCode);
    }

    [UnitCase("UTC028", "POST /api/v1/commerce/my-goods-orders/{id}/receive", "Boundary", "id=0", "Return: 404 (route rejects ids below 1)", "Player signed in")]
    public async Task MyOrders_ZeroId()
    {
        var (app, client) = await SignedIn(Player);
        await using var _ = app;
        Assert.Equal(HttpStatusCode.NotFound, (await Send(client, HttpMethod.Post, "/api/v1/commerce/my-goods-orders/0/receive")).StatusCode);
    }

    [UnitCase("UTC029", "POST /api/v1/commerce/my-goods-orders/{id}/receive", "Abnormal", "X-TKSC-Request header missing", "Return: 400 invalid_request", "Player signed in")]
    public async Task MyOrders_NoHeader()
    {
        var (app, client) = await SignedIn(Player);
        await using var _ = app;
        var token = await client.GetFromJsonAsync<JsonElement>("/api/auth/csrf");
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/commerce/my-goods-orders/1/receive");
        request.Headers.Add("X-CSRF-TOKEN", token.GetProperty("token").GetString());
        Assert.Equal(HttpStatusCode.BadRequest, (await client.SendAsync(request)).StatusCode);
    }

    [UnitCase("UTC030", "POST /api/stock/session-supplies/{id}/issue", "Normal", "productId=1; quantity=12; recipientId=9; unitName=Tube", "Return: 200 with the allocation", StaffLoggedIn)]
    public async Task Issue_Valid()
    {
        var (app, client) = await SignedIn(Staff);
        await using var _ = app;
        var response = await Send(client, HttpMethod.Post, "/api/stock/session-supplies/5/issue", new { productId = 1, quantity = 12, recipientId = 9, unitName = "Tube" });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(Staff.Id, app.Supplies.LastOperator);
    }

    [UnitCase("UTC031", "POST /api/stock/session-supplies/{id}/issue", "Boundary", "unitName has 51 characters", "Return: 400 VALIDATION_FAILED; store not called", StaffLoggedIn)]
    public async Task Issue_LongUnit()
    {
        var (app, client) = await SignedIn(Staff);
        await using var _ = app;
        await AssertInvalid(await Send(client, HttpMethod.Post, "/api/stock/session-supplies/5/issue", new { productId = 1, quantity = 1, recipientId = 9, unitName = new string('a', 51) }), "VALIDATION_FAILED");
        Assert.Equal(0, app.Supplies.IssueCalls);
    }

    [UnitCase("UTC032", "GET /api/stock/session-supplies", "Normal", "no date (means today)", "Return: 200 with sessions", StaffLoggedIn)]
    public async Task Supplies_Today()
    {
        var (app, client) = await SignedIn(Staff);
        await using var _ = app;
        Assert.Single((await client.GetFromJsonAsync<List<JsonElement>>("/api/stock/session-supplies"))!);
    }

    [UnitCase("UTC033", "POST /api/stock/session-supplies/{id}/products/{pid}/reconcile", "Normal", "12 issued; returnedQuantity=3", "Return: 200; used=9", StaffLoggedIn)]
    public async Task Reconcile_Valid()
    {
        var (app, client) = await SignedIn(Staff);
        await using var _ = app;
        await app.Supplies.IssueAsync(5, new IssueSupplyRequest(1, 12, 9), Staff.Id, CancellationToken.None);
        var response = await Send(client, HttpMethod.Post, "/api/stock/session-supplies/5/products/1/reconcile", new { returnedQuantity = 3 });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var allocation = (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("allocations")[0];
        Assert.Equal(9, allocation.GetProperty("used").GetInt32());
    }

    [UnitCase("UTC034", "POST /api/stock/session-supplies/{id}/products/{pid}/reconcile", "Boundary", "returnedQuantity=-1", "Return: 400 VALIDATION_FAILED", StaffLoggedIn)]
    public async Task Reconcile_Negative()
    {
        var (app, client) = await SignedIn(Staff);
        await using var _ = app;
        await AssertInvalid(await Send(client, HttpMethod.Post, "/api/stock/session-supplies/5/products/1/reconcile", new { returnedQuantity = -1 }), "VALIDATION_FAILED");
    }

    [UnitCase("UTC035", "POST /api/stock/referee-supplies/{id}/products/{pid}/usage", "Normal", "assigned referee; 12 issued; usedQuantity=9", "Return: 200; reportedUsed=9", "Referee signed in and assigned to the session")]
    public async Task Usage_Valid()
    {
        var (app, client) = await SignedIn(Referee);
        await using var _ = app;
        app.Supplies.AssignedReferees.Add(Referee.Id);
        await app.Supplies.IssueAsync(5, new IssueSupplyRequest(1, 12, Referee.Id), Staff.Id, CancellationToken.None);
        var response = await Send(client, HttpMethod.Post, "/api/stock/referee-supplies/5/products/1/usage", new { usedQuantity = 9 });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var allocation = (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("allocations")[0];
        Assert.Equal(9, allocation.GetProperty("reportedUsed").GetInt32());
    }

    [UnitCase("UTC036", "GET /api/v1/commerce/my-goods-orders", "Abnormal", "signed in as Referee", "Return: 403 Forbidden", "Referee signed in")]
    public async Task MyOrders_RefereeForbidden()
    {
        var (app, client) = await SignedIn(Referee);
        await using var _ = app;
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/v1/commerce/my-goods-orders")).StatusCode);
    }

    [UnitCase("UTC037", "POST /api/v1/commerce/my-goods-orders/{id}/receive", "Abnormal", "no sign-in", "Return: 401 Unauthorized")]
    public async Task MyOrders_ReceiveAnonymous()
    {
        await using var app = new ApiApp();
        using var client = app.Client();
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsync("/api/v1/commerce/my-goods-orders/1/receive", null)).StatusCode);
    }

    private sealed class ApiApp : WebApplicationFactory<Program>
    {
        public FakeSessionAccounts Accounts { get; } = new();
        public FakeStockStore Stock { get; } = new();
        public FakeSupplyStore Supplies { get; } = new();

        public ApiApp()
        {
            foreach (var account in new[] { Staff, Referee, Player })
            {
                Accounts.Accounts[account.Email] = account;
                Accounts.AccountsById[account.Id] = account;
            }
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Authentication:Google:ClientId"] = "test-client",
                ["Authentication:Google:ClientSecret"] = "test-secret",
                ["ConnectionStrings:Sep490"] = "Server=unused;Database=unused"
            }));
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IAccountStore>();
                services.AddSingleton<IAccountStore>(Accounts);
                services.RemoveAll<IStockStore>();
                services.AddSingleton<IStockStore>(Stock);
                services.RemoveAll<ISessionSupplyStore>();
                services.AddSingleton<ISessionSupplyStore>(Supplies);
                services.PostConfigure<GoogleOptions>(GoogleDefaults.AuthenticationScheme,
                    options => options.Backchannel = new HttpClient(new GoogleStub(() => true)));
            });
        }

        public HttpClient Client() => CreateClient(new WebApplicationFactoryClientOptions
        { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false, HandleCookies = true });
    }
}
