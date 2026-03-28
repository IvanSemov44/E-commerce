using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ECommerce.Tests.Integration;

/// <summary>
/// Integration tests for ProductsController endpoints.
/// Tests product retrieval with various filters and CRUD operations.
/// </summary>
[TestClass]
public class ProductsControllerTests
{
    private TestWebApplicationFactory _factory = null!;
    private static readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };
    private static readonly Guid ExistingProductId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    [TestInitialize]
    public void Setup()
    {
        _factory = new TestWebApplicationFactory();
    }

    [TestCleanup]
    public void Cleanup()
    {
        // Reset authentication state
        ConditionalTestAuthHandler.IsAuthenticationEnabled = true;
        ConditionalTestAuthHandler.CurrentUserId = ConditionalTestAuthHandler.TestUserId;
        ConditionalTestAuthHandler.CurrentUserRole = "Customer";
        _factory?.Dispose();
    }

    #region Get Products Tests

    [TestMethod]
    public async Task GetProducts_WithDefaultQuery_ReturnsSuccessfulResponse()
    {
        // Arrange
        using var client = _factory.CreateUnauthenticatedClient();

        // Act
        var response = await client.GetAsync("/api/products");

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
    }

    [TestMethod]
    public async Task GetProducts_WithPagination_ReturnsPaginatedResult()
    {
        // Arrange
        using var client = _factory.CreateUnauthenticatedClient();

        // Act
        var response = await client.GetAsync("/api/products?page=1&pageSize=10");
        var responseContent = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        var jsonOptions = _jsonOptions;
        var responseData = JsonSerializer.Deserialize<JsonElement>(responseContent, jsonOptions);
        Assert.IsTrue(responseData.TryGetProperty("data", out _), "Response should have data property");
    }

    [TestMethod]
    public async Task GetProducts_WithMinPriceFilter_ReturnsFilteredResult()
    {
        // Arrange
        using var client = _factory.CreateUnauthenticatedClient();

        // Act
        var response = await client.GetAsync("/api/products?minPrice=5");

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
    }

    [TestMethod]
    public async Task GetProducts_WithMaxPriceFilter_ReturnsFilteredResult()
    {
        // Arrange
        using var client = _factory.CreateUnauthenticatedClient();

        // Act
        var response = await client.GetAsync("/api/products?maxPrice=50");

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
    }

    [TestMethod]
    public async Task GetProducts_WithSearchQuery_ReturnsSearchResults()
    {
        // Arrange
        using var client = _factory.CreateUnauthenticatedClient();

        // Act
        var response = await client.GetAsync("/api/products?search=integration");

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
    }

    #endregion

    #region Get Featured Products Tests

    [TestMethod]
    public async Task GetFeaturedProducts_WithDefaultCount_ReturnsSuccessfulResponse()
    {
        // Arrange
        using var client = _factory.CreateUnauthenticatedClient();

        // Act
        var response = await client.GetAsync("/api/products/featured");
        var responseContent = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        var jsonOptions = _jsonOptions;
        var responseData = JsonSerializer.Deserialize<JsonElement>(responseContent, jsonOptions);

        Assert.IsTrue(responseData.TryGetProperty("data", out var data), "Response should have data property");
        Assert.IsTrue(data.TryGetProperty("items", out var items), "Featured response should have items");
        Assert.AreEqual(JsonValueKind.Array, items.ValueKind);
        Assert.IsTrue(data.TryGetProperty("totalCount", out _), "Featured response should have totalCount");
        Assert.IsTrue(data.TryGetProperty("page", out _), "Featured response should have page");
        Assert.IsTrue(data.TryGetProperty("pageSize", out _), "Featured response should have pageSize");
    }

    [TestMethod]
    public async Task GetFeaturedProducts_WithCustomPageSize_ReturnsSuccessfulResponse()
    {
        // Arrange
        using var client = _factory.CreateUnauthenticatedClient();

        // Act
        var response = await client.GetAsync("/api/products/featured?page=1&pageSize=5");
        var responseContent = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        var jsonOptions = _jsonOptions;
        var responseData = JsonSerializer.Deserialize<JsonElement>(responseContent, jsonOptions);

        Assert.IsTrue(responseData.TryGetProperty("data", out var data), "Response should have data property");
        Assert.IsTrue(data.TryGetProperty("items", out var items), "Featured response should have items");
        Assert.AreEqual(JsonValueKind.Array, items.ValueKind);
        Assert.IsLessThanOrEqualTo(5, items.GetArrayLength(), "Featured items should not exceed requested pageSize");

        Assert.IsTrue(data.TryGetProperty("pageSize", out var pageSize), "Featured response should have pageSize");
        Assert.AreEqual(5, pageSize.GetInt32());
    }

    #endregion

    #region Get Product By Id Tests

    [TestMethod]
    public async Task GetProductById_WithExistingProduct_ReturnsSuccessfulResponse()
    {
        // Arrange
        using var client = _factory.CreateUnauthenticatedClient();

        // Act
        var response = await client.GetAsync($"/api/products/{ExistingProductId}");

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
    }

    [TestMethod]
    public async Task GetProductById_WithNonexistentProduct_ReturnsNotFound()
    {
        // Arrange
        using var client = _factory.CreateUnauthenticatedClient();
        var nonexistentId = Guid.NewGuid();

        // Act
        var response = await client.GetAsync($"/api/products/{nonexistentId}");

        // Assert
        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
    }

    [TestMethod]
    public async Task GetProductById_ReturnsCorrectProductDetails()
    {
        // Arrange
        using var client = _factory.CreateUnauthenticatedClient();

        // Act
        var response = await client.GetAsync($"/api/products/{ExistingProductId}");
        var responseContent = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        var jsonOptions = _jsonOptions;
        var responseData = JsonSerializer.Deserialize<JsonElement>(responseContent, jsonOptions);
        Assert.IsTrue(responseData.TryGetProperty("data", out var data), "Response should have product data");
        Assert.IsTrue(data.TryGetProperty("id", out _), "Product should have ID");
        Assert.IsTrue(data.TryGetProperty("name", out _), "Product should have name");
    }

    #endregion

    #region Get Product By Slug Tests

    [TestMethod]
    public async Task GetProductBySlug_WithExistingSlug_ReturnsSuccessfulResponse()
    {
        // Arrange
        using var client = _factory.CreateUnauthenticatedClient();

        // Act
        var response = await client.GetAsync("/api/products/slug/integration-product");

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
    }

    [TestMethod]
    public async Task GetProductBySlug_WithNonexistentSlug_ReturnsNotFound()
    {
        // Arrange
        using var client = _factory.CreateUnauthenticatedClient();

        // Act
        var response = await client.GetAsync("/api/products/slug/nonexistent-product");

        // Assert
        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
    }

    #endregion

    #region Create Product Tests

    [TestMethod]
    public async Task CreateProduct_WithAdminAndValidData_ReturnsCreated()
    {
        // Arrange
        using var client = _factory.CreateAdminClient();
        var createProductDto = new
        {
            Name = "New Test Product",
            Slug = "new-test-product-" + Guid.NewGuid().ToString().Substring(0, 8),
            Price = 49.99m,
            CategoryId = "66666666-6666-6666-6666-666666666666",
            StockQuantity = 50
        };

        var content = new StringContent(JsonSerializer.Serialize(createProductDto), Encoding.UTF8, "application/json");

        // Act
        var response = await client.PostAsync("/api/products", content);

        // Assert
        Assert.AreEqual(HttpStatusCode.Created, response.StatusCode);
    }

    [TestMethod]
    public async Task CreateProduct_WithoutAdminRole_ReturnsForbidden()
    {
        // Arrange
        using var client = _factory.CreateAuthenticatedClient(); // Customer role
        var createProductDto = new
        {
            Name = "New Test Product",
            Slug = "forbidden-product-" + Guid.NewGuid().ToString().Substring(0, 8),
            Price = 49.99m,
            StockQuantity = 50
        };

        var content = new StringContent(JsonSerializer.Serialize(createProductDto), Encoding.UTF8, "application/json");

        // Act
        var response = await client.PostAsync("/api/products", content);

        // Assert
        Assert.AreEqual(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [TestMethod]
    public async Task CreateProduct_WithMissingName_ReturnsBadRequest()
    {
        // Arrange
        using var client = _factory.CreateAdminClient();
        var createProductDto = new
        {
            Slug = "test-product",
            Price = 29.99m,
            StockQuantity = 100
        };

        var content = new StringContent(JsonSerializer.Serialize(createProductDto), Encoding.UTF8, "application/json");

        // Act
        var response = await client.PostAsync("/api/products", content);

        // Assert
        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [TestMethod]
    public async Task CreateProduct_WithInvalidPrice_ReturnsBadRequest()
    {
        // Arrange
        using var client = _factory.CreateAdminClient();
        var createProductDto = new
        {
            Name = "Test Product",
            Slug = "test-product-" + Guid.NewGuid().ToString().Substring(0, 8),
            Price = -10m,  // Invalid negative price
            StockQuantity = 100
        };

        var content = new StringContent(JsonSerializer.Serialize(createProductDto), Encoding.UTF8, "application/json");

        // Act
        var response = await client.PostAsync("/api/products", content);

        // Assert
        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
    }

    #endregion

    #region Update Product Tests

    [TestMethod]
    public async Task UpdateProduct_WithAdminAndNonexistentProduct_ReturnsNotFound()
    {
        // Arrange
        using var client = _factory.CreateAdminClient();
        var nonexistentId = Guid.NewGuid();
        var updateProductDto = new
        {
            Name = "Updated Product",
            Slug = "updated-product-" + Guid.NewGuid().ToString().Substring(0, 8),
            Price = 39.99m,
            StockQuantity = 75
        };

        var content = new StringContent(JsonSerializer.Serialize(updateProductDto), Encoding.UTF8, "application/json");

        // Act
        var response = await client.PutAsync($"/api/products/{nonexistentId}", content);

        // Assert
        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
    }

    [TestMethod]
    public async Task UpdateProduct_WithAdminAndValidData_ReturnsOk()
    {
        // Arrange
        using var client = _factory.CreateAdminClient();
        var updateProductDto = new
        {
            Name = "Updated Integration Product",
            Slug = "updated-integration-" + Guid.NewGuid().ToString().Substring(0, 8),
            Price = 19.99m,
            StockQuantity = 150
        };

        var content = new StringContent(JsonSerializer.Serialize(updateProductDto), Encoding.UTF8, "application/json");

        // Act
        var response = await client.PutAsync($"/api/products/{ExistingProductId}", content);

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
    }

    #endregion

    #region Delete Product Tests

    [TestMethod]
    public async Task DeleteProduct_WithAdminAndNonexistentProduct_ReturnsNotFound()
    {
        // Arrange
        using var client = _factory.CreateAdminClient();
        var nonexistentId = Guid.NewGuid();

        // Act
        var response = await client.DeleteAsync($"/api/products/{nonexistentId}");

        // Assert
        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
    }

    [TestMethod]
    public async Task DeleteProduct_WithoutAdminRole_ReturnsForbidden()
    {
        // Arrange
        using var client = _factory.CreateAuthenticatedClient(); // Customer role
        var productId = Guid.NewGuid();

        // Act
        var response = await client.DeleteAsync($"/api/products/{productId}");

        // Assert
        Assert.AreEqual(HttpStatusCode.Forbidden, response.StatusCode);
    }

    #endregion

    #region Response Format Tests

    [TestMethod]
    public async Task GetProducts_ReturnsCorrectResponseFormat()
    {
        // Arrange
        using var client = _factory.CreateUnauthenticatedClient();

        // Act
        var response = await client.GetAsync("/api/products");
        var responseContent = await response.Content.ReadAsStringAsync();

        // Assert
        var jsonOptions = _jsonOptions;
        var responseData = JsonSerializer.Deserialize<JsonElement>(responseContent, jsonOptions);

        Assert.IsTrue(responseData.TryGetProperty("success", out var success) && success.GetBoolean(), "Response should have success=true");
        Assert.IsTrue(responseData.TryGetProperty("data", out var data), "Response should have data property");
        Assert.IsTrue(data.TryGetProperty("items", out _), "Data should have items");
    }

    #endregion
}
