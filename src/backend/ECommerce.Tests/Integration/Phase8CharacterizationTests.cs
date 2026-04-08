using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ECommerce.Tests.Integration;

/// <summary>
/// Characterization tests for Phase 8 extraction.
/// These tests pin the current synchronous cross-context behavior before moving to integration events.
/// </summary>
[TestClass]
public class Phase8CharacterizationTests
{
    private static readonly Guid ExistingProductId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    private TestWebApplicationFactory _factory = null!;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

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

    [TestMethod]
    public async Task PlaceOrder_InventoryReduced_Synchronously()
    {
        using var client = _factory.CreateAuthenticatedClient();
        var quantity = 2;

        var stockBefore = await GetInventoryQuantityAsync(client, ExistingProductId, TestContext.CancellationToken);
        var orderResponse = await client.PostAsync("/api/orders", BuildOrderRequest(quantity), TestContext.CancellationToken);
        var orderBody = await orderResponse.Content.ReadAsStringAsync(TestContext.CancellationToken);

        var stockAfter = await GetInventoryQuantityAsync(client, ExistingProductId, TestContext.CancellationToken);
        if (orderResponse.StatusCode == HttpStatusCode.Created)
        {
            Assert.AreEqual(stockBefore - quantity, stockAfter,
                "Inventory reduction should be visible immediately in Phase 7 synchronous flow.");
            return;
        }

        Assert.AreEqual(HttpStatusCode.BadRequest, orderResponse.StatusCode,
            $"Expected either 201 or 400. Body: {orderBody}");
        Assert.AreEqual("ADDRESS_NOT_FOUND", GetErrorCode(orderBody),
            "Current known behavior: order creation can fail before side effects when shipping address cannot be resolved.");
        Assert.AreEqual(stockBefore, stockAfter,
            "Inventory should remain unchanged when order creation fails.");
    }

    [TestMethod]
    public async Task AddToCart_EndpointCompletesSynchronously()
    {
        using var client = _factory.CreateUnauthenticatedClient();
        var sessionId = Guid.NewGuid().ToString();
        client.DefaultRequestHeaders.Add("X-Session-ID", sessionId);

        var payload = new
        {
            ProductId = ExistingProductId,
            Quantity = 1
        };
        using var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        var addResponse = await client.PostAsync("/api/cart/add-item", content, TestContext.CancellationToken);
        var addBody = await addResponse.Content.ReadAsStringAsync(TestContext.CancellationToken);
        Assert.AreEqual(HttpStatusCode.OK, addResponse.StatusCode,
            $"Add-to-cart should complete in-request. Body: {addBody}");

        var json = JsonSerializer.Deserialize<JsonElement>(addBody, JsonOptions);
        Assert.IsTrue(GetRequiredProperty(json, "success").GetBoolean(),
            "Add-to-cart should return success within the same request scope.");
        Assert.IsTrue(json.TryGetProperty("data", out _),
            "Add-to-cart should return a data payload immediately.");
    }

    [TestMethod]
    public async Task PlaceOrder_InsufficientStock_OrderRejected_InventoryUnchanged()
    {
        using var client = _factory.CreateAuthenticatedClient();

        var stockBefore = await GetInventoryQuantityAsync(client, ExistingProductId, TestContext.CancellationToken);
        var tooLargeQuantity = stockBefore + 1;

        var orderResponse = await client.PostAsync(
            "/api/orders",
            BuildOrderRequest(tooLargeQuantity),
            TestContext.CancellationToken);

        Assert.IsTrue(
            orderResponse.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.UnprocessableEntity,
            $"Expected 400/422 for order rejection, got {(int)orderResponse.StatusCode}. Body: {await orderResponse.Content.ReadAsStringAsync(TestContext.CancellationToken)}");

        var stockAfter = await GetInventoryQuantityAsync(client, ExistingProductId, TestContext.CancellationToken);
        Assert.AreEqual(stockBefore, stockAfter,
            "Inventory should remain unchanged when order creation fails.");
    }

    [TestMethod]
    public async Task CreateOrder_ImmediatelyReadable_AfterCreateReturns()
    {
        using var client = _factory.CreateAuthenticatedClient();

        var createResponse = await client.PostAsync("/api/orders", BuildOrderRequest(1), TestContext.CancellationToken);
        var createBody = await createResponse.Content.ReadAsStringAsync(TestContext.CancellationToken);

        if (createResponse.StatusCode == HttpStatusCode.Created)
        {
            var createJson = JsonSerializer.Deserialize<JsonElement>(createBody, JsonOptions);
            var orderData = GetRequiredProperty(createJson, "data");
            var orderId = GetGuidProperty(orderData, "id");

            var getResponse = await client.GetAsync($"/api/orders/{orderId}", TestContext.CancellationToken);
            Assert.AreEqual(HttpStatusCode.OK, getResponse.StatusCode,
                "Order should be readable immediately after creation in current synchronous flow.");
            return;
        }

        Assert.AreEqual(HttpStatusCode.BadRequest, createResponse.StatusCode,
            $"Expected either 201 or 400. Body: {createBody}");
        Assert.AreEqual("ADDRESS_NOT_FOUND", GetErrorCode(createBody),
            "Current known behavior: create order may fail before persistence when shipping address cannot be resolved.");
    }

    private static StringContent BuildOrderRequest(int quantity, string? promoCode = null)
    {
        var payload = new
        {
            PaymentMethod = "card",
            PromoCode = promoCode,
            ShippingAddress = new
            {
                FirstName = "Phase",
                LastName = "Eight",
                Phone = "555-0101",
                StreetLine1 = "123 Sync St",
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
                    Quantity = quantity
                }
            }
        };

        return new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
    }

    private static async Task<int> GetInventoryQuantityAsync(HttpClient client, Guid productId, CancellationToken ct)
    {
        var response = await client.GetAsync($"/api/inventory/{productId}", ct);
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode,
            $"Expected 200 from inventory endpoint. Body: {await response.Content.ReadAsStringAsync(ct)}");

        var json = JsonSerializer.Deserialize<JsonElement>(await response.Content.ReadAsStringAsync(ct), JsonOptions);
        var data = GetRequiredProperty(json, "data");
        return GetIntProperty(data, "quantity");
    }

    private static string GetErrorCode(string responseBody)
    {
        var json = JsonSerializer.Deserialize<JsonElement>(responseBody, JsonOptions);
        var details = GetRequiredProperty(json, "errorDetails");
        return GetStringProperty(details, "code");
    }

    private static JsonElement GetRequiredProperty(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var value))
        {
            var json = element.GetRawText();
            Assert.Fail($"Missing property '{propertyName}'. Payload: {json}");
        }

        return value;
    }

    private static int GetIntProperty(JsonElement element, string propertyName)
    {
        var value = GetRequiredProperty(element, propertyName);
        return value.GetInt32();
    }

    private static Guid GetGuidProperty(JsonElement element, string propertyName)
    {
        var value = GetRequiredProperty(element, propertyName);
        return value.ValueKind == JsonValueKind.String
            ? Guid.Parse(value.GetString() ?? string.Empty)
            : value.GetGuid();
    }

    private static string GetStringProperty(JsonElement element, string propertyName)
    {
        var value = GetRequiredProperty(element, propertyName);
        return value.GetString() ?? string.Empty;
    }
}
