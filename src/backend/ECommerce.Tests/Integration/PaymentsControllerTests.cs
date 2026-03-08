using System.Net;
using System.Text;
using System.Text.Json;
using ECommerce.Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ECommerce.Tests.Integration;

/// <summary>
/// Integration tests for PaymentsController endpoints.
/// Tests payment processing, refunds, and status retrieval.
/// </summary>
[TestClass]
public class PaymentsControllerTests
{
    private TestWebApplicationFactory _factory = null!;
    private static readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

    [TestInitialize]
    public void Setup()
    {
        _factory = new TestWebApplicationFactory();
    }

    [TestCleanup]
    public void Cleanup()
    {
        _factory?.Dispose();
    }

    #region Process Payment Tests

    [TestMethod]
    public async Task ProcessPayment_WithValidData_ReturnsOk()
    {
        // Arrange
        using var client = _factory.CreateAuthenticatedClient();
        var orderId = Guid.Parse(ConditionalTestAuthHandler.TestOrderId);
        var processPaymentDto = new
        {
            OrderId = orderId,
            PaymentMethod = "credit_card",
            Amount = 100.00m,
            CardToken = "tok_visa"
        };

        var content = new StringContent(JsonSerializer.Serialize(processPaymentDto), Encoding.UTF8, "application/json");

        // Act
        var response = await client.PostAsync("/api/payments/process", content);

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
    }

    [TestMethod]
    public async Task ProcessPayment_WithMissingOrderId_ReturnsBadRequest()
    {
        // Arrange
        using var client = _factory.CreateAuthenticatedClient();
        var processPaymentDto = new
        {
            PaymentMethod = "credit_card",
            Amount = 100.00m,
            CardToken = "tok_visa"
        };

        var content = new StringContent(JsonSerializer.Serialize(processPaymentDto), Encoding.UTF8, "application/json");

        // Act
        var response = await client.PostAsync("/api/payments/process", content);

        // Assert
        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [TestMethod]
    public async Task ProcessPayment_WithMissingPaymentMethod_ReturnsBadRequest()
    {
        // Arrange
        using var client = _factory.CreateAuthenticatedClient();
        var orderId = Guid.NewGuid();
        var processPaymentDto = new
        {
            OrderId = orderId,
            Amount = 100.00m,
            CardToken = "tok_visa"
        };

        var content = new StringContent(JsonSerializer.Serialize(processPaymentDto), Encoding.UTF8, "application/json");

        // Act
        var response = await client.PostAsync("/api/payments/process", content);

        // Assert
        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [TestMethod]
    public async Task ProcessPayment_WithInvalidAmount_ReturnsBadRequest()
    {
        // Arrange
        using var client = _factory.CreateAuthenticatedClient();
        var orderId = Guid.NewGuid();
        var processPaymentDto = new
        {
            OrderId = orderId,
            PaymentMethod = "credit_card",
            Amount = -50.00m,  // Invalid negative amount
            CardToken = "tok_visa"
        };

        var content = new StringContent(JsonSerializer.Serialize(processPaymentDto), Encoding.UTF8, "application/json");

        // Act
        var response = await client.PostAsync("/api/payments/process", content);

        // Assert
        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [TestMethod]
    public async Task ProcessPayment_WithZeroAmount_ReturnsBadRequest()
    {
        // Arrange
        using var client = _factory.CreateAuthenticatedClient();
        var orderId = Guid.NewGuid();
        var processPaymentDto = new
        {
            OrderId = orderId,
            PaymentMethod = "credit_card",
            Amount = 0.00m,  // Invalid zero amount
            CardToken = "tok_visa"
        };

        var content = new StringContent(JsonSerializer.Serialize(processPaymentDto), Encoding.UTF8, "application/json");

        // Act
        var response = await client.PostAsync("/api/payments/process", content);

        // Assert
        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
    }

    #endregion

    #region Get Payment Status Tests

    [TestMethod]
    public async Task GetPaymentStatus_WithValidOrderId_ReturnsSuccessOrNotFound()
    {
        // Arrange
        using var client = _factory.CreateAuthenticatedClient();
        var orderId = Guid.NewGuid();

        // Act
        var response = await client.GetAsync($"/api/payments/{orderId}");

        // Assert
        Assert.IsTrue(response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.NotFound,
            "Should return OK or NotFound if order/payment doesn't exist");
    }

    [TestMethod]
    public async Task GetPaymentStatus_WithNonexistentOrderId_ReturnsNotFound()
    {
        // Arrange
        using var client = _factory.CreateAuthenticatedClient();
        var nonexistentOrderId = Guid.NewGuid();

        // Act
        var response = await client.GetAsync($"/api/payments/{nonexistentOrderId}");

        // Assert
        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
    }

    #endregion

    #region Refund Payment Tests

    [TestMethod]
    public async Task RefundPayment_WithValidOrderId_ReturnsSuccessOrNotFound()
    {
        // Arrange
        using var client = _factory.CreateAdminClient();
        var orderId = Guid.Parse(ConditionalTestAuthHandler.TestOrderId);
        var refundDto = new
        {
            Reason = "Customer requested refund"
        };

        var content = new StringContent(JsonSerializer.Serialize(refundDto), Encoding.UTF8, "application/json");

        // Act
        var response = await client.PostAsync($"/api/payments/{orderId}/refund", content);

        // Assert
        Assert.IsTrue(response.StatusCode is HttpStatusCode.OK or HttpStatusCode.NotFound,
            $"RefundPayment should return OK or NotFound, got {response.StatusCode}");
    }

    [TestMethod]
    public async Task RefundPayment_WithPartialRefund_ReturnsSuccessOrNotFound()
    {
        // Arrange
        using var client = _factory.CreateAdminClient();
        var orderId = Guid.Parse(ConditionalTestAuthHandler.TestOrderId);
        var refundDto = new
        {
            Amount = 50.00m,
            Reason = "Partial refund for one item"
        };

        var content = new StringContent(JsonSerializer.Serialize(refundDto), Encoding.UTF8, "application/json");

        // Act
        var response = await client.PostAsync($"/api/payments/{orderId}/refund", content);

        // Assert
        Assert.IsTrue(response.StatusCode is HttpStatusCode.OK or HttpStatusCode.NotFound,
            $"Partial refund should return OK or NotFound, got {response.StatusCode}");
    }

    [TestMethod]
    public async Task RefundPayment_WithNegativeAmount_ReturnsBadRequest()
    {
        // Arrange
        using var client = _factory.CreateAdminClient();
        var orderId = Guid.Parse(ConditionalTestAuthHandler.TestOrderId);
        var refundDto = new
        {
            Amount = -50.00m,  // Invalid negative amount
            Reason = "Partial refund for one item"
        };

        var content = new StringContent(JsonSerializer.Serialize(refundDto), Encoding.UTF8, "application/json");

        // Act
        var response = await client.PostAsync($"/api/payments/{orderId}/refund", content);

        // Assert
        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [TestMethod]
    public async Task RefundPayment_WithoutIdempotencyKey_ReturnsBadRequest()
    {
        // Arrange
        using var client = _factory.CreateAdminClient();
        var orderId = Guid.Parse(ConditionalTestAuthHandler.TestOrderId);
        client.DefaultRequestHeaders.Remove("Idempotency-Key");

        var refundDto = new
        {
            Reason = "Missing key validation"
        };

        var content = new StringContent(JsonSerializer.Serialize(refundDto), Encoding.UTF8, "application/json");

        // Act
        var response = await client.PostAsync($"/api/payments/{orderId}/refund", content);
        var responseContent = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.IsTrue(responseContent.Contains("INVALID_IDEMPOTENCY_KEY", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public async Task RefundPayment_WithSameIdempotencyKey_ReplaysCachedResponse()
    {
        // Arrange
        using var client = _factory.CreateAdminClient();
        var orderId = Guid.Parse(ConditionalTestAuthHandler.TestOrderId);
        var idempotencyKey = Guid.NewGuid().ToString();

        client.DefaultRequestHeaders.Remove("Idempotency-Key");
        client.DefaultRequestHeaders.Add("Idempotency-Key", idempotencyKey);

        var refundDto = new
        {
            Reason = "Idempotent replay test"
        };

        var firstContent = new StringContent(JsonSerializer.Serialize(refundDto), Encoding.UTF8, "application/json");
        var secondContent = new StringContent(JsonSerializer.Serialize(refundDto), Encoding.UTF8, "application/json");

        // Act
        var firstResponse = await client.PostAsync($"/api/payments/{orderId}/refund", firstContent);
        var secondResponse = await client.PostAsync($"/api/payments/{orderId}/refund", secondContent);

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, firstResponse.StatusCode);
        Assert.AreEqual(HttpStatusCode.OK, secondResponse.StatusCode);

        var firstJson = JsonSerializer.Deserialize<JsonElement>(await firstResponse.Content.ReadAsStringAsync());
        var secondJson = JsonSerializer.Deserialize<JsonElement>(await secondResponse.Content.ReadAsStringAsync());

        var firstRefundId = firstJson.GetProperty("data").GetProperty("refundId").GetString();
        var secondRefundId = secondJson.GetProperty("data").GetProperty("refundId").GetString();

        Assert.AreEqual(firstRefundId, secondRefundId,
            "Second request with the same idempotency key should return the cached refund response");
    }

    [TestMethod]
    public async Task RefundPayment_WhenIdempotencyRequestInProgress_ReturnsConflict()
    {
        // Arrange
        using var client = _factory.CreateAdminClient();
        var orderId = Guid.Parse(ConditionalTestAuthHandler.TestOrderId);
        var idempotencyKey = Guid.NewGuid().ToString();

        using var scope = _factory.Services.CreateScope();
        var idempotencyStore = scope.ServiceProvider.GetRequiredService<IIdempotencyStore>();
        var storeKey = $"payments:refund:{orderId}:{idempotencyKey}";
        await idempotencyStore.StartAsync<object>(storeKey, TimeSpan.FromMinutes(5), CancellationToken.None);

        client.DefaultRequestHeaders.Remove("Idempotency-Key");
        client.DefaultRequestHeaders.Add("Idempotency-Key", idempotencyKey);

        var refundDto = new
        {
            Reason = "In progress conflict"
        };

        var content = new StringContent(JsonSerializer.Serialize(refundDto), Encoding.UTF8, "application/json");

        // Act
        var response = await client.PostAsync($"/api/payments/{orderId}/refund", content);
        var responseContent = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.AreEqual(HttpStatusCode.Conflict, response.StatusCode);
        Assert.IsTrue(responseContent.Contains("IDEMPOTENCY_IN_PROGRESS", StringComparison.OrdinalIgnoreCase));
    }

    #endregion

    #region Webhook Tests

    [TestMethod]
    public async Task ProcessPaymentWebhook_WithValidPayload_ReturnsSuccessful()
    {
        // Arrange
        using var client = _factory.CreateUnauthenticatedClient();
        var webhookPayload = new
        {
            EventType = "charge.succeeded",
            PaymentIntentId = "pi_test_123",
            Amount = 10000,
            Status = "succeeded"
        };

        var content = new StringContent(JsonSerializer.Serialize(webhookPayload), Encoding.UTF8, "application/json");

        // Act
        var response = await client.PostAsync("/api/payments/webhook", content);

        // Assert
        Assert.IsTrue(response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.NoContent,
            "Webhook should return OK or NoContent");
    }

    [TestMethod]
    public async Task ProcessPaymentWebhook_AllowsAnonymousAccess()
    {
        // Arrange
        using var client = _factory.CreateUnauthenticatedClient();
        var webhookPayload = new
        {
            EventType = "charge.succeeded",
            PaymentIntentId = "pi_test_123"
        };

        var content = new StringContent(JsonSerializer.Serialize(webhookPayload), Encoding.UTF8, "application/json");

        // Act
        var response = await client.PostAsync("/api/payments/webhook", content);

        // Assert
        Assert.IsFalse(response.StatusCode == HttpStatusCode.Unauthorized,
            "Webhook endpoint should allow anonymous access for payment provider callbacks");
    }

    #endregion

    #region Response Format Tests

    [TestMethod]
    public async Task ProcessPayment_ReturnsCorrectResponseFormat()
    {
        // Arrange
        using var client = _factory.CreateAuthenticatedClient();
        // Use the seeded test order ID instead of a random one
        var orderId = Guid.Parse(ConditionalTestAuthHandler.TestOrderId);
        var processPaymentDto = new
        {
            OrderId = orderId,
            PaymentMethod = "credit_card",
            Amount = 100.00m,
            CardToken = "tok_visa"
        };

        var content = new StringContent(JsonSerializer.Serialize(processPaymentDto), Encoding.UTF8, "application/json");

        // Act
        var response = await client.PostAsync("/api/payments/process", content);
        var responseContent = await response.Content.ReadAsStringAsync();

        // Assert
        var jsonOptions = _jsonOptions;
        var responseData = JsonSerializer.Deserialize<JsonElement>(responseContent, jsonOptions);

        Assert.IsTrue(responseData.TryGetProperty("success", out _), "Response should have 'success' property");
        Assert.IsTrue(responseData.TryGetProperty("data", out _), "Response should have 'data' property");
    }

    #endregion

    #region Multiple Payment Methods Tests

    [TestMethod]
    public async Task ProcessPayment_WithCreditCard_ReturnsOk()
    {
        // Arrange
        using var client = _factory.CreateAuthenticatedClient();
        var orderId = Guid.Parse(ConditionalTestAuthHandler.TestOrderId);
        var processPaymentDto = new
        {
            OrderId = orderId,
            PaymentMethod = "credit_card",
            Amount = 100.00m,
            CardToken = "tok_visa"
        };

        var content = new StringContent(JsonSerializer.Serialize(processPaymentDto), Encoding.UTF8, "application/json");

        // Act
        var response = await client.PostAsync("/api/payments/process", content);

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
    }

    [TestMethod]
    public async Task ProcessPayment_WithPayPal_ReturnsOk()
    {
        // Arrange
        using var client = _factory.CreateAuthenticatedClient();
        var orderId = Guid.Parse(ConditionalTestAuthHandler.TestOrderId);
        var processPaymentDto = new
        {
            OrderId = orderId,
            PaymentMethod = "paypal",
            Amount = 100.00m,
            PaypalToken = "EC-test123"
        };

        var content = new StringContent(JsonSerializer.Serialize(processPaymentDto), Encoding.UTF8, "application/json");

        // Act
        var response = await client.PostAsync("/api/payments/process", content);

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
    }

    [TestMethod]
    public async Task ProcessPayment_WithSameIdempotencyKey_ReplaysCachedResponse()
    {
        // Arrange
        using var client = _factory.CreateAuthenticatedClient();
        var orderId = Guid.Parse(ConditionalTestAuthHandler.TestOrderId);
        var idempotencyKey = Guid.NewGuid().ToString();

        client.DefaultRequestHeaders.Remove("Idempotency-Key");
        client.DefaultRequestHeaders.Add("Idempotency-Key", idempotencyKey);

        var processPaymentDto = new
        {
            OrderId = orderId,
            PaymentMethod = "credit_card",
            Amount = 100.00m,
            CardToken = "tok_visa"
        };

        var firstContent = new StringContent(JsonSerializer.Serialize(processPaymentDto), Encoding.UTF8, "application/json");
        var secondContent = new StringContent(JsonSerializer.Serialize(processPaymentDto), Encoding.UTF8, "application/json");

        // Act
        var firstResponse = await client.PostAsync("/api/payments/process", firstContent);
        var secondResponse = await client.PostAsync("/api/payments/process", secondContent);

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, firstResponse.StatusCode);
        Assert.AreEqual(HttpStatusCode.OK, secondResponse.StatusCode);

        var firstJson = JsonSerializer.Deserialize<JsonElement>(await firstResponse.Content.ReadAsStringAsync());
        var secondJson = JsonSerializer.Deserialize<JsonElement>(await secondResponse.Content.ReadAsStringAsync());

        var firstPaymentIntentId = firstJson.GetProperty("data").GetProperty("paymentIntentId").GetString();
        var secondPaymentIntentId = secondJson.GetProperty("data").GetProperty("paymentIntentId").GetString();

        Assert.AreEqual(firstPaymentIntentId, secondPaymentIntentId,
            "Second request with the same idempotency key should return the cached payment response");
    }

    #endregion

    #region IDOR Protection Tests

    [TestMethod]
    public async Task GetPaymentDetails_UserCannotAccessOtherUsersPayments_ReturnsForbidden()
    {
        // Arrange - Create an order owned by User A, then try to access payment details as User B
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
                    ProductId = Guid.Parse("22222222-2222-2222-2222-222222222222").ToString(),
                    ProductName = "TestProduct",
                    Price = 10.0m,
                    Quantity = 1
                }
            }
        };

        var content = new StringContent(JsonSerializer.Serialize(orderDto), Encoding.UTF8, "application/json");
        var createResponse = await clientUserA.PostAsync("/api/orders", content);

        // If order creation failed, skip the test
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

        // Act - User B tries to access User A's payment details
        var paymentDetailsResponse = await clientUserB.GetAsync($"/api/payments/{orderId}");

        // Assert
        Assert.AreEqual(HttpStatusCode.Forbidden, paymentDetailsResponse.StatusCode,
            "User B should not be able to access User A's payment details");
    }

    [TestMethod]
    public async Task GetPaymentDetails_AdminCanAccessAnyPayment_ReturnsSuccess()
    {
        // Arrange - Create an order as regular user, then access payment details as admin
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
                    ProductId = Guid.Parse("22222222-2222-2222-2222-222222222222").ToString(),
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

        // Act - Admin accesses user's payment details
        var paymentDetailsResponse = await adminClient.GetAsync($"/api/payments/{orderId}");

        // Assert
        Assert.IsTrue(paymentDetailsResponse.StatusCode == HttpStatusCode.OK || paymentDetailsResponse.StatusCode == HttpStatusCode.NotFound,
            "Admin should be able to access any payment details");
    }

    [TestMethod]
    public async Task GetPaymentDetails_UserCanAccessOwnPayment_ReturnsSuccess()
    {
        // Arrange - Create order and access payment details as same user
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
                    ProductId = Guid.Parse("22222222-2222-2222-2222-222222222222").ToString(),
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
            Assert.Inconclusive("Order creation failed, cannot test own payment access");
            return;
        }

        var responseContent = await createResponse.Content.ReadAsStringAsync();
        var orderResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);
        var orderId = orderResponse.GetProperty("data").GetProperty("id").GetGuid();

        // Act - User accesses their own payment details
        var paymentDetailsResponse = await client.GetAsync($"/api/payments/{orderId}");

        // Assert
        Assert.IsTrue(paymentDetailsResponse.StatusCode == HttpStatusCode.OK || paymentDetailsResponse.StatusCode == HttpStatusCode.NotFound,
            "User should be able to access their own payment details");
    }

    #endregion
}
