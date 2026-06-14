using System.Net;
using System.Text;
using System.Text.Json;
using ECommerce.Contracts.DTOs.Common;
using ECommerce.SharedKernel.Constants;

namespace ECommerce.Tests.Integration;

[TestClass]
public class ProductsControllerTests
{
    private static readonly TestWebApplicationFactory _factory = SharedTestInfrastructure.Factory;
    private static readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };
    private static readonly Guid _seededProductId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    public TestContext TestContext { get; set; } = null!;

    private static StringContent Serialize(object dto) =>
        new(JsonSerializer.Serialize(dto), Encoding.UTF8, "application/json");

    private static async Task<ApiResponse<T>?> Deserialize<T>(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        string content = await response.Content.ReadAsStringAsync(cancellationToken);
        return JsonSerializer.Deserialize<ApiResponse<T>>(content, _jsonOptions);
    }

    private static Guid ExtractIdFromLocation(HttpResponseMessage response, string expectedPathSegment)
    {
        Assert.IsNotNull(response.Headers.Location, "Response must include Location header");
        string location = response.Headers.Location!.ToString();
        Assert.IsTrue(location.Contains(expectedPathSegment), $"Location '{location}' should contain '{expectedPathSegment}'");
        return Guid.Parse(location[(location.LastIndexOf('/') + 1)..]);
    }

    // ── GET /api/products ────────────────────────────────────────────────────

    [TestMethod]
    public async Task GetProducts_Returns200AndArray()
    {
        using var client = _factory.CreateUnauthenticatedClient();

        var res = await client.GetAsync("/api/products", TestContext.CancellationToken);

        Assert.AreEqual(HttpStatusCode.OK, res.StatusCode);
        var json = JsonSerializer.Deserialize<JsonElement>(await res.Content.ReadAsStringAsync(), _jsonOptions);
        Assert.IsTrue(json.TryGetProperty("data", out var data) && data.ValueKind != JsonValueKind.Null);
    }

    [TestMethod]
    public async Task GetProductById_Existing_Returns200AndProduct()
    {
        using var client = _factory.CreateUnauthenticatedClient();

        var res = await client.GetAsync($"/api/products/{_seededProductId}", TestContext.CancellationToken);

        Assert.AreEqual(HttpStatusCode.OK, res.StatusCode);
        var api = await Deserialize<JsonElement>(res, TestContext.CancellationToken);
        Assert.IsTrue(api != null && api.Success && api.Data!.TryGetProperty("id", out _));
    }

    [TestMethod]
    public async Task GetProductById_Nonexistent_Returns404()
    {
        using var client = _factory.CreateUnauthenticatedClient();

        var res = await client.GetAsync($"/api/products/{Guid.NewGuid()}", TestContext.CancellationToken);

        Assert.AreEqual(HttpStatusCode.NotFound, res.StatusCode);
    }

    [TestMethod]
    public async Task GetProductBySlug_Existing_Returns200()
    {
        using var client = _factory.CreateUnauthenticatedClient();

        var res = await client.GetAsync("/api/products/slug/integration-product", TestContext.CancellationToken);

        Assert.AreEqual(HttpStatusCode.OK, res.StatusCode);
    }

    [TestMethod]
    public async Task GetProductBySlug_Nonexistent_Returns404()
    {
        using var client = _factory.CreateUnauthenticatedClient();

        var res = await client.GetAsync("/api/products/slug/does-not-exist-xyz", TestContext.CancellationToken);

        Assert.AreEqual(HttpStatusCode.NotFound, res.StatusCode);
    }

    [TestMethod]
    public async Task GetFeaturedProducts_Returns200AndItems()
    {
        using var client = _factory.CreateUnauthenticatedClient();

        var res = await client.GetAsync("/api/products/featured", TestContext.CancellationToken);

        Assert.AreEqual(HttpStatusCode.OK, res.StatusCode);
        var json = JsonSerializer.Deserialize<JsonElement>(await res.Content.ReadAsStringAsync(), _jsonOptions);
        Assert.IsTrue(json.TryGetProperty("data", out var data) && data.TryGetProperty("items", out var items) && items.ValueKind == JsonValueKind.Array);
    }

    // ── POST /api/products ───────────────────────────────────────────────────

    [TestMethod]
    public async Task PostProduct_AdminValid_Returns302WithLocation()
    {
        using var client = _factory.CreateAdminClientNoRedirect();
        var catRes = await client.PostAsync("/api/categories",
            Serialize(new { Name = "TempCat", Slug = "temp-cat-" + Guid.NewGuid().ToString()[..8] }),
            TestContext.CancellationToken);
        Assert.AreEqual(HttpStatusCode.Created, catRes.StatusCode);
        var catId = ExtractIdFromLocation(catRes, "/api/categories/");

        var res = await client.PostAsync("/api/products",
            Serialize(new { Name = "NewProduct", Slug = "prod-" + Guid.NewGuid().ToString()[..8], Price = 9.99m, StockQuantity = 10, CategoryId = catId }),
            TestContext.CancellationToken);

        Assert.AreEqual(HttpStatusCode.Created, res.StatusCode);
        Assert.AreNotEqual(Guid.Empty, ExtractIdFromLocation(res, "/api/products/"));
    }

    [TestMethod]
    public async Task PostProduct_AdminMissingName_Returns400()
    {
        using var client = _factory.CreateAdminClientNoRedirect();

        var res = await client.PostAsync("/api/products",
            Serialize(new { Slug = "no-name-" + Guid.NewGuid().ToString()[..8], Price = 5m, StockQuantity = 1 }),
            TestContext.CancellationToken);

        Assert.AreEqual(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [TestMethod]
    public async Task PostProduct_AdminNegativePrice_Returns400()
    {
        using var client = _factory.CreateAdminClientNoRedirect();

        var res = await client.PostAsync("/api/products",
            Serialize(new { Name = "BadPrice", Slug = "bad-price-" + Guid.NewGuid().ToString()[..8], Price = -1m, StockQuantity = 1 }),
            TestContext.CancellationToken);

        Assert.AreEqual(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [TestMethod]
    public async Task PostProduct_DuplicateSlug_Returns422WithCode()
    {
        using var client = _factory.CreateAdminClientNoRedirect();
        var catRes = await client.PostAsync("/api/categories",
            Serialize(new { Name = "DupCat", Slug = "dup-cat-" + Guid.NewGuid().ToString()[..8] }),
            TestContext.CancellationToken);
        Assert.AreEqual(HttpStatusCode.Created, catRes.StatusCode);
        var catId = ExtractIdFromLocation(catRes, "/api/categories/");

        string slug = "dup-slug-" + Guid.NewGuid().ToString()[..8];
        var create = new { Name = "P1", Slug = slug, Price = 1m, StockQuantity = 1, CategoryId = catId };
        var r1 = await client.PostAsync("/api/products", Serialize(create), TestContext.CancellationToken);
        var r2 = await client.PostAsync("/api/products", Serialize(create), TestContext.CancellationToken);

        Assert.AreEqual(HttpStatusCode.Created, r1.StatusCode);
        Assert.AreEqual((HttpStatusCode)422, r2.StatusCode);
        var api = await Deserialize<object>(r2, TestContext.CancellationToken);
        Assert.IsFalse(api!.Success);
        Assert.AreEqual(ErrorCodes.DuplicateProductSlug, api.ErrorDetails!.Code);
    }

    [TestMethod]
    public async Task PostProduct_DuplicateSku_Returns422WithCode()
    {
        using var client = _factory.CreateAdminClientNoRedirect();
        var catRes = await client.PostAsync("/api/categories",
            Serialize(new { Name = "DupSkuCat", Slug = "dup-sku-cat-" + Guid.NewGuid().ToString()[..8] }),
            TestContext.CancellationToken);
        Assert.AreEqual(HttpStatusCode.Created, catRes.StatusCode);
        var catId = ExtractIdFromLocation(catRes, "/api/categories/");

        string sku = "dup-sku-" + Guid.NewGuid().ToString()[..8];
        var first = new { Name = "SkuOne", Slug = "sku-one-" + Guid.NewGuid().ToString()[..8], Price = 1m, StockQuantity = 1, CategoryId = catId, Sku = sku };
        var second = new { Name = "SkuTwo", Slug = "sku-two-" + Guid.NewGuid().ToString()[..8], Price = 1m, StockQuantity = 1, CategoryId = catId, Sku = sku };

        var r1 = await client.PostAsync("/api/products", Serialize(first), TestContext.CancellationToken);
        var r2 = await client.PostAsync("/api/products", Serialize(second), TestContext.CancellationToken);

        Assert.AreEqual(HttpStatusCode.Created, r1.StatusCode);
        Assert.AreEqual(HttpStatusCode.Conflict, r2.StatusCode);
        var api = await Deserialize<object>(r2, TestContext.CancellationToken);
        Assert.IsFalse(api!.Success);
        Assert.AreEqual(ErrorCodes.SkuAlreadyExists, api.ErrorDetails!.Code);
    }

    [TestMethod]
    public async Task PostProduct_Unauthenticated_Returns401()
    {
        using var client = _factory.CreateUnauthenticatedClient();

        var res = await client.PostAsync("/api/products",
            Serialize(new { Name = "X", Slug = "x-" + Guid.NewGuid().ToString()[..8], Price = 1m, StockQuantity = 1 }),
            TestContext.CancellationToken);

        Assert.AreEqual(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [TestMethod]
    public async Task PostProduct_CustomerRole_Returns403()
    {
        using var client = _factory.CreateAuthenticatedClient();

        var res = await client.PostAsync("/api/products",
            Serialize(new { Name = "X", Slug = "x-cust-" + Guid.NewGuid().ToString()[..8], Price = 1m, StockQuantity = 1 }),
            TestContext.CancellationToken);

        Assert.AreEqual(HttpStatusCode.Forbidden, res.StatusCode);
    }

    // ── PUT /api/products/{id} ───────────────────────────────────────────────

    [TestMethod]
    public async Task PutProduct_AdminExistingValid_Returns302WithLocation()
    {
        using var client = _factory.CreateAdminClientNoRedirect();
        var catRes = await client.PostAsync("/api/categories",
            Serialize(new { Name = "PutCat", Slug = "put-cat-" + Guid.NewGuid().ToString()[..8] }),
            TestContext.CancellationToken);
        Assert.AreEqual(HttpStatusCode.Created, catRes.StatusCode);
        var catId = ExtractIdFromLocation(catRes, "/api/categories/");

        var r = await client.PostAsync("/api/products",
            Serialize(new { Name = "ToUpdate", Slug = "to-update-" + Guid.NewGuid().ToString()[..8], Price = 5m, StockQuantity = 2, CategoryId = catId }),
            TestContext.CancellationToken);
        Assert.AreEqual(HttpStatusCode.Created, r.StatusCode);
        var id = ExtractIdFromLocation(r, "/api/products/");

        var res = await client.PutAsync($"/api/products/{id}",
            Serialize(new { Name = "UpdatedName", Slug = "updated-slug-" + Guid.NewGuid().ToString()[..8], Price = 6m, StockQuantity = 3, CategoryId = catId }),
            TestContext.CancellationToken);

        Assert.AreEqual(HttpStatusCode.OK, res.StatusCode);
        var body = JsonSerializer.Deserialize<JsonElement>(await res.Content.ReadAsStringAsync(TestContext.CancellationToken), _jsonOptions);
        Assert.AreEqual(id, body.GetProperty("data").GetProperty("id").GetGuid());
    }

    [TestMethod]
    public async Task PutProduct_AdminNonexistent_Returns404()
    {
        using var client = _factory.CreateAdminClientNoRedirect();

        var res = await client.PutAsync($"/api/products/{Guid.NewGuid()}",
            Serialize(new { Name = "No", Slug = "no-" + Guid.NewGuid().ToString()[..8], Price = 1m, StockQuantity = 1 }),
            TestContext.CancellationToken);

        Assert.AreEqual(HttpStatusCode.NotFound, res.StatusCode);
    }

    [TestMethod]
    public async Task PutProduct_AdminMissingName_Returns400()
    {
        using var client = _factory.CreateAdminClientNoRedirect();

        var res = await client.PutAsync($"/api/products/{_seededProductId}",
            Serialize(new { Slug = "no-name-" + Guid.NewGuid().ToString()[..8], Price = 1m, StockQuantity = 1 }),
            TestContext.CancellationToken);

        Assert.AreEqual(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [TestMethod]
    public async Task PutProduct_Unauthenticated_Returns401()
    {
        using var client = _factory.CreateUnauthenticatedClient();

        var res = await client.PutAsync($"/api/products/{_seededProductId}",
            Serialize(new { Name = "A", Slug = "a-" + Guid.NewGuid().ToString()[..8], Price = 1m, StockQuantity = 1 }),
            TestContext.CancellationToken);

        Assert.AreEqual(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [TestMethod]
    public async Task PutProduct_CustomerRole_Returns403()
    {
        using var client = _factory.CreateAuthenticatedClient();

        var res = await client.PutAsync($"/api/products/{_seededProductId}",
            Serialize(new { Name = "A", Slug = "a-" + Guid.NewGuid().ToString()[..8], Price = 1m, StockQuantity = 1 }),
            TestContext.CancellationToken);

        Assert.AreEqual(HttpStatusCode.Forbidden, res.StatusCode);
    }

    // ── DELETE /api/products/{id} ────────────────────────────────────────────

    [TestMethod]
    public async Task DeleteProduct_AdminExisting_Returns204()
    {
        using var client = _factory.CreateAdminClientNoRedirect();
        var catRes = await client.PostAsync("/api/categories",
            Serialize(new { Name = "DelCat", Slug = "del-cat-" + Guid.NewGuid().ToString()[..8] }),
            TestContext.CancellationToken);
        Assert.AreEqual(HttpStatusCode.Created, catRes.StatusCode);
        var catId = ExtractIdFromLocation(catRes, "/api/categories/");

        var r = await client.PostAsync("/api/products",
            Serialize(new { Name = "ToDelete", Slug = "to-delete-" + Guid.NewGuid().ToString()[..8], Price = 2m, StockQuantity = 1, CategoryId = catId }),
            TestContext.CancellationToken);
        Assert.AreEqual(HttpStatusCode.Created, r.StatusCode);
        var id = ExtractIdFromLocation(r, "/api/products/");

        var res = await client.SendAsync(new HttpRequestMessage(HttpMethod.Delete, $"/api/products/{id}"),
            TestContext.CancellationToken);

        Assert.AreEqual(HttpStatusCode.NoContent, res.StatusCode);
    }

    [TestMethod]
    public async Task DeleteProduct_AdminNonexistent_Returns404()
    {
        using var client = _factory.CreateAdminClient();

        var res = await client.SendAsync(new HttpRequestMessage(HttpMethod.Delete, $"/api/products/{Guid.NewGuid()}"),
            TestContext.CancellationToken);

        Assert.AreEqual(HttpStatusCode.NotFound, res.StatusCode);
    }

    [TestMethod]
    public async Task DeleteProduct_Unauthenticated_Returns401()
    {
        using var client = _factory.CreateUnauthenticatedClient();

        var res = await client.SendAsync(new HttpRequestMessage(HttpMethod.Delete, $"/api/products/{_seededProductId}"),
            TestContext.CancellationToken);

        Assert.AreEqual(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [TestMethod]
    public async Task DeleteProduct_CustomerRole_Returns403()
    {
        using var client = _factory.CreateAuthenticatedClient();

        var res = await client.SendAsync(new HttpRequestMessage(HttpMethod.Delete, $"/api/products/{_seededProductId}"),
            TestContext.CancellationToken);

        Assert.AreEqual(HttpStatusCode.Forbidden, res.StatusCode);
    }
}
