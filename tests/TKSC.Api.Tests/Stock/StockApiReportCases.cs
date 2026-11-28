using be_da.Application.DTOs;
using be_da.Domain.Entities;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;
using Xunit.Abstractions;
using static TKSC.Api.Tests.Stock.StockApiValidationTests;

namespace TKSC.Api.Tests.UnitTests.StockApi;

// Report 5.1 – StockApi (Lê Tuấn Dũng): request validation of the stock, supply and player goods-order APIs through
// the real ASP.NET pipeline with in-memory stores: roles, CSRF, missing fields, wrong types, route and value limits.
public sealed class StockApiReportCases(ITestOutputHelper output) : ReportCaseOutput(output)
{
    private static readonly object WaterLine = new[] { new { code = "WATER", quantity = 1 } };

    // "<status> <code>" from the API's error body, or "<status> <reason>" when the body has no code.
    private static async Task<string> Describe(HttpResponseMessage response, string? extra = null)
    {
        string? code = null;
        var text = await response.Content.ReadAsStringAsync();
        if (text.TrimStart().StartsWith('{'))
        {
            using var json = JsonDocument.Parse(text);
            if (json.RootElement.TryGetProperty("code", out var value))
                code = value.GetString();
        }

        return $"{(int)response.StatusCode} {code ?? response.StatusCode.ToString()}" + (extra is null ? "" : $"; {extra}");
    }

    private static async Task<string> Call(Account who, HttpMethod method, string path, object? body = null, string? raw = null)
    {
        var (app, client) = await SignedIn(who);
        await using var _ = app;
        return await Describe(await Send(client, method, path, body, raw));
    }

    private static async Task<string> Get(Account who, string path)
    {
        var (app, client) = await SignedIn(who);
        await using var _ = app;
        return await Describe(await client.GetAsync(path));
    }

    // ---------------- POST /api/stock/products
    [Fact, Trait("Type", "Normal"), Trait("Pre", "Manager signed in with a valid CSRF token")]
    public Task UTC001_CreateProductValid() =>
        CheckAsync("POST /api/stock/products", "body={sku:\"CAU-1\", name:\"Shuttle\", category:\"Equipment\", unitPriceVnd:1000, reorderLevel:1, initialQuantity:0}",
            "201 Created", () => Call(Manager, HttpMethod.Post, "/api/stock/products", ValidProduct));

    [Fact, Trait("Type", "Abnormal"), Trait("Pre", "Manager signed in with a valid CSRF token")]
    public Task UTC002_CreateProductMissingPrice() =>
        CheckAsync("POST /api/stock/products", "body={sku:\"CAU-1\", name:\"Shuttle\", reorderLevel:1, initialQuantity:0} (unitPriceVnd missing)",
            "400 INVALID_REQUEST", () => Call(Manager, HttpMethod.Post, "/api/stock/products", new { sku = "CAU-1", name = "Shuttle", reorderLevel = 1, initialQuantity = 0 }));

    [Fact, Trait("Type", "Abnormal"), Trait("Pre", "Manager signed in with a valid CSRF token")]
    public Task UTC003_CreateProductWrongType() =>
        CheckAsync("POST /api/stock/products", "body={..., unitPriceVnd:\"abc\"} (wrong type)",
            "400 INVALID_REQUEST", () => Call(Manager, HttpMethod.Post, "/api/stock/products", new { sku = "CAU-1", name = "Shuttle", unitPriceVnd = "abc", reorderLevel = 1, initialQuantity = 0 }));

    [Fact, Trait("Type", "Abnormal"), Trait("Pre", "Manager signed in; request sent without the CSRF header")]
    public Task UTC004_CreateProductNoCsrf() =>
        CheckAsync("POST /api/stock/products", "valid body; no X-CSRF-TOKEN header", "400 invalid_csrf_token", async () =>
        {
            var (app, client) = await SignedIn(Manager);
            await using var _ = app;
            return await Describe(await client.PostAsJsonAsync("/api/stock/products", ValidProduct));
        });

    [Fact, Trait("Type", "Abnormal"), Trait("Pre", "Player signed in with a valid CSRF token")]
    public Task UTC005_CreateProductPlayer() =>
        CheckAsync("POST /api/stock/products", "valid body; role=Player", "403 Forbidden", () => Call(Player, HttpMethod.Post, "/api/stock/products", ValidProduct));

    // ---------------- PUT /api/stock/products/{id}
    [Fact, Trait("Type", "Abnormal"), Trait("Pre", "Manager signed in with a valid CSRF token")]
    public Task UTC006_UpdateProductMissingIsActive() =>
        CheckAsync("PUT /api/stock/products/{id}", "id=1; body={name:\"Shuttle\", unitPriceVnd:1, reorderLevel:1} (isActive missing)",
            "400 INVALID_REQUEST", () => Call(Manager, HttpMethod.Put, "/api/stock/products/1", new { name = "Shuttle", unitPriceVnd = 1, reorderLevel = 1 }));

    [Fact, Trait("Type", "Boundary"), Trait("Pre", "Manager signed in with a valid CSRF token")]
    public Task UTC007_UpdateProductZeroId() =>
        CheckAsync("PUT /api/stock/products/{id}", "id=0; valid body", "404 NotFound",
            () => Call(Manager, HttpMethod.Put, "/api/stock/products/0", new { name = "Shuttle", unitPriceVnd = 1, reorderLevel = 1, isActive = true }));

    [Fact, Trait("Type", "Abnormal"), Trait("Pre", "Manager signed in with a valid CSRF token")]
    public Task UTC008_UpdateProductTwoBaseUnits() =>
        CheckAsync("PUT /api/stock/products/{id}", "id=1; units=[A=1, B=1] (two base units)", "400 VALIDATION_FAILED",
            () => Call(Manager, HttpMethod.Put, "/api/stock/products/1", new
            {
                name = "Shuttle", unitPriceVnd = 1, reorderLevel = 1, isActive = true,
                units = new[] { new { unitName = "A", factorToBase = 1 }, new { unitName = "B", factorToBase = 1 } }
            }));

    // ---------------- POST /api/stock/products/{id}/movements
    [Fact, Trait("Type", "Abnormal"), Trait("Pre", "Staff signed in with a valid CSRF token")]
    public Task UTC009_MovementDecimalQuantity() =>
        CheckAsync("POST /api/stock/products/{id}/movements", "id=1; body={\"type\":\"Receipt\",\"quantity\":1.5}", "400 INVALID_REQUEST",
            () => Call(Staff, HttpMethod.Post, "/api/stock/products/1/movements", raw: """{"type":"Receipt","quantity":1.5}"""));

    [Fact, Trait("Type", "Boundary"), Trait("Pre", "Staff signed in with a valid CSRF token")]
    public Task UTC010_MovementZeroQuantity() =>
        CheckAsync("POST /api/stock/products/{id}/movements", "id=1; body={type:\"Receipt\", quantity:0}", "400 VALIDATION_FAILED",
            () => Call(Staff, HttpMethod.Post, "/api/stock/products/1/movements", new { type = "Receipt", quantity = 0 }));

    // ---------------- GET /api/stock/lookup
    [Fact, Trait("Type", "Normal"), Trait("Pre", "Staff signed in; barcode 9930001 is the Thùng unit of WATER")]
    public Task UTC011_LookupFound() =>
        CheckAsync("GET /api/stock/lookup", "code=9930001", "200 OK; factorToBase=24", async () =>
        {
            var (app, client) = await SignedIn(Staff);
            await using var _ = app;
            app.Stock.Lookups["9930001"] = new StockCodeLookup(1, "WATER", "Nước suối 500ml", true, 10000, 5, "Chai", "Thùng", 24, "9930001");
            var response = await client.GetAsync("/api/stock/lookup?code=9930001");
            var factor = (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("factorToBase").GetInt32();
            return $"{(int)response.StatusCode} {response.StatusCode}; factorToBase={factor}";
        });

    [Fact, Trait("Type", "Abnormal"), Trait("Pre", "Staff signed in")]
    public Task UTC012_LookupScript() =>
        CheckAsync("GET /api/stock/lookup", "code=<script>", "400 VALIDATION_FAILED", () => Get(Staff, "/api/stock/lookup?code=%3Cscript%3E"));

    [Fact, Trait("Type", "Abnormal"), Trait("Pre", "Staff signed in; code not in the catalogue")]
    public Task UTC013_LookupNotFound() =>
        CheckAsync("GET /api/stock/lookup", "code=NOPE1234", "404 CODE_NOT_FOUND", () => Get(Staff, "/api/stock/lookup?code=NOPE1234"));

    // ---------------- POST /api/stock/receipts/batch
    [Fact, Trait("Type", "Normal"), Trait("Pre", "Staff signed in with a valid CSRF token")]
    public Task UTC014_BatchValid() =>
        CheckAsync("POST /api/stock/receipts/batch", "body={sourceRef:\"PN-01\", lines:[{code:\"WATER\", quantity:1}]}", "200 OK",
            () => Call(Staff, HttpMethod.Post, "/api/stock/receipts/batch", new { sourceRef = "PN-01", lines = WaterLine }));

    [Fact, Trait("Type", "Abnormal"), Trait("Pre", "Staff signed in with a valid CSRF token")]
    public Task UTC015_BatchNoRef() =>
        CheckAsync("POST /api/stock/receipts/batch", "body={lines:[{code:\"WATER\", quantity:1}]} (sourceRef missing)", "400 VALIDATION_FAILED",
            () => Call(Staff, HttpMethod.Post, "/api/stock/receipts/batch", new { lines = WaterLine }));

    [Fact, Trait("Type", "Abnormal"), Trait("Pre", "Staff signed in; reference PN-01 already used by another receipt")]
    public Task UTC016_BatchRefUsed() =>
        CheckAsync("POST /api/stock/receipts/batch", "body={sourceRef:\"PN-01\", lines:[{code:\"WATER\", quantity:1}]}", "409 SOURCE_REF_USED", async () =>
        {
            var (app, client) = await SignedIn(Staff);
            await using var _ = app;
            app.Stock.NextBatchResult = BatchReceiptResult.Fail("SOURCE_REF_USED", "Mã phiếu đã dùng cho phiếu nhập khác.");
            return await Describe(await Send(client, HttpMethod.Post, "/api/stock/receipts/batch", new { sourceRef = "PN-01", lines = WaterLine }));
        });

    // ---------------- POST /api/stock/session-supplies/{id}/issue, reconcile
    [Fact, Trait("Type", "Normal"), Trait("Pre", "Staff signed in with a valid CSRF token")]
    public Task UTC017_IssueValid() =>
        CheckAsync("POST /api/stock/session-supplies/{id}/issue", "id=5; body={productId:1, quantity:12, recipientId:9, unitName:\"Ống\"}", "200 OK",
            () => Call(Staff, HttpMethod.Post, "/api/stock/session-supplies/5/issue", new { productId = 1, quantity = 12, recipientId = 9, unitName = "Ống" }));

    [Fact, Trait("Type", "Abnormal"), Trait("Pre", "Staff signed in with a valid CSRF token")]
    public Task UTC018_IssueMissingFields() =>
        CheckAsync("POST /api/stock/session-supplies/{id}/issue", "id=5; body={productId:1} (quantity, recipientId missing)", "400 INVALID_REQUEST",
            () => Call(Staff, HttpMethod.Post, "/api/stock/session-supplies/5/issue", new { productId = 1 }));

    [Fact, Trait("Type", "Abnormal"), Trait("Pre", "Staff signed in")]
    public Task UTC019_SuppliesBadDate() =>
        CheckAsync("GET /api/stock/session-supplies", "date=abc", "400 INVALID_REQUEST", () => Get(Staff, "/api/stock/session-supplies?date=abc"));

    [Fact, Trait("Type", "Normal"), Trait("Pre", "Staff signed in; 12 items issued to session 5")]
    public Task UTC020_ReconcileValid() =>
        CheckAsync("POST /api/stock/session-supplies/{id}/products/{pid}/reconcile", "id=5; pid=1; body={returnedQuantity:3}", "200 OK; used=9", async () =>
        {
            var (app, client) = await SignedIn(Staff);
            await using var _ = app;
            await app.Supplies.IssueAsync(5, new IssueSupplyRequest(1, 12, 9), Staff.Id, CancellationToken.None);
            var response = await Send(client, HttpMethod.Post, "/api/stock/session-supplies/5/products/1/reconcile", new { returnedQuantity = 3 });
            var used = (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("allocations")[0].GetProperty("used").GetInt32();
            return $"{(int)response.StatusCode} {response.StatusCode}; used={used}";
        });

    // ---------------- POST /api/stock/referee-supplies/{id}/products/{pid}/usage
    [Fact, Trait("Type", "Boundary"), Trait("Pre", "Referee signed in with a valid CSRF token")]
    public Task UTC021_UsageNegative() =>
        CheckAsync("POST /api/stock/referee-supplies/{id}/products/{pid}/usage", "id=5; pid=1; body={usedQuantity:-1}", "400 VALIDATION_FAILED",
            () => Call(Referee, HttpMethod.Post, "/api/stock/referee-supplies/5/products/1/usage", new { usedQuantity = -1 }));

    // ---------------- GET /api/v1/commerce/my-goods-orders
    [Fact, Trait("Type", "Abnormal"), Trait("Pre", "Nobody signed in")]
    public Task UTC022_MyOrdersAnonymous() =>
        CheckAsync("GET /api/v1/commerce/my-goods-orders", "no sign-in", "401 Unauthorized", async () =>
        {
            await using var app = new ApiApp();
            using var client = app.Client();
            return await Describe(await client.GetAsync("/api/v1/commerce/my-goods-orders"));
        });

    [Fact, Trait("Type", "Abnormal"), Trait("Pre", "Staff signed in (not a buyer)")]
    public Task UTC023_MyOrdersStaff() =>
        CheckAsync("GET /api/v1/commerce/my-goods-orders", "role=Staff", "403 Forbidden", () => Get(Staff, "/api/v1/commerce/my-goods-orders"));
}
