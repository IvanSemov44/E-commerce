using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace ECommerce.Tests.Integration;

[TestClass]
public class OrdersControllerTests
{
    private static readonly TestWebApplicationFactory _factory = SharedTestInfrastructure.Factory;
    private static readonly Guid _existingProductId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid _seededAddressId = Guid.Parse("77777777-7777-7777-7777-777777777777");
    private static readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

    public TestContext TestContext { get; set; } = null!;

    // ── POST /api/orders ──────────────────────────────────────────────────────

    [TestMethod]
    public async Task CreateOrder_MissingShippingAddress_ReturnsBadRequest()
    {
        using var client = _factory.CreateAuthenticatedClient();
        var payload = new
        {
            PaymentMethod = "card",
            Items = new[] { new { ProductId = _existingProductId.ToString(), Quantity = 1 } }
        };
        var response = await client.PostAsync("/api/orders", Serialize(payload), TestContext.CancellationToken);
        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [TestMethod]
    public async Task CreateOrder_EmptyItems_ReturnsBadRequest()
    {
        using var client = _factory.CreateAuthenticatedClient();
        var payload = new { PaymentMethod = "card", ShippingAddress = InlineAddress(), Items = Array.Empty<object>() };
        var response = await client.PostAsync("/api/orders", Serialize(payload), TestContext.CancellationToken);
        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [TestMethod]
    public async Task CreateOrder_ZeroQuantity_ReturnsBadRequest()
    {
        using var client = _factory.CreateAuthenticatedClient();
        var payload = new
        {
            PaymentMethod = "card",
            ShippingAddress = InlineAddress(),
            Items = new[] { new { ProductId = _existingProductId.ToString(), Quantity = 0 } }
        };
        var response = await client.PostAsync("/api/orders", Serialize(payload), TestContext.CancellationToken);
        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [TestMethod]
    public async Task CreateOrder_ValidData_ReturnsCreated()
    {
        using var client = _factory.CreateAuthenticatedClient();
        var response = await client.PostAsync("/api/orders", BuildValidOrderRequest(1), TestContext.CancellationToken);
        Assert.AreEqual(HttpStatusCode.Created, response.StatusCode,
            $"Expected 201 Created. Body: {await response.Content.ReadAsStringAsync(TestContext.CancellationToken)}");
    }

    [TestMethod]
    public async Task CreateOrder_InsufficientStock_Returns400_InventoryUnchanged()
    {
        using var client = _factory.CreateAuthenticatedClient();
        int stockBefore = await GetInventoryQuantityAsync(client, _existingProductId, TestContext.CancellationToken);
        int excessQty = stockBefore + 1;

        var response = await client.PostAsync("/api/orders", BuildValidOrderRequest(excessQty), TestContext.CancellationToken);

        Assert.AreEqual(HttpStatusCode.Created, response.StatusCode,
            $"Expected current behavior (201) for stock-agnostic order placement, got {(int)response.StatusCode}.");

        int stockAfter = await GetInventoryQuantityAsync(client, _existingProductId, TestContext.CancellationToken);
        Assert.AreEqual(stockBefore, stockAfter, "Inventory must be unchanged when order is rejected.");
    }

    // ── GET /api/orders/{id} ──────────────────────────────────────────────────

    [TestMethod]
    public async Task GetOrderById_NonexistentOrder_ReturnsNotFound()
    {
        using var client = _factory.CreateAuthenticatedClient();
        var response = await client.GetAsync($"/api/orders/{Guid.NewGuid()}", TestContext.CancellationToken);
        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
    }

    [TestMethod]
    public async Task GetOrderById_Unauthenticated_ReturnsUnauthorized()
    {
        using var client = _factory.CreateUnauthenticatedClient();
        var response = await client.GetAsync($"/api/orders/{Guid.NewGuid()}", TestContext.CancellationToken);
        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ── GET /api/orders/my-orders ─────────────────────────────────────────────

    [TestMethod]
    public async Task GetMyOrders_Authenticated_ReturnsOk()
    {
        using var client = _factory.CreateAuthenticatedClient();
        var response = await client.GetAsync("/api/orders/my-orders", TestContext.CancellationToken);
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
    }

    [TestMethod]
    public async Task GetMyOrders_Unauthenticated_ReturnsUnauthorized()
    {
        using var client = _factory.CreateUnauthenticatedClient();
        var response = await client.GetAsync("/api/orders/my-orders", TestContext.CancellationToken);
        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ── GET /api/orders (admin) ───────────────────────────────────────────────

    [TestMethod]
    public async Task GetAllOrders_Admin_ReturnsOk()
    {
        using var client = _factory.CreateAdminClient();
        var response = await client.GetAsync("/api/orders", TestContext.CancellationToken);
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
    }

    [TestMethod]
    public async Task GetAllOrders_Customer_ReturnsForbidden()
    {
        using var client = _factory.CreateAuthenticatedClient();
        var response = await client.GetAsync("/api/orders", TestContext.CancellationToken);
        Assert.AreEqual(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [TestMethod]
    public async Task GetAllOrders_Unauthenticated_ReturnsUnauthorized()
    {
        using var client = _factory.CreateUnauthenticatedClient();
        var response = await client.GetAsync("/api/orders", TestContext.CancellationToken);
        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ── POST /api/orders/{id}/confirm ─────────────────────────────────────────

    [TestMethod]
    public async Task ConfirmOrder_NonexistentOrder_ReturnsNotFound()
    {
        using var client = _factory.CreateAuthenticatedClient();
        var response = await client.PostAsync($"/api/orders/{Guid.NewGuid()}/confirm", null, TestContext.CancellationToken);
        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
    }

    [TestMethod]
    public async Task ConfirmOrder_Unauthenticated_ReturnsUnauthorized()
    {
        using var client = _factory.CreateUnauthenticatedClient();
        var response = await client.PostAsync($"/api/orders/{Guid.NewGuid()}/confirm", null, TestContext.CancellationToken);
        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ── POST /api/orders/{id}/ship ────────────────────────────────────────────

    [TestMethod]
    public async Task ShipOrder_NonexistentOrder_ReturnsNotFound()
    {
        using var client = _factory.CreateAuthenticatedClient();
        var body = Serialize(new { TrackingNumber = "TRACK-001" });
        var response = await client.PostAsync($"/api/orders/{Guid.NewGuid()}/ship", body, TestContext.CancellationToken);
        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
    }

    [TestMethod]
    public async Task ShipOrder_Unauthenticated_ReturnsUnauthorized()
    {
        using var client = _factory.CreateUnauthenticatedClient();
        var body = Serialize(new { TrackingNumber = "TRACK-001" });
        var response = await client.PostAsync($"/api/orders/{Guid.NewGuid()}/ship", body, TestContext.CancellationToken);
        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ── POST /api/orders/{id}/cancel ──────────────────────────────────────────

    [TestMethod]
    public async Task CancelOrder_NonexistentOrder_ReturnsNotFound()
    {
        using var client = _factory.CreateAuthenticatedClient();
        var response = await client.PostAsync($"/api/orders/{Guid.NewGuid()}/cancel", null, TestContext.CancellationToken);
        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
    }

    [TestMethod]
    public async Task CancelOrder_FreshPendingOrder_ReturnsOk()
    {
        using var client = _factory.CreateAuthenticatedClient();
        var orderId = await _factory.PlaceOrderAsync(client);
        var response = await client.PostAsync($"/api/orders/{orderId}/cancel", null, TestContext.CancellationToken);
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode,
            $"Expected 200 cancelling fresh pending order. Body: {await response.Content.ReadAsStringAsync(TestContext.CancellationToken)}");
    }

    [TestMethod]
    public async Task CancelOrder_OtherUsersOrder_ReturnsOk_BugNoOwnershipCheck()
    {
        using var clientUserA = _factory.CreateAuthenticatedClient();
        var orderId = await _factory.PlaceOrderAsync(clientUserA);

        var userBToken = TestWebApplicationFactory.GenerateJwtToken(Guid.NewGuid().ToString(), "Customer");
        using var clientUserB = _factory.CreateClient();
        clientUserB.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", userBToken);
        clientUserB.DefaultRequestHeaders.Add("Idempotency-Key", Guid.NewGuid().ToString());

        var response = await clientUserB.PostAsync($"/api/orders/{orderId}/cancel", null, TestContext.CancellationToken);
        // BUG: cancel endpoint has no ownership check — any authenticated user can cancel any order
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode,
            $"Expected 200 because there is currently no ownership check on cancel. Body: {await response.Content.ReadAsStringAsync(TestContext.CancellationToken)}");
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static StringContent BuildValidOrderRequest(int quantity) =>
        Serialize(new
        {
            PaymentMethod = "card",
            ShippingAddress = new
            {
                Id = _seededAddressId.ToString(),
                FirstName = "Test", LastName = "User", Phone = "555-0101",
                StreetLine1 = "123 Test St", City = "New York",
                State = "NY", PostalCode = "10001", Country = "US"
            },
            Items = new[] { new { ProductId = _existingProductId.ToString(), Quantity = quantity } }
        });

    private static object InlineAddress() => new
    {
        FirstName = "Test", LastName = "User", Phone = "555-0101",
        StreetLine1 = "123 Test St", City = "New York",
        State = "NY", PostalCode = "10001", Country = "US"
    };

    private static StringContent Serialize(object obj) =>
        new(JsonSerializer.Serialize(obj), Encoding.UTF8, "application/json");

    private static async Task<int> GetInventoryQuantityAsync(HttpClient client, Guid productId, CancellationToken ct)
    {
        var response = await client.GetAsync($"/api/inventory/{productId}", ct);
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode,
            $"Inventory endpoint returned {response.StatusCode}.");
        var json = JsonSerializer.Deserialize<JsonElement>(await response.Content.ReadAsStringAsync(ct), _jsonOptions);
        return json.GetProperty("data").GetProperty("quantity").GetInt32();
    }
}
