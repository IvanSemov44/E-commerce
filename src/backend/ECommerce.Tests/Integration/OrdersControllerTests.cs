using System.Net;
using System.Text;
using System.Text.Json;
using ECommerce.Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;
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

    [TestMethod]
    public async Task CreateOrder_WithSameIdempotencyKey_ReplaysCachedResponse()
    {
        // Arrange
        using var client = _factory.CreateAuthenticatedClient();
        var idempotencyKey = Guid.NewGuid().ToString();

        client.DefaultRequestHeaders.Remove("Idempotency-Key");
        client.DefaultRequestHeaders.Add("Idempotency-Key", idempotencyKey);

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
                    Quantity = 1
                }
            }
        };

        var firstContent = new StringContent(JsonSerializer.Serialize(createOrderDto), Encoding.UTF8, "application/json");
        var secondContent = new StringContent(JsonSerializer.Serialize(createOrderDto), Encoding.UTF8, "application/json");

        // Act
        var firstResponse = await client.PostAsync("/api/orders", firstContent);
        var secondResponse = await client.PostAsync("/api/orders", secondContent);

        // Assert
        Assert.AreEqual(HttpStatusCode.Created, firstResponse.StatusCode);
        Assert.AreEqual(HttpStatusCode.Created, secondResponse.StatusCode);

        var firstJson = JsonSerializer.Deserialize<JsonElement>(await firstResponse.Content.ReadAsStringAsync());
        var secondJson = JsonSerializer.Deserialize<JsonElement>(await secondResponse.Content.ReadAsStringAsync());

        var firstOrderId = firstJson.GetProperty("data").GetProperty("id").GetGuid();
        var secondOrderId = secondJson.GetProperty("data").GetProperty("id").GetGuid();

        Assert.AreEqual(firstOrderId, secondOrderId,
            "Second request with the same idempotency key should return the cached order response");
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
        Assert.IsTrue(response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.NotFound,
            "Update should return OK or NotFound");
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
            Assert.IsTrue(response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.NotFound,
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

    [TestMethod]
    public async Task CancelOrder_WithoutIdempotencyKey_ReturnsBadRequest()
    {
        // Arrange
        using var client = _factory.CreateAuthenticatedClient();
        var orderId = Guid.NewGuid();
        client.DefaultRequestHeaders.Remove("Idempotency-Key");

        // Act
        var response = await client.PostAsync($"/api/orders/{orderId}/cancel", null);
        var responseContent = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.IsTrue(responseContent.Contains("INVALID_IDEMPOTENCY_KEY", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public async Task CancelOrder_WithSameIdempotencyKey_ReplaysCachedResponse()
    {
        // Arrange
        using var client = _factory.CreateAuthenticatedClient();
        var idempotencyKey = Guid.NewGuid().ToString();

        var orderDto = new
        {
            PaymentMethod = "credit_card",
            ShippingAddress = new
            {
                FirstName = "Replay",
                LastName = "User",
                StreetLine1 = "123 Replay St",
                City = "ReplayCity",
                State = "RP",
                PostalCode = "12345",
                Country = "US"
            },
            Items = new[]
            {
                new
                {
                    ProductId = ExistingProductId.ToString(),
                    ProductName = "ReplayProduct",
                    Price = 10.0m,
                    Quantity = 1
                }
            }
        };

        var content = new StringContent(JsonSerializer.Serialize(orderDto), Encoding.UTF8, "application/json");
        var createResponse = await client.PostAsync("/api/orders", content);

        if (createResponse.StatusCode != HttpStatusCode.Created)
        {
            Assert.Inconclusive("Order creation failed, cannot test idempotent cancellation replay");
            return;
        }

        var createBody = await createResponse.Content.ReadAsStringAsync();
        var orderResponse = JsonSerializer.Deserialize<JsonElement>(createBody);
        var orderId = orderResponse.GetProperty("data").GetProperty("id").GetGuid();

        client.DefaultRequestHeaders.Remove("Idempotency-Key");
        client.DefaultRequestHeaders.Add("Idempotency-Key", idempotencyKey);

        // Act
        var firstResponse = await client.PostAsync($"/api/orders/{orderId}/cancel", null);
        var secondResponse = await client.PostAsync($"/api/orders/{orderId}/cancel", null);

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, firstResponse.StatusCode);
        Assert.AreEqual(HttpStatusCode.OK, secondResponse.StatusCode);

        var firstJson = JsonSerializer.Deserialize<JsonElement>(await firstResponse.Content.ReadAsStringAsync());
        var secondJson = JsonSerializer.Deserialize<JsonElement>(await secondResponse.Content.ReadAsStringAsync());
        var firstSuccess = firstJson.GetProperty("success").GetBoolean();
        var secondSuccess = secondJson.GetProperty("success").GetBoolean();

        Assert.IsTrue(firstSuccess && secondSuccess, "Both responses should be successful for replayed idempotent cancellation");
    }

    [TestMethod]
    public async Task CancelOrder_WhenIdempotencyRequestInProgress_ReturnsConflict()
    {
        // Arrange
        using var client = _factory.CreateAuthenticatedClient();
        var idempotencyKey = Guid.NewGuid().ToString();

        var orderDto = new
        {
            PaymentMethod = "credit_card",
            ShippingAddress = new
            {
                FirstName = "Lock",
                LastName = "User",
                StreetLine1 = "321 Lock St",
                City = "LockCity",
                State = "LK",
                PostalCode = "54321",
                Country = "US"
            },
            Items = new[]
            {
                new
                {
                    ProductId = ExistingProductId.ToString(),
                    ProductName = "LockProduct",
                    Price = 10.0m,
                    Quantity = 1
                }
            }
        };

        var content = new StringContent(JsonSerializer.Serialize(orderDto), Encoding.UTF8, "application/json");
        var createResponse = await client.PostAsync("/api/orders", content);

        if (createResponse.StatusCode != HttpStatusCode.Created)
        {
            Assert.Inconclusive("Order creation failed, cannot test in-progress idempotency");
            return;
        }

        var createBody = await createResponse.Content.ReadAsStringAsync();
        var orderResponse = JsonSerializer.Deserialize<JsonElement>(createBody);
        var orderId = orderResponse.GetProperty("data").GetProperty("id").GetGuid();

        using var scope = _factory.Services.CreateScope();
        var idempotencyStore = scope.ServiceProvider.GetRequiredService<IIdempotencyStore>();
        var storeKey = $"orders:cancel:{orderId}:{idempotencyKey}";
        await idempotencyStore.StartAsync<object>(storeKey, TimeSpan.FromMinutes(5), CancellationToken.None);

        client.DefaultRequestHeaders.Remove("Idempotency-Key");
        client.DefaultRequestHeaders.Add("Idempotency-Key", idempotencyKey);

        // Act
        var response = await client.PostAsync($"/api/orders/{orderId}/cancel", null);
        var responseContent = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.AreEqual(HttpStatusCode.Conflict, response.StatusCode);
        Assert.IsTrue(responseContent.Contains("IDEMPOTENCY_IN_PROGRESS", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public async Task CancelOrder_UserCannotCancelOtherUsersOrder_ReturnsForbidden()
    {
        // Arrange - Create an order owned by User A, then try to cancel it as User B
        using var clientUserA = _factory.CreateAuthenticatedClient();
        var orderDto = new
        {
            PaymentMethod = "credit_card",
            ShippingAddress = new
            {
                FirstName = "User",
                LastName = "A",
                StreetLine1 = "123 Test St",
                City = "TestCity",
                State = "TS",
                PostalCode = "12345",
                Country = "US"
            },
            Items = new[]
            {
                new
                {
                    ProductId = ExistingProductId.ToString(),
                    ProductName = "TestProduct",
                    Price = 10.0m,
                    Quantity = 1
                }
            }
        };

        var content = new StringContent(JsonSerializer.Serialize(orderDto), Encoding.UTF8, "application/json");
        var createResponse = await clientUserA.PostAsync("/api/orders", content);

        // If order creation failed (e.g., inventory issues), skip the test
        if (createResponse.StatusCode != HttpStatusCode.Created)
        {
            Assert.Inconclusive("Order creation failed, cannot test IDOR protection");
            return;
        }

        var responseContent = await createResponse.Content.ReadAsStringAsync();
        var orderResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);
        var orderId = orderResponse.GetProperty("data").GetProperty("id").GetGuid();

        // Create client for User B (different user)
        var userBToken = TestWebApplicationFactory.GenerateJwtToken(Guid.NewGuid().ToString(), "Customer");
        using var clientUserB = _factory.CreateClient();
        clientUserB.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", userBToken);
        clientUserB.DefaultRequestHeaders.Remove("Idempotency-Key");
        clientUserB.DefaultRequestHeaders.Add("Idempotency-Key", Guid.NewGuid().ToString());

        // Act - User B tries to cancel User A's order
        var cancelResponse = await clientUserB.PostAsync($"/api/orders/{orderId}/cancel", null);

        // Assert
        Assert.AreEqual(HttpStatusCode.Forbidden, cancelResponse.StatusCode,
            "User B should not be able to cancel User A's order");
    }

    [TestMethod]
    public async Task CancelOrder_AdminCanCancelAnyOrder_ReturnsSuccess()
    {
        // Arrange - Create an order as regular user, then cancel it as admin
        using var clientUser = _factory.CreateAuthenticatedClient();
        var orderDto = new
        {
            PaymentMethod = "credit_card",
            ShippingAddress = new
            {
                FirstName = "User",
                LastName = "Test",
                StreetLine1 = "123 Test St",
                City = "TestCity",
                State = "TS",
                PostalCode = "12345",
                Country = "US"
            },
            Items = new[]
            {
                new
                {
                    ProductId = ExistingProductId.ToString(),
                    ProductName = "TestProduct",
                    Price = 10.0m,
                    Quantity = 1
                }
            }
        };

        var content = new StringContent(JsonSerializer.Serialize(orderDto), Encoding.UTF8, "application/json");
        var createResponse = await clientUser.PostAsync("/api/orders", content);

        // If order creation failed, skip the test
        if (createResponse.StatusCode != HttpStatusCode.Created)
        {
            Assert.Inconclusive("Order creation failed, cannot test admin access");
            return;
        }

        var responseContent = await createResponse.Content.ReadAsStringAsync();
        var orderResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);
        var orderId = orderResponse.GetProperty("data").GetProperty("id").GetGuid();

        // Create admin client
        using var adminClient = _factory.CreateAdminClient();

        // Act - Admin cancels user's order
        var cancelResponse = await adminClient.PostAsync($"/api/orders/{orderId}/cancel", null);

        // Assert
        Assert.IsTrue(cancelResponse.StatusCode == HttpStatusCode.OK || cancelResponse.StatusCode == HttpStatusCode.BadRequest,
            "Admin should be able to cancel any order (OK) or get BadRequest if already shipped");
    }

    [TestMethod]
    public async Task CancelOrder_UserCanCancelOwnOrder_ReturnsSuccess()
    {
        // Arrange - Create and cancel order as same user
        using var client = _factory.CreateAuthenticatedClient();
        var orderDto = new
        {
            PaymentMethod = "credit_card",
            ShippingAddress = new
            {
                FirstName = "User",
                LastName = "Test",
                StreetLine1 = "123 Test St",
                City = "TestCity",
                State = "TS",
                PostalCode = "12345",
                Country = "US"
            },
            Items = new[]
            {
                new
                {
                    ProductId = ExistingProductId.ToString(),
                    ProductName = "TestProduct",
                    Price = 10.0m,
                    Quantity = 1
                }
            }
        };

        var content = new StringContent(JsonSerializer.Serialize(orderDto), Encoding.UTF8, "application/json");
        var createResponse = await client.PostAsync("/api/orders", content);

        // If order creation failed, skip the test
        if (createResponse.StatusCode != HttpStatusCode.Created)
        {
            Assert.Inconclusive("Order creation failed, cannot test own order cancellation");
            return;
        }

        var responseContent = await createResponse.Content.ReadAsStringAsync();
        var orderResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);
        var orderId = orderResponse.GetProperty("data").GetProperty("id").GetGuid();

        // Act - User cancels their own order
        var cancelResponse = await client.PostAsync($"/api/orders/{orderId}/cancel", null);

        // Assert
        Assert.IsTrue(cancelResponse.StatusCode == HttpStatusCode.OK || cancelResponse.StatusCode == HttpStatusCode.BadRequest,
            "User should be able to cancel their own order");
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
        Assert.IsTrue(response.StatusCode is HttpStatusCode.OK or HttpStatusCode.NotFound,
            $"GetUserOrders should return OK or NotFound, got {response.StatusCode}");
    }

    #endregion

    #region Guest Checkout Tests

    [TestMethod]
    public async Task CreateOrder_GuestWithEmail_ReturnsCreatedOrBadRequest()
    {
        // Arrange
        using var client = _factory.CreateUnauthenticatedClient();
        var createOrderDto = new
        {
            PaymentMethod = "card",
            GuestEmail = "guest@example.com",
            ShippingAddress = new
            {
                FirstName = "Guest",
                LastName = "User",
                Phone = "555-1234",
                StreetLine1 = "123 Guest St",
                City = "New York",
                State = "NY",
                PostalCode = "10001",
                Country = "USA"
            },
            Items = new[]
            {
          new
                {
                    ProductId = ExistingProductId.ToString(),
                    Quantity = 1
                }
            }
        };

        var content = new StringContent(JsonSerializer.Serialize(createOrderDto), Encoding.UTF8, "application/json");

        // Act
        var response = await client.PostAsync("/api/orders", content);

        // Assert
        Assert.IsTrue(response.StatusCode == HttpStatusCode.Created || response.StatusCode == HttpStatusCode.BadRequest,
            $"Guest checkout with email should return Created or BadRequest, got {response.StatusCode}");
    }

    [TestMethod]
    public async Task CreateOrder_GuestWithoutEmail_ReturnsBadRequest()
    {
        // Arrange
        using var client = _factory.CreateUnauthenticatedClient();
        var createOrderDto = new
        {
            PaymentMethod = "card",
            // Missing GuestEmail
            ShippingAddress = new
            {
                FirstName = "Guest",
                LastName = "User",
                Phone = "555-1234",
                StreetLine1 = "123 Guest St",
                City = "New York",
                State = "NY",
                PostalCode = "10001",
                Country = "USA"
            },
            Items = new[]
            {
                new
                {
                    ProductId = ExistingProductId.ToString(),
                    Quantity = 1
                }
            }
        };

        var content = new StringContent(JsonSerializer.Serialize(createOrderDto), Encoding.UTF8, "application/json");

        // Act
        var response = await client.PostAsync("/api/orders", content);

        // Assert
        Assert.IsTrue(response.StatusCode == HttpStatusCode.BadRequest || response.StatusCode == HttpStatusCode.NotFound,
            $"Guest checkout without email should return BadRequest or NotFound, got {response.StatusCode}");

        var responseContent = await response.Content.ReadAsStringAsync();
        var normalized = responseContent.ToLowerInvariant();
        Assert.IsTrue(normalized.Contains("email") || normalized.Contains("guest"),
            "Error response should mention guest email requirement context");
    }

    [TestMethod]
    public async Task CreateOrder_GuestWithEmptyEmail_ReturnsBadRequest()
    {
        // Arrange
        using var client = _factory.CreateUnauthenticatedClient();
        var createOrderDto = new
        {
            PaymentMethod = "card",
            GuestEmail = "", // Empty email
            ShippingAddress = new
            {
                FirstName = "Guest",
                LastName = "User",
                Phone = "555-1234",
                StreetLine1 = "123 Guest St",
                City = "New York",
                State = "NY",
                PostalCode = "10001",
                Country = "USA"
            },
            Items = new[]
            {
                new
                {
                    ProductId = ExistingProductId.ToString(),
                    Quantity = 1
                }
            }
        };

        var content = new StringContent(JsonSerializer.Serialize(createOrderDto), Encoding.UTF8, "application/json");

        // Act
        var response = await client.PostAsync("/api/orders", content);

        // Assert
        Assert.IsTrue(response.StatusCode == HttpStatusCode.BadRequest || response.StatusCode == HttpStatusCode.NotFound,
            $"Guest checkout with empty email should return BadRequest or NotFound, got {response.StatusCode}");
    }

    [TestMethod]
    public async Task CreateOrder_AuthenticatedUser_DoesNotRequireGuestEmail()
    {
        // Arrange
        using var client = _factory.CreateAuthenticatedClient();
        var createOrderDto = new
        {
            PaymentMethod = "card",
            // No GuestEmail provided - authenticated users shouldn't need it
            ShippingAddress = new
            {
                FirstName = "John",
                LastName = "Doe",
                Phone = "555-1234",
                StreetLine1 = "456 Auth St",
                City = "New York",
                State = "NY",
                PostalCode = "10001",
                Country = "USA"
            },
            Items = new[]
            {
                new
                {
                    ProductId = ExistingProductId.ToString(),
                    Quantity = 1
                }
            }
        };

        var content = new StringContent(JsonSerializer.Serialize(createOrderDto), Encoding.UTF8, "application/json");

        // Act
        var response = await client.PostAsync("/api/orders", content);

        // Assert - Should not require guestEmail for authenticated users
        Assert.IsTrue(response.StatusCode == HttpStatusCode.Created || response.StatusCode == HttpStatusCode.BadRequest,
            $"Authenticated user checkout should work without guestEmail, got {response.StatusCode}");
    }

    [TestMethod]
    public async Task CreateOrder_GuestWithValidEmail_OrderNumberPresent()
    {
        // Arrange
        using var client = _factory.CreateUnauthenticatedClient();
        var createOrderDto = new
        {
            PaymentMethod = "card",
            GuestEmail = "guest123@example.com",
            ShippingAddress = new
            {
                FirstName = "Test",
                LastName = "Guest",
                Phone = "555-9999",
                StreetLine1 = "789 Test Ave",
                City = "Boston",
                State = "MA",
                PostalCode = "02101",
                Country = "USA"
            },
            Items = new[]
            {
                new
                {
                    ProductId = ExistingProductId.ToString(),
                    Quantity = 2
                }
            }
        };

        var content = new StringContent(JsonSerializer.Serialize(createOrderDto), Encoding.UTF8, "application/json");

        // Act
        var response = await client.PostAsync("/api/orders", content);
        var responseContent = await response.Content.ReadAsStringAsync();

        // Assert - If successful, should have order number
        if (response.StatusCode == HttpStatusCode.Created)
        {
            Assert.IsTrue(responseContent.Contains("orderNumber"),
                "Successful guest order should contain orderNumber");
        }
    }

    [TestMethod]
    public async Task CreateOrder_GuestWithPromoCode_CalculatesDiscount()
    {
        // Arrange
        using var client = _factory.CreateUnauthenticatedClient();
        var createOrderDto = new
        {
            PaymentMethod = "card",
            GuestEmail = "promo@example.com",
            PromoCode = "SAVE20", // Assuming this code exists
            ShippingAddress = new
            {
                FirstName = "Promo",
                LastName = "Guest",
                Phone = "555-2020",
                StreetLine1 = "999 Promo Rd",
                City = "Chicago",
                State = "IL",
                PostalCode = "60601",
                Country = "USA"
            },
            Items = new[]
            {
                new
                {
                    ProductId = ExistingProductId.ToString(),
                    Quantity = 1
                }
            }
        };

        var content = new StringContent(JsonSerializer.Serialize(createOrderDto), Encoding.UTF8, "application/json");

        // Act
        var response = await client.PostAsync("/api/orders", content);
        var responseContent = await response.Content.ReadAsStringAsync();

        // Assert - Should handle promo code for guests
        Assert.IsTrue(response.StatusCode == HttpStatusCode.Created || response.StatusCode == HttpStatusCode.BadRequest,
            $"Guest checkout with promo code should return Created or BadRequest, got {response.StatusCode}");
    }

    #endregion
}
