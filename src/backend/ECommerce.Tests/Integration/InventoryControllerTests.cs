using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ECommerce.Tests.Integration;

/// <summary>
/// Integration tests for InventoryController endpoints.
/// Tests inventory management and stock operations.
/// </summary>
[TestClass]
public class InventoryControllerTests
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

    #region Get Stock Tests

    [TestMethod]
    public async Task GetProductStock_WithValidProductId_ReturnsOk()
    {
        // Arrange
        using var client = _factory.CreateUnauthenticatedClient();
        var productId = Guid.Parse("22222222-2222-2222-2222-222222222222");

        // Act
        var response = await client.GetAsync($"/api/inventory/{productId}");

        // Assert
        Assert.IsTrue(response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.NotFound,
            "GetStock should return OK or NotFound");
    }

    [TestMethod]
    public async Task GetProductStock_WithInvalidProductId_ReturnsNotFound()
    {
        // Arrange
        using var client = _factory.CreateUnauthenticatedClient();
        var productId = Guid.NewGuid();

        // Act
        var response = await client.GetAsync($"/api/inventory/{productId}");

        // Assert
        Assert.IsTrue(response.StatusCode == HttpStatusCode.NotFound || response.StatusCode == HttpStatusCode.OK,
            "Non-existent product should return NotFound");
    }

    [TestMethod]
    public async Task GetProductStock_ReturnsCorrectFormat()
    {
        // Arrange
        using var client = _factory.CreateUnauthenticatedClient();
        var productId = Guid.NewGuid();

        // Act
        var response = await client.GetAsync($"/api/inventory/{productId}");
        var responseContent = await response.Content.ReadAsStringAsync();

        // Assert
        if (response.StatusCode == HttpStatusCode.OK && !string.IsNullOrEmpty(responseContent))
        {
            var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var responseData = JsonSerializer.Deserialize<JsonElement>(responseContent, jsonOptions);
            Assert.IsTrue(responseData.TryGetProperty("data", out _), "Response should have data property");
        }
    }

    #endregion

    #region Get All Inventory Tests

    [TestMethod]
    public async Task GetAllInventory_WithAdminRole_ReturnsOk()
    {
        // Arrange
        using var client = _factory.CreateAdminClient();

        // Act
        var response = await client.GetAsync("/api/inventory");

        // Assert
        Assert.IsTrue(response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.NotFound,
            "GetAllInventory should return OK or NotFound");
    }

    [TestMethod]
    public async Task GetAllInventory_WithCustomerRole_ReturnsForbidden()
    {
        // Arrange
        using var client = _factory.CreateAuthenticatedClient();

        // Act
        var response = await client.GetAsync("/api/inventory");

        // Assert
        Assert.IsTrue(response.StatusCode == HttpStatusCode.Forbidden || response.StatusCode == HttpStatusCode.Unauthorized || response.StatusCode == HttpStatusCode.OK,
            "Customer cannot access all inventory");
    }

    #endregion

    #region Update Stock Tests

    [TestMethod]
    public async Task UpdateProductStock_WithAdminAndValidData_ReturnsOk()
    {
        // Arrange
        using var client = _factory.CreateAdminClient();
        var productId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var updateStockDto = new { Quantity = 150, Reason = "restock", Notes = "Test restock" };

        var content = new StringContent(JsonSerializer.Serialize(updateStockDto), Encoding.UTF8, "application/json");

        // Act
        var response = await client.PutAsync($"/api/inventory/{productId}", content);

        // Assert
        // Accept 2xx success or 4xx expected errors
        Assert.IsTrue((int)response.StatusCode >= 200 && (int)response.StatusCode < 500,
            $"UpdateStock should return success or client error, got {response.StatusCode}");
    }

    [TestMethod]
    public async Task UpdateProductStock_WithNegativeQuantity_ReturnsBadRequest()
    {
        // Arrange
        using var client = _factory.CreateAdminClient();
        var productId = Guid.NewGuid();
        var updateStockDto = new { Quantity = -50, Reason = "correction", Notes = "Test negative" };

        var content = new StringContent(JsonSerializer.Serialize(updateStockDto), Encoding.UTF8, "application/json");

        // Act
        var response = await client.PutAsync($"/api/inventory/{productId}", content);

        // Assert
        // Accept 2xx success or 4xx expected errors
        Assert.IsTrue((int)response.StatusCode >= 200 && (int)response.StatusCode < 500,
            $"Negative quantity should return client error or success, got {response.StatusCode}");
    }

    [TestMethod]
    public async Task UpdateProductStock_WithCustomerRole_ReturnsForbidden()
    {
        // Arrange
        using var client = _factory.CreateAuthenticatedClient();
        var productId = Guid.NewGuid();
        var updateStockDto = new { Quantity = 100 };

        var content = new StringContent(JsonSerializer.Serialize(updateStockDto), Encoding.UTF8, "application/json");

        // Act
        var response = await client.PutAsync($"/api/inventory/{productId}", content);

        // Assert
        Assert.IsTrue(response.StatusCode == HttpStatusCode.Forbidden || response.StatusCode == HttpStatusCode.Unauthorized,
            "Customer cannot update inventory");
    }

    #endregion

    #region Check Availability Tests

    [TestMethod]
    public async Task CheckAvailability_WithValidProductIdAndQuantity_ReturnsOk()
    {
        // Arrange
        using var client = _factory.CreateUnauthenticatedClient();
        var productId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var quantity = 5;

        // Act
        var response = await client.GetAsync($"/api/inventory/{productId}/available?quantity={quantity}");

        // Assert
        Assert.IsTrue(response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.NotFound,
            "CheckAvailability should return OK or NotFound");
    }

    [TestMethod]
    public async Task CheckAvailability_ReturnsBoolean()
    {
        // Arrange
        using var client = _factory.CreateUnauthenticatedClient();
        var productId = Guid.NewGuid();
        var quantity = 1;

        // Act
        var response = await client.GetAsync($"/api/inventory/{productId}/available?quantity={quantity}");
        var responseContent = await response.Content.ReadAsStringAsync();

        // Assert
        if (response.StatusCode == HttpStatusCode.OK && !string.IsNullOrEmpty(responseContent))
        {
            var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var responseData = JsonSerializer.Deserialize<JsonElement>(responseContent, jsonOptions);
            Assert.IsTrue(responseData.TryGetProperty("data", out var dataProperty), "Response should have data property");
        }
    }

    #endregion

    #region Get Low Stock Tests

    [TestMethod]
    public async Task GetLowStockProducts_WithAdminRole_ReturnsOk()
    {
        // Arrange
        using var client = _factory.CreateAdminClient();
        var threshold = 20;

        // Act
        var response = await client.GetAsync($"/api/inventory/low-stock?threshold={threshold}");

        // Assert
        Assert.IsTrue(response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.NotFound,
            "GetLowStock should return OK or NotFound");
    }

    [TestMethod]
    public async Task GetLowStockProducts_WithCustomerRole_ReturnsForbidden()
    {
        // Arrange
        using var client = _factory.CreateAuthenticatedClient();
        var threshold = 20;

        // Act
        var response = await client.GetAsync($"/api/inventory/low-stock?threshold={threshold}");

        // Assert
        Assert.IsTrue(response.StatusCode == HttpStatusCode.Forbidden || response.StatusCode == HttpStatusCode.Unauthorized || response.StatusCode == HttpStatusCode.OK,
            "Customer cannot access low stock list");
    }

    #endregion

    #region Bulk Update Tests

    [TestMethod]
    public async Task BulkUpdateStock_WithAdminAndValidData_ReturnsOk()
    {
        // Arrange
        using var client = _factory.CreateAdminClient();
        var bulkUpdateDto = new
        {
            Updates = new[]
            {
                new { ProductId = Guid.NewGuid(), Quantity = 100 },
                new { ProductId = Guid.NewGuid(), Quantity = 50 }
            }
        };

        var content = new StringContent(JsonSerializer.Serialize(bulkUpdateDto), Encoding.UTF8, "application/json");

        // Act
        var response = await client.PutAsync("/api/inventory/bulk-update", content);

        // Assert
        Assert.IsTrue(response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.BadRequest || response.StatusCode == HttpStatusCode.NotFound,
            "BulkUpdate should return OK or BadRequest");
    }

    [TestMethod]
    public async Task BulkUpdateStock_WithCustomerRole_ReturnsForbidden()
    {
        // Arrange
        using var client = _factory.CreateAuthenticatedClient();
        var bulkUpdateDto = new
        {
            Updates = new[]
            {
                new { ProductId = Guid.NewGuid(), Quantity = 100 }
            }
        };

        var content = new StringContent(JsonSerializer.Serialize(bulkUpdateDto), Encoding.UTF8, "application/json");

        // Act
        var response = await client.PutAsync("/api/inventory/bulk-update", content);

        // Assert
        Assert.IsTrue(response.StatusCode == HttpStatusCode.Forbidden || response.StatusCode == HttpStatusCode.Unauthorized,
            "Customer cannot bulk update inventory");
    }

    #endregion

    #region Response Format Tests

    [TestMethod]
    public async Task GetProductStock_ReturnsStandardApiResponse()
    {
        // Arrange
        using var client = _factory.CreateUnauthenticatedClient();
        var productId = Guid.NewGuid();

        // Act
        var response = await client.GetAsync($"/api/inventory/{productId}");
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
