using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ECommerce.Contracts.DTOs.Common;
using ECommerce.Core.Constants;

namespace ECommerce.Tests.Integration;

[TestClass]
public class ProductsCharacterizationTests
{
    private TestWebApplicationFactory _factory = null!;
    private static readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };
    private static readonly Guid _seededProductId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    public TestContext TestContext { get; set; } = null!;

    [TestInitialize]
    public void Setup() => _factory = new TestWebApplicationFactory();

    [TestCleanup]
    public void Cleanup()
    {
        ConditionalTestAuthHandler.IsAuthenticationEnabled = true;
        ConditionalTestAuthHandler.CurrentUserId = ConditionalTestAuthHandler.TestUserId;
        ConditionalTestAuthHandler.CurrentUserRole = "Customer";
        _factory?.Dispose();
    }

    private static StringContent Serialize(object dto) =>
        new(JsonSerializer.Serialize(dto), Encoding.UTF8, "application/json");

    private static async Task<ApiResponse<T>?> Deserialize<T>(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        string content = await response.Content.ReadAsStringAsync(cancellationToken);
        return JsonSerializer.Deserialize<ApiResponse<T>>(content, _jsonOptions);
    }

    // ---------- PRODUCTS ----------

    [TestMethod]
    public async Task GetProducts_Returns200AndArray()
    {
        // Arrange
        using HttpClient client = _factory.CreateUnauthenticatedClient();

        // Act
        HttpResponseMessage res = await client.GetAsync("/api/products", TestContext.CancellationToken);

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, res.StatusCode);
        string body = await res.Content.ReadAsStringAsync();
        JsonElement json = JsonSerializer.Deserialize<JsonElement>(body, _jsonOptions);
        Assert.IsTrue(json.TryGetProperty("data", out var data) && data.ValueKind != JsonValueKind.Null);
    }

    [TestMethod]
    public async Task GetProductById_Existing_Returns200AndProduct()
    {
        // Arrange
        using HttpClient client = _factory.CreateUnauthenticatedClient();

        // Act
        HttpResponseMessage res = await client.GetAsync($"/api/products/{_seededProductId}", TestContext.CancellationToken);

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, res.StatusCode);
        ApiResponse<JsonElement>? api = await Deserialize<JsonElement>(res, TestContext.CancellationToken);
        Assert.IsTrue(api != null && api.Success && api.Data!.TryGetProperty("id", out _));
    }

    [TestMethod]
    public async Task GetProductById_Nonexistent_Returns404()
    {
        // Arrange
        using HttpClient client = _factory.CreateUnauthenticatedClient();

        // Act
        HttpResponseMessage res = await client.GetAsync($"/api/products/{Guid.NewGuid()}", TestContext.CancellationToken);

        // Assert
        Assert.AreEqual(HttpStatusCode.NotFound, res.StatusCode);
    }

    [TestMethod]
    public async Task GetProductBySlug_Existing_Returns200()
    {
        // Arrange
        using HttpClient client = _factory.CreateUnauthenticatedClient();

        // Act
        HttpResponseMessage res = await client.GetAsync("/api/products/slug/integration-product", TestContext.CancellationToken);

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, res.StatusCode);
    }

    [TestMethod]
    public async Task GetProductBySlug_Nonexistent_Returns404()
    {
        // Arrange
        using HttpClient client = _factory.CreateUnauthenticatedClient();

        // Act
        HttpResponseMessage res = await client.GetAsync("/api/products/slug/does-not-exist-xyz", TestContext.CancellationToken);

        // Assert
        Assert.AreEqual(HttpStatusCode.NotFound, res.StatusCode);
    }

    [TestMethod]
    public async Task GetFeaturedProducts_Returns200AndItems()
    {
        // Arrange
        using HttpClient client = _factory.CreateUnauthenticatedClient();

        // Act
        HttpResponseMessage res = await client.GetAsync("/api/products/featured", TestContext.CancellationToken);

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, res.StatusCode);
        string body = await res.Content.ReadAsStringAsync();
        JsonElement json = JsonSerializer.Deserialize<JsonElement>(body, _jsonOptions);
        Assert.IsTrue(json.TryGetProperty("data", out var data) && data.TryGetProperty("items", out var items) && items.ValueKind == JsonValueKind.Array);
    }

    [TestMethod]
    public async Task PostProduct_AdminValid_Returns201AndMatches()
    {
        // Arrange
        using HttpClient client = _factory.CreateAdminClient();
        // create category first
        var createCat = new { Name = "TempCat", Slug = "temp-cat-" + Guid.NewGuid().ToString().Substring(0,8) };

        // Act (create category)
        HttpResponseMessage catRes = await client.PostAsync("/api/categories", Serialize(createCat), TestContext.CancellationToken);

        // Assert category created
        Assert.AreEqual(HttpStatusCode.Created, catRes.StatusCode);
        ApiResponse<JsonElement>? catApi = await Deserialize<JsonElement>(catRes, TestContext.CancellationToken);
        Guid catId = Guid.Parse(catApi!.Data!.GetProperty("id").GetString()!);

        // Arrange (product)
        var slug = "prod-" + Guid.NewGuid().ToString().Substring(0,8);
        var create = new { Name = "NewProduct", Slug = slug, Price = 9.99m, StockQuantity = 10, CategoryId = catId };

        // Act (create product)
        HttpResponseMessage res = await client.PostAsync("/api/products", Serialize(create), TestContext.CancellationToken);

        // Assert
        Assert.AreEqual(HttpStatusCode.Created, res.StatusCode);
        ApiResponse<JsonElement>? api = await Deserialize<JsonElement>(res, TestContext.CancellationToken);
        Assert.IsTrue(api != null && api.Success && api.Data!.GetProperty("name").GetString() == "NewProduct");
    }

    [TestMethod]
    public async Task PostProduct_AdminMissingName_Returns400()
    {
        // Arrange
        using HttpClient client = _factory.CreateAdminClient();
        var create = new { Slug = "no-name-" + Guid.NewGuid().ToString().Substring(0,8), Price = 5m, StockQuantity = 1 };

        // Act
        HttpResponseMessage res = await client.PostAsync("/api/products", Serialize(create), TestContext.CancellationToken);

        // Assert
        Assert.AreEqual(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [TestMethod]
    public async Task PostProduct_AdminNegativePrice_Returns400()
    {
        // Arrange
        using HttpClient client = _factory.CreateAdminClient();
        var create = new { Name = "BadPrice", Slug = "bad-price-" + Guid.NewGuid().ToString().Substring(0,8), Price = -1m, StockQuantity = 1 };

        // Act
        HttpResponseMessage res = await client.PostAsync("/api/products", Serialize(create), TestContext.CancellationToken);

        // Assert
        Assert.AreEqual(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [TestMethod]
    public async Task PostProduct_DuplicateSlug_Returns422WithCode()
    {
        // Arrange
        using HttpClient client = _factory.CreateAdminClient();
        // create category
        var createCat = new { Name = "DupCat", Slug = "dup-cat-" + Guid.NewGuid().ToString().Substring(0,8) };

        // Act (create category)
        HttpResponseMessage catRes = await client.PostAsync("/api/categories", Serialize(createCat), TestContext.CancellationToken);

        // Assert category created
        Assert.AreEqual(HttpStatusCode.Created, catRes.StatusCode);
        ApiResponse<JsonElement>? catApi = await Deserialize<JsonElement>(catRes, TestContext.CancellationToken);
        Guid catId = Guid.Parse(catApi!.Data!.GetProperty("id").GetString()!);

        // Arrange (product)
        var slug = "dup-slug-" + Guid.NewGuid().ToString().Substring(0,8);
        var create = new { Name = "P1", Slug = slug, Price = 1m, StockQuantity = 1, CategoryId = catId };

        // Act (create product twice)
        HttpResponseMessage r1 = await client.PostAsync("/api/products", Serialize(create), TestContext.CancellationToken);
        HttpResponseMessage r2 = await client.PostAsync("/api/products", Serialize(create), TestContext.CancellationToken);

        // Assert
        Assert.AreEqual(HttpStatusCode.Created, r1.StatusCode);
        Assert.AreEqual((HttpStatusCode)422, r2.StatusCode);
        ApiResponse<object>? api = await Deserialize<object>(r2, TestContext.CancellationToken);
        Assert.IsFalse(api!.Success);
        Assert.AreEqual(ErrorCodes.DuplicateProductSlug, api.ErrorDetails!.Code);
    }

    [TestMethod]
    public async Task PostProduct_Unauthenticated_Returns401()
    {
        // Arrange
        using HttpClient client = _factory.CreateUnauthenticatedClient();
        var create = new { Name = "X", Slug = "x-" + Guid.NewGuid().ToString().Substring(0,8), Price = 1m, StockQuantity = 1 };

        // Act
        HttpResponseMessage res = await client.PostAsync("/api/products", Serialize(create), TestContext.CancellationToken);

        // Assert
        Assert.AreEqual(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [TestMethod]
    public async Task PostProduct_CustomerRole_Returns403()
    {
        // Arrange
        using HttpClient client = _factory.CreateAuthenticatedClient();
        var create = new { Name = "X", Slug = "x-cust-" + Guid.NewGuid().ToString().Substring(0,8), Price = 1m, StockQuantity = 1 };

        // Act
        HttpResponseMessage res = await client.PostAsync("/api/products", Serialize(create), TestContext.CancellationToken);

        // Assert
        Assert.AreEqual(HttpStatusCode.Forbidden, res.StatusCode);
    }

    [TestMethod]
    public async Task PutProduct_AdminExistingValid_Returns200AndUpdated()
    {
        // Arrange
        using HttpClient client = _factory.CreateAdminClient();
        // Create product first
        var createCat = new { Name = "PutCat", Slug = "put-cat-" + Guid.NewGuid().ToString().Substring(0,8) };

        // Act (create category)
        HttpResponseMessage catRes = await client.PostAsync("/api/categories", Serialize(createCat), TestContext.CancellationToken);

        // Assert category created
        Assert.AreEqual(HttpStatusCode.Created, catRes.StatusCode);
        ApiResponse<JsonElement>? catApi = await Deserialize<JsonElement>(catRes, TestContext.CancellationToken);
        Guid catId = Guid.Parse(catApi!.Data!.GetProperty("id").GetString()!);

        // Arrange (create product)
        var create = new { Name = "ToUpdate", Slug = "to-update-" + Guid.NewGuid().ToString().Substring(0,8), Price = 5m, StockQuantity = 2, CategoryId = catId };

        // Act (create product)
        HttpResponseMessage r = await client.PostAsync("/api/products", Serialize(create), TestContext.CancellationToken);

        // Assert product created
        Assert.AreEqual(HttpStatusCode.Created, r.StatusCode);
        ApiResponse<JsonElement>? created = await Deserialize<JsonElement>(r, TestContext.CancellationToken);
        Guid id = Guid.Parse(created!.Data!.GetProperty("id").GetString()!);

        // Arrange (update)
        var update = new { Name = "UpdatedName", Slug = "updated-slug-" + Guid.NewGuid().ToString().Substring(0,8), Price = 6m, StockQuantity = 3, CategoryId = catId };

        // Act (update product)
        HttpResponseMessage res = await client.PutAsync($"/api/products/{id}", Serialize(update), TestContext.CancellationToken);

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, res.StatusCode);
        ApiResponse<JsonElement>? api = await Deserialize<JsonElement>(res, TestContext.CancellationToken);
        Assert.AreEqual("UpdatedName", api!.Data!.GetProperty("name").GetString());
    }

    [TestMethod]
    public async Task PutProduct_AdminNonexistent_Returns404()
    {
        // Arrange
        using HttpClient client = _factory.CreateAdminClient();
        var update = new { Name = "No", Slug = "no-" + Guid.NewGuid().ToString().Substring(0,8), Price = 1m, StockQuantity = 1 };

        // Act
        HttpResponseMessage res = await client.PutAsync($"/api/products/{Guid.NewGuid()}", Serialize(update), TestContext.CancellationToken);

        // Assert
        Assert.AreEqual(HttpStatusCode.NotFound, res.StatusCode);
    }

    [TestMethod]
    public async Task PutProduct_AdminMissingName_Returns400()
    {
        // Arrange
        using HttpClient client = _factory.CreateAdminClient();
        var update = new { Slug = "no-name-" + Guid.NewGuid().ToString().Substring(0,8), Price = 1m, StockQuantity = 1 };

        // Act
        HttpResponseMessage res = await client.PutAsync($"/api/products/{_seededProductId}", Serialize(update), TestContext.CancellationToken);

        // Assert
        Assert.AreEqual(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [TestMethod]
    public async Task PutProduct_Unauthenticated_Returns401()
    {
        // Arrange
        using HttpClient client = _factory.CreateUnauthenticatedClient();
        var update = new { Name = "A", Slug = "a-" + Guid.NewGuid().ToString().Substring(0,8), Price = 1m, StockQuantity = 1 };

        // Act
        HttpResponseMessage res = await client.PutAsync($"/api/products/{_seededProductId}", Serialize(update), TestContext.CancellationToken);

        // Assert
        Assert.AreEqual(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [TestMethod]
    public async Task PutProduct_CustomerRole_Returns403()
    {
        // Arrange
        using HttpClient client = _factory.CreateAuthenticatedClient();
        var update = new { Name = "A", Slug = "a-" + Guid.NewGuid().ToString().Substring(0,8), Price = 1m, StockQuantity = 1 };

        // Act
        HttpResponseMessage res = await client.PutAsync($"/api/products/{_seededProductId}", Serialize(update), TestContext.CancellationToken);

        // Assert
        Assert.AreEqual(HttpStatusCode.Forbidden, res.StatusCode);
    }

    [TestMethod]
    public async Task DeleteProduct_AdminExisting_ReturnsSuccess()
    {
        // Arrange
        using HttpClient client = _factory.CreateAdminClient();
        // create product
        var createCat = new { Name = "DelCat", Slug = "del-cat-" + Guid.NewGuid().ToString().Substring(0,8) };

        // Act (create category)
        HttpResponseMessage catRes = await client.PostAsync("/api/categories", Serialize(createCat), TestContext.CancellationToken);

        // Assert category created
        Assert.AreEqual(HttpStatusCode.Created, catRes.StatusCode);
        ApiResponse<JsonElement>? catApi = await Deserialize<JsonElement>(catRes, TestContext.CancellationToken);
        Guid catId = Guid.Parse(catApi!.Data!.GetProperty("id").GetString()!);

        // Arrange (product)
        var create = new { Name = "ToDelete", Slug = "to-delete-" + Guid.NewGuid().ToString().Substring(0,8), Price = 2m, StockQuantity = 1, CategoryId = catId };

        // Act (create product)
        HttpResponseMessage r = await client.PostAsync("/api/products", Serialize(create), TestContext.CancellationToken);

        // Assert product created
        Assert.AreEqual(HttpStatusCode.Created, r.StatusCode);
        ApiResponse<JsonElement>? created = await Deserialize<JsonElement>(r, TestContext.CancellationToken);
        Guid id = Guid.Parse(created!.Data!.GetProperty("id").GetString()!);

        // Act (delete)
        HttpResponseMessage res = await client.SendAsync(new HttpRequestMessage(HttpMethod.Delete, $"/api/products/{id}"), TestContext.CancellationToken);

        // Assert delete success
        Assert.IsTrue(res.StatusCode == HttpStatusCode.OK || res.StatusCode == HttpStatusCode.NoContent);
    }

    [TestMethod]
    public async Task DeleteProduct_AdminNonexistent_Returns404()
    {
        // Arrange
        using HttpClient client = _factory.CreateAdminClient();

        // Act
        HttpResponseMessage res = await client.SendAsync(new HttpRequestMessage(HttpMethod.Delete, $"/api/products/{Guid.NewGuid()}"), TestContext.CancellationToken);

        // Assert
        Assert.AreEqual(HttpStatusCode.NotFound, res.StatusCode);
    }

    [TestMethod]
    public async Task DeleteProduct_Unauthenticated_Returns401()
    {
        // Arrange
        using HttpClient client = _factory.CreateUnauthenticatedClient();

        // Act
        HttpResponseMessage res = await client.SendAsync(new HttpRequestMessage(HttpMethod.Delete, $"/api/products/{_seededProductId}"), TestContext.CancellationToken);

        // Assert
        Assert.AreEqual(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [TestMethod]
    public async Task DeleteProduct_CustomerRole_Returns403()
    {
        // Arrange
        using HttpClient client = _factory.CreateAuthenticatedClient();

        // Act
        HttpResponseMessage res = await client.SendAsync(new HttpRequestMessage(HttpMethod.Delete, $"/api/products/{_seededProductId}"), TestContext.CancellationToken);

        // Assert
        Assert.AreEqual(HttpStatusCode.Forbidden, res.StatusCode);
    }
}

