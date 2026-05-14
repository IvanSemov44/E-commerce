using System.Net;
using System.Text;
using System.Text.Json;
using ECommerce.SharedKernel.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ECommerce.Tests.Integration;

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

    // ── POST /api/payments/process ────────────────────────────────────────────

    [TestMethod]
    public async Task ProcessPayment_WithValidData_ReturnsOk()
    {
        using var client = _factory.CreateAuthenticatedClient();
        var orderId = Guid.Parse(ConditionalTestAuthHandler.TestOrderId);
        var content = Json(new { OrderId = orderId, PaymentMethod = "credit_card", Amount = 100.00m, CardToken = "tok_visa" });
        var response = await client.PostAsync("/api/payments/process", content);
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
    }

    [TestMethod]
    public async Task ProcessPayment_WithMissingOrderId_ReturnsBadRequest()
    {
        using var client = _factory.CreateAuthenticatedClient();
        var response = await client.PostAsync("/api/payments/process",
            Json(new { PaymentMethod = "credit_card", Amount = 100.00m, CardToken = "tok_visa" }));
        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [TestMethod]
    public async Task ProcessPayment_WithMissingPaymentMethod_ReturnsBadRequest()
    {
        using var client = _factory.CreateAuthenticatedClient();
        var response = await client.PostAsync("/api/payments/process",
            Json(new { OrderId = Guid.NewGuid(), Amount = 100.00m, CardToken = "tok_visa" }));
        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [TestMethod]
    public async Task ProcessPayment_WithInvalidAmount_ReturnsBadRequest()
    {
        using var client = _factory.CreateAuthenticatedClient();
        var response = await client.PostAsync("/api/payments/process",
            Json(new { OrderId = Guid.NewGuid(), PaymentMethod = "credit_card", Amount = -50.00m, CardToken = "tok_visa" }));
        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [TestMethod]
    public async Task ProcessPayment_WithZeroAmount_ReturnsBadRequest()
    {
        using var client = _factory.CreateAuthenticatedClient();
        var response = await client.PostAsync("/api/payments/process",
            Json(new { OrderId = Guid.NewGuid(), PaymentMethod = "credit_card", Amount = 0.00m, CardToken = "tok_visa" }));
        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [TestMethod]
    public async Task ProcessPayment_WithCreditCard_ReturnsOk()
    {
        using var client = _factory.CreateAuthenticatedClient();
        var orderId = Guid.Parse(ConditionalTestAuthHandler.TestOrderId);
        var response = await client.PostAsync("/api/payments/process",
            Json(new { OrderId = orderId, PaymentMethod = "credit_card", Amount = 100.00m, CardToken = "tok_visa" }));
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
    }

    [TestMethod]
    public async Task ProcessPayment_WithPayPal_ReturnsOk()
    {
        using var client = _factory.CreateAuthenticatedClient();
        var orderId = Guid.Parse(ConditionalTestAuthHandler.TestOrderId);
        var response = await client.PostAsync("/api/payments/process",
            Json(new { OrderId = orderId, PaymentMethod = "paypal", Amount = 100.00m, PaypalToken = "EC-test123" }));
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
    }

    [TestMethod]
    public async Task ProcessPayment_ReturnsCorrectResponseShape()
    {
        using var client = _factory.CreateAuthenticatedClient();
        var orderId = Guid.Parse(ConditionalTestAuthHandler.TestOrderId);
        var response = await client.PostAsync("/api/payments/process",
            Json(new { OrderId = orderId, PaymentMethod = "credit_card", Amount = 100.00m, CardToken = "tok_visa" }));
        var json = JsonSerializer.Deserialize<JsonElement>(await response.Content.ReadAsStringAsync(), _jsonOptions);
        Assert.IsTrue(json.TryGetProperty("success", out _));
        Assert.IsTrue(json.TryGetProperty("data", out _));
    }

    [TestMethod]
    public async Task ProcessPayment_WithSameIdempotencyKey_ReplaysCachedResponse()
    {
        using var client = _factory.CreateAuthenticatedClient();
        var orderId = Guid.Parse(ConditionalTestAuthHandler.TestOrderId);
        var key = Guid.NewGuid().ToString();
        client.DefaultRequestHeaders.Remove("Idempotency-Key");
        client.DefaultRequestHeaders.Add("Idempotency-Key", key);

        var dto = new { OrderId = orderId, PaymentMethod = "credit_card", Amount = 100.00m, CardToken = "tok_visa" };
        var first = await client.PostAsync("/api/payments/process", Json(dto));
        var second = await client.PostAsync("/api/payments/process", Json(dto));

        Assert.AreEqual(HttpStatusCode.OK, first.StatusCode);
        Assert.AreEqual(HttpStatusCode.OK, second.StatusCode);

        var firstId = JsonSerializer.Deserialize<JsonElement>(await first.Content.ReadAsStringAsync())
            .GetProperty("data").GetProperty("paymentIntentId").GetString();
        var secondId = JsonSerializer.Deserialize<JsonElement>(await second.Content.ReadAsStringAsync())
            .GetProperty("data").GetProperty("paymentIntentId").GetString();

        Assert.AreEqual(firstId, secondId);
    }

    // ── GET /api/payments/{orderId} ───────────────────────────────────────────

    [TestMethod]
    public async Task GetPaymentStatus_WithNonexistentOrderId_ReturnsNotFound()
    {
        using var client = _factory.CreateAuthenticatedClient();
        var response = await client.GetAsync($"/api/payments/{Guid.NewGuid()}");
        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
    }

    [TestMethod]
    public async Task GetPaymentDetails_UserCannotAccessOtherUsersPayments_ReturnsForbidden()
    {
        using var clientUserA = _factory.CreateAuthenticatedClient();
        var createResponse = await clientUserA.PostAsync("/api/orders", Json(new
        {
            PaymentMethod = "credit_card",
            ShippingAddress = new { FirstName = "User", LastName = "A", StreetLine1 = "123 Test St", City = "TestCity", State = "TS", PostalCode = "12345", Country = "US" },
            Items = new[] { new { ProductId = "22222222-2222-2222-2222-222222222222", ProductName = "TestProduct", Price = 10.0m, Quantity = 1 } }
        }));

        if (createResponse.StatusCode != HttpStatusCode.Created)
        {
            Assert.Inconclusive("Order creation failed, cannot test IDOR protection");
            return;
        }

        var orderId = JsonSerializer.Deserialize<JsonElement>(await createResponse.Content.ReadAsStringAsync())
            .GetProperty("data").GetProperty("id").GetGuid();

        var userBToken = TestWebApplicationFactory.GenerateJwtToken(Guid.NewGuid().ToString(), "Customer");
        using var clientUserB = _factory.CreateClient();
        clientUserB.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", userBToken);

        var response = await clientUserB.GetAsync($"/api/payments/{orderId}");
        Assert.AreEqual(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [TestMethod]
    public async Task GetPaymentDetails_AdminCanAccessAnyPayment_ReturnsSuccessOrNotFound()
    {
        using var clientUser = _factory.CreateAuthenticatedClient();
        var createResponse = await clientUser.PostAsync("/api/orders", Json(new
        {
            PaymentMethod = "credit_card",
            ShippingAddress = new { FirstName = "User", LastName = "Test", StreetLine1 = "123 Test St", City = "TestCity", State = "TS", PostalCode = "12345", Country = "US" },
            Items = new[] { new { ProductId = "22222222-2222-2222-2222-222222222222", ProductName = "TestProduct", Price = 10.0m, Quantity = 1 } }
        }));

        if (createResponse.StatusCode != HttpStatusCode.Created)
        {
            Assert.Inconclusive("Order creation failed, cannot test admin access");
            return;
        }

        var orderId = JsonSerializer.Deserialize<JsonElement>(await createResponse.Content.ReadAsStringAsync())
            .GetProperty("data").GetProperty("id").GetGuid();

        using var adminClient = _factory.CreateAdminClient();
        var response = await adminClient.GetAsync($"/api/payments/{orderId}");
        Assert.IsTrue(response.StatusCode is HttpStatusCode.OK or HttpStatusCode.NotFound);
    }

    [TestMethod]
    public async Task GetPaymentDetails_UserCanAccessOwnPayment_ReturnsSuccessOrNotFound()
    {
        using var client = _factory.CreateAuthenticatedClient();
        var createResponse = await client.PostAsync("/api/orders", Json(new
        {
            PaymentMethod = "credit_card",
            ShippingAddress = new { FirstName = "User", LastName = "Test", StreetLine1 = "123 Test St", City = "TestCity", State = "TS", PostalCode = "12345", Country = "US" },
            Items = new[] { new { ProductId = "22222222-2222-2222-2222-222222222222", ProductName = "TestProduct", Price = 10.0m, Quantity = 1 } }
        }));

        if (createResponse.StatusCode != HttpStatusCode.Created)
        {
            Assert.Inconclusive("Order creation failed, cannot test own payment access");
            return;
        }

        var orderId = JsonSerializer.Deserialize<JsonElement>(await createResponse.Content.ReadAsStringAsync())
            .GetProperty("data").GetProperty("id").GetGuid();

        var response = await client.GetAsync($"/api/payments/{orderId}");
        Assert.IsTrue(response.StatusCode is HttpStatusCode.OK or HttpStatusCode.NotFound);
    }

    // ── POST /api/payments/{orderId}/refund ───────────────────────────────────

    [TestMethod]
    public async Task RefundPayment_WithNegativeAmount_ReturnsBadRequest()
    {
        using var client = _factory.CreateAdminClient();
        var orderId = Guid.Parse(ConditionalTestAuthHandler.TestOrderId);
        var response = await client.PostAsync($"/api/payments/{orderId}/refund",
            Json(new { Amount = -50.00m, Reason = "Partial refund for one item" }));
        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [TestMethod]
    public async Task RefundPayment_WithoutIdempotencyKey_ReturnsBadRequest()
    {
        using var client = _factory.CreateAdminClient();
        client.DefaultRequestHeaders.Remove("Idempotency-Key");
        var orderId = Guid.Parse(ConditionalTestAuthHandler.TestOrderId);
        var response = await client.PostAsync($"/api/payments/{orderId}/refund",
            Json(new { Reason = "Missing key validation" }));
        var body = await response.Content.ReadAsStringAsync();
        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.IsTrue(body.Contains("INVALID_IDEMPOTENCY_KEY", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public async Task RefundPayment_WithSameIdempotencyKey_ReplaysCachedResponse()
    {
        using var client = _factory.CreateAdminClient();
        var orderId = Guid.Parse(ConditionalTestAuthHandler.TestOrderId);

        var processResponse = await client.PostAsync("/api/payments/process",
            Json(new { OrderId = orderId, PaymentMethod = "credit_card", Amount = 100.00m, CardToken = "tok_visa" }));
        Assert.AreEqual(HttpStatusCode.OK, processResponse.StatusCode, "Payment must succeed before refund test.");

        var key = Guid.NewGuid().ToString();
        client.DefaultRequestHeaders.Remove("Idempotency-Key");
        client.DefaultRequestHeaders.Add("Idempotency-Key", key);

        var first = await client.PostAsync($"/api/payments/{orderId}/refund", Json(new { Reason = "Idempotent replay test" }));
        var second = await client.PostAsync($"/api/payments/{orderId}/refund", Json(new { Reason = "Idempotent replay test" }));

        Assert.AreEqual(HttpStatusCode.OK, first.StatusCode);
        Assert.AreEqual(HttpStatusCode.OK, second.StatusCode);

        var firstRefundId = JsonSerializer.Deserialize<JsonElement>(await first.Content.ReadAsStringAsync())
            .GetProperty("data").GetProperty("refundId").GetString();
        var secondRefundId = JsonSerializer.Deserialize<JsonElement>(await second.Content.ReadAsStringAsync())
            .GetProperty("data").GetProperty("refundId").GetString();

        Assert.AreEqual(firstRefundId, secondRefundId);
    }

    [TestMethod]
    public async Task RefundPayment_WhenIdempotencyRequestInProgress_ReturnsConflict()
    {
        using var client = _factory.CreateAdminClient();
        var orderId = Guid.Parse(ConditionalTestAuthHandler.TestOrderId);
        var key = Guid.NewGuid().ToString();

        using var scope = _factory.Services.CreateScope();
        var store = scope.ServiceProvider.GetRequiredService<IIdempotencyStore>();
        await store.StartAsync<object>($"payments:refund:{orderId}:{key}", TimeSpan.FromMinutes(5), CancellationToken.None);

        client.DefaultRequestHeaders.Remove("Idempotency-Key");
        client.DefaultRequestHeaders.Add("Idempotency-Key", key);

        var response = await client.PostAsync($"/api/payments/{orderId}/refund", Json(new { Reason = "In progress conflict" }));
        var body = await response.Content.ReadAsStringAsync();

        Assert.AreEqual(HttpStatusCode.Conflict, response.StatusCode);
        Assert.IsTrue(body.Contains("IDEMPOTENCY_IN_PROGRESS", StringComparison.OrdinalIgnoreCase));
    }

    // ── POST /api/payments/webhook ────────────────────────────────────────────

    [TestMethod]
    public async Task ProcessPaymentWebhook_AllowsAnonymousAccess()
    {
        using var client = _factory.CreateUnauthenticatedClient();
        var response = await client.PostAsync("/api/payments/webhook",
            Json(new { EventType = "charge.succeeded", PaymentIntentId = "pi_test_123" }));
        Assert.AreNotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [TestMethod]
    public async Task ProcessPaymentWebhook_WithValidPayload_ReturnsSuccessful()
    {
        using var client = _factory.CreateUnauthenticatedClient();
        var response = await client.PostAsync("/api/payments/webhook",
            Json(new { EventType = "charge.succeeded", PaymentIntentId = "pi_test_123", Amount = 10000, Status = "succeeded" }));
        Assert.IsTrue(response.StatusCode is HttpStatusCode.OK or HttpStatusCode.NoContent);
    }

    private static StringContent Json(object dto) =>
        new(JsonSerializer.Serialize(dto), Encoding.UTF8, "application/json");
}
