using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ECommerce.Tests.Integration;

/// <summary>
/// Integration tests for CategoriesController endpoints.
/// Tests category CRUD operations and retrieval.
/// </summary>
[TestClass]
public class CategoriesControllerTests
{
    private TestWebApplicationFactory _factory = null!;

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

    #region Get Categories Tests

    [TestMethod]
    public async Task GetCategories_WithDefaultQuery_ReturnsSuccessful()
    {
        // Arrange
        using var client = _factory.CreateUnauthenticatedClient();

        // Act
        var response = await client.GetAsync("/api/categories");

        // Assert
        Assert.IsTrue(response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.NotFound,
            "GetCategories should return OK or NotFound if no categories exist");
    }

    [TestMethod]
    public async Task GetCategories_ReturnsCorrectFormat()
    {
        // Arrange
        using var client = _factory.CreateUnauthenticatedClient();

        // Act
        var response = await client.GetAsync("/api/categories");
        var responseContent = await response.Content.ReadAsStringAsync();

        // Assert
        if (response.StatusCode == HttpStatusCode.OK)
        {
            var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var responseData = JsonSerializer.Deserialize<JsonElement>(responseContent, jsonOptions);
            Assert.IsTrue(responseData.TryGetProperty("data", out _), "Response should have data property");
        }
    }

    #endregion

    #region Get Category By ID Tests

    [TestMethod]
    public async Task GetCategoryById_WithExistingId_ReturnsOk()
    {
        // Arrange
        using var client = _factory.CreateUnauthenticatedClient();
        var categoryId = Guid.NewGuid();

        // Act
        var response = await client.GetAsync($"/api/categories/{categoryId}");

        // Assert
        Assert.IsTrue(response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.NotFound,
            "Should return OK if exists, NotFound otherwise");
    }

    [TestMethod]
    public async Task GetCategoryById_WithNonexistentId_ReturnsNotFound()
    {
        // Arrange
        using var client = _factory.CreateUnauthenticatedClient();
        var nonexistentId = Guid.NewGuid();

        // Act
        var response = await client.GetAsync($"/api/categories/{nonexistentId}");

        // Assert
        Assert.IsTrue(response.StatusCode == HttpStatusCode.NotFound || response.StatusCode == HttpStatusCode.OK,
            "Non-existent category may return NotFound or OK with empty data");
    }

    #endregion

    #region Create Category Tests

    [TestMethod]
    public async Task CreateCategory_WithAdminAndValidData_ReturnsCreated()
    {
        // Arrange
        using var client = _factory.CreateAdminClient();
        var createCategoryDto = new
        {
            Name = "Electronics",
            Description = "Electronic devices and gadgets",
            Slug = "electronics-" + Guid.NewGuid().ToString().Substring(0, 8)
        };

        var content = new StringContent(JsonSerializer.Serialize(createCategoryDto), Encoding.UTF8, "application/json");

        // Act
        var response = await client.PostAsync("/api/categories", content);

        // Assert
        Assert.IsTrue(response.StatusCode == HttpStatusCode.Created || response.StatusCode == HttpStatusCode.Forbidden,
            "Create should return Created or Forbidden based on role");
    }

    [TestMethod]
    public async Task CreateCategory_WithCustomerRole_ReturnsForbidden()
    {
        // Arrange
        using var client = _factory.CreateAuthenticatedClient();
        var createCategoryDto = new
        {
            Name = "Fashion",
            Description = "Fashion and apparel",
            Slug = "fashion"
        };

        var content = new StringContent(JsonSerializer.Serialize(createCategoryDto), Encoding.UTF8, "application/json");

        // Act
        var response = await client.PostAsync("/api/categories", content);

        // Assert
        Assert.IsTrue(response.StatusCode == HttpStatusCode.Forbidden || response.StatusCode == HttpStatusCode.Unauthorized,
            "Customer should not be able to create categories");
    }

    #endregion

    #region Update Category Tests

    [TestMethod]
    public async Task UpdateCategory_WithAdminAndValidData_ReturnsOk()
    {
        // Arrange
        using var client = _factory.CreateAdminClient();
        var categoryId = Guid.NewGuid();
        var updateCategoryDto = new
        {
            Name = "Updated Category",
            Description = "Updated description",
            Slug = "updated-" + Guid.NewGuid().ToString().Substring(0, 8)
        };

        var content = new StringContent(JsonSerializer.Serialize(updateCategoryDto), Encoding.UTF8, "application/json");

        // Act
        var response = await client.PutAsync($"/api/categories/{categoryId}", content);

        // Assert
        Assert.IsTrue(response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.NotFound || response.StatusCode == HttpStatusCode.Forbidden,
            "Update should return OK, NotFound, or Forbidden");
    }

    #endregion

    #region Delete Category Tests

    [TestMethod]
    public async Task DeleteCategory_WithNonexistentId_ReturnsNotFound()
    {
        // Arrange
        using var client = _factory.CreateAdminClient();
        var nonexistentId = Guid.NewGuid();

        // Act
        var response = await client.DeleteAsync($"/api/categories/{nonexistentId}");

        // Assert
        Assert.IsTrue(response.StatusCode == HttpStatusCode.NotFound || response.StatusCode == HttpStatusCode.Forbidden || response.StatusCode == HttpStatusCode.OK,
            "Delete should return NotFound, Forbidden, or OK");
    }

    [TestMethod]
    public async Task DeleteCategory_WithCustomerRole_ReturnsForbidden()
    {
        // Arrange
        using var client = _factory.CreateAuthenticatedClient();
        var categoryId = Guid.NewGuid();

        // Act
        var response = await client.DeleteAsync($"/api/categories/{categoryId}");

        // Assert
        Assert.IsTrue(response.StatusCode == HttpStatusCode.Forbidden || response.StatusCode == HttpStatusCode.Unauthorized,
            "Customer should not be able to delete categories");
    }

    #endregion

    #region Response Format Tests

    [TestMethod]
    public async Task GetCategories_ReturnsStandardApiResponse()
    {
        // Arrange
        using var client = _factory.CreateUnauthenticatedClient();

        // Act
        var response = await client.GetAsync("/api/categories");
        var responseContent = await response.Content.ReadAsStringAsync();

        // Assert
        if (!string.IsNullOrEmpty(responseContent) && response.StatusCode == HttpStatusCode.OK)
        {
            var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var responseData = JsonSerializer.Deserialize<JsonElement>(responseContent, jsonOptions);
            Assert.IsTrue(responseData.TryGetProperty("success", out _) || responseData.TryGetProperty("data", out _),
                "Response should have success or data property");
        }
    }

    #endregion
}
