using System.Net;
using System.Text;
using System.Text.Json;
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
        Assert.IsTrue(response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.NotFound,
            "Should return OK or NotFound if payment/order doesn't exist");
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
        Assert.IsTrue(response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.NotFound,
            "Partial refund should return OK or NotFound");
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
        var orderId = Guid.NewGuid();
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
        var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
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

    #endregion
}
