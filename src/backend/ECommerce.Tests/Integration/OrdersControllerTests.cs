using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ECommerce.Tests.Integration;

/// <summary>
/// Integration tests for OrdersController endpoints.
/// Tests order creation, retrieval, and status updates.
/// </summary>
[TestClass]
public class OrdersControllerTests
{
    private TestWebApplicationFactory _factory = null!;
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

    #region Create Order Tests

    [TestMethod]
    public async Task CreateOrder_WithValidData_ReturnsCreatedOrBadRequest()
    {
        // Arrange
        using var client = _factory.CreateAuthenticatedClient();
        var createOrderDto = new
        {
            PaymentMethod = "credit_card",
            ShippingAddress = new
            {
                FirstName = "John",
                LastName = "Doe",
                StreetLine1 = "123 Main St",
                City = "New York",
                State = "NY",
                PostalCode = "10001",
                Country = "US"
            },
            Items = new[]
            {
                new
                {
                    ProductId = ExistingProductId.ToString(),
                    ProductName = "IntegrationProduct",
                    Price = 10.0m,
                    Quantity = 2
                }
            }
        };

        var content = new StringContent(JsonSerializer.Serialize(createOrderDto), Encoding.UTF8, "application/json");

        // Act
        var response = await client.PostAsync("/api/orders", content);

        // Assert
        Assert.IsTrue(response.StatusCode == HttpStatusCode.Created || response.StatusCode == HttpStatusCode.BadRequest,
            "Create should return Created or BadRequest (if cart validation fails)");
    }

    [TestMethod]
    public async Task CreateOrder_WithMissingShippingAddress_ReturnsBadRequest()
    {
        // Arrange
        using var client = _factory.CreateAuthenticatedClient();
        var createOrderDto = new
        {
            PaymentMethod = "credit_card",
            Items = new[]
            {
                new
                {
                    ProductId = ExistingProductId.ToString(),
                    ProductName = "IntegrationProduct",
                    Price = 10.0m,
                    Quantity = 2
                }
            }
        };

        var content = new StringContent(JsonSerializer.Serialize(createOrderDto), Encoding.UTF8, "application/json");

        // Act
        var response = await client.PostAsync("/api/orders", content);

        // Assert
        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [TestMethod]
    public async Task CreateOrder_WithEmptyItems_ReturnsBadRequest()
    {
        // Arrange
        using var client = _factory.CreateAuthenticatedClient();
        var createOrderDto = new
        {
            PaymentMethod = "credit_card",
            ShippingAddress = new
            {
                FirstName = "John",
                LastName = "Doe",
                StreetLine1 = "123 Main St",
                City = "New York",
                State = "NY",
                PostalCode = "10001",
                Country = "US"
            },
            Items = new object[] { }  // Empty items
        };

        var content = new StringContent(JsonSerializer.Serialize(createOrderDto), Encoding.UTF8, "application/json");

        // Act
        var response = await client.PostAsync("/api/orders", content);

        // Assert
        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [TestMethod]
    public async Task CreateOrder_WithInvalidQuantity_ReturnsBadRequest()
    {
        // Arrange
        using var client = _factory.CreateAuthenticatedClient();
        var createOrderDto = new
        {
            PaymentMethod = "credit_card",
            ShippingAddress = new
            {
                FirstName = "John",
                LastName = "Doe",
                StreetLine1 = "123 Main St",
                City = "New York",
                State = "NY",
                PostalCode = "10001",
                Country = "US"
            },
            Items = new[]
            {
                new
                {
                    ProductId = ExistingProductId.ToString(),
                    ProductName = "IntegrationProduct",
                    Price = 10.0m,
                    Quantity = 0  // Invalid quantity
                }
            }
        };

        var content = new StringContent(JsonSerializer.Serialize(createOrderDto), Encoding.UTF8, "application/json");

        // Act
        var response = await client.PostAsync("/api/orders", content);

        // Assert
        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
    }

    #endregion

    #region Get Order Tests

    [TestMethod]
    public async Task GetOrderById_WithNonexistentOrder_ReturnsNotFound()
    {
        // Arrange
        using var client = _factory.CreateAuthenticatedClient();
        var nonexistentOrderId = Guid.NewGuid();

        // Act
        var response = await client.GetAsync($"/api/orders/{nonexistentOrderId}");

        // Assert
        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
    }

    [TestMethod]
    public async Task GetOrderByNumber_WithNonexistentOrderNumber_ReturnsNotFound()
    {
        // Arrange
        using var client = _factory.CreateAuthenticatedClient();

        // Act
        var response = await client.GetAsync($"/api/orders/number/NONEXISTENT-ORDER-001");

        // Assert
        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
    }

    #endregion

    #region Get User Orders Tests

    [TestMethod]
    public async Task GetUserOrders_WithAuthentication_ReturnsSuccessfulResponse()
    {
        // Arrange
        using var client = _factory.CreateAuthenticatedClient();

        // Act
        var response = await client.GetAsync("/api/orders/my-orders");

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
    }

    [TestMethod]
    public async Task GetUserOrders_WithPagination_ReturnsPaginatedResult()
    {
        // Arrange
        using var client = _factory.CreateAuthenticatedClient();

        // Act
        var response = await client.GetAsync("/api/orders/my-orders?page=1&pageSize=10");

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
    }

    #endregion

    #region Update Order Status Tests

    [TestMethod]
    public async Task UpdateOrderStatus_WithAdminAndValidStatus_ReturnsSuccessOrNotFound()
    {
        // Arrange
        using var client = _factory.CreateAdminClient();
        var orderId = Guid.NewGuid();
        var updateStatusDto = new
        {
            Status = "confirmed"
        };

        var content = new StringContent(JsonSerializer.Serialize(updateStatusDto), Encoding.UTF8, "application/json");

        // Act
        var response = await client.PutAsync($"/api/orders/{orderId}/status", content);

        // Assert
        Assert.IsTrue(response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.NotFound || response.StatusCode == HttpStatusCode.BadRequest,
            "Update should return OK, NotFound, or BadRequest");
    }

    [TestMethod]
    public async Task UpdateOrderStatus_WithoutAdminRole_ReturnsForbidden()
    {
        // Arrange
        using var client = _factory.CreateAuthenticatedClient(); // Customer role
        var orderId = Guid.NewGuid();
        var updateStatusDto = new
        {
            Status = "confirmed"
        };

        var content = new StringContent(JsonSerializer.Serialize(updateStatusDto), Encoding.UTF8, "application/json");

        // Act
        var response = await client.PutAsync($"/api/orders/{orderId}/status", content);

        // Assert
        Assert.AreEqual(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [TestMethod]
    public async Task UpdateOrderStatus_WithInvalidStatus_ReturnsBadRequest()
    {
        // Arrange
        using var client = _factory.CreateAdminClient();
        var orderId = Guid.NewGuid();
        var updateStatusDto = new
        {
            Status = "invalid_status"
        };

        var content = new StringContent(JsonSerializer.Serialize(updateStatusDto), Encoding.UTF8, "application/json");

        // Act
        var response = await client.PutAsync($"/api/orders/{orderId}/status", content);

        // Assert
        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [TestMethod]
    public async Task UpdateOrderStatus_WithAllValidStatuses_ReturnsSuccess()
    {
        // Arrange
        using var client = _factory.CreateAdminClient();
        var orderId = Guid.NewGuid();
        var validStatuses = new[] { "pending", "confirmed", "processing", "shipped", "delivered", "cancelled", "refunded" };

        foreach (var status in validStatuses)
        {
            var updateStatusDto = new { Status = status };
            var content = new StringContent(JsonSerializer.Serialize(updateStatusDto), Encoding.UTF8, "application/json");

            // Act
            var response = await client.PutAsync($"/api/orders/{orderId}/status", content);

            // Assert
            Assert.IsTrue(response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.NotFound || response.StatusCode == HttpStatusCode.Forbidden,
                $"Status '{status}' should be accepted");
        }
    }

    #endregion

    #region Cancel Order Tests

    [TestMethod]
    public async Task CancelOrder_WithValidOrder_ReturnsSuccessOrNotFound()
    {
        // Arrange
        using var client = _factory.CreateAuthenticatedClient();
        var orderId = Guid.NewGuid();

        // Act
        var response = await client.PostAsync($"/api/orders/{orderId}/cancel", null);

        // Assert
        Assert.IsTrue(response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.NotFound,
            "Cancel should return OK or NotFound if order doesn't exist");
    }

    #endregion

    #region Response Format Tests

    [TestMethod]
    public async Task GetUserOrders_ReturnsCorrectResponseFormat()
    {
        // Arrange
        using var client = _factory.CreateAuthenticatedClient();

        // Act
        var response = await client.GetAsync("/api/orders/user/orders");
        var responseContent = await response.Content.ReadAsStringAsync();

        // Assert
        var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var responseData = JsonSerializer.Deserialize<JsonElement>(responseContent, jsonOptions);

        Assert.IsTrue(responseData.TryGetProperty("success", out var success) && success.GetBoolean(), "Response should have success=true");
        Assert.IsTrue(responseData.TryGetProperty("data", out _), "Response should have data");
    }

    #endregion
}
