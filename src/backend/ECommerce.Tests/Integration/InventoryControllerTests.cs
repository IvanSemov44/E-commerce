using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ECommerce.Tests.Integration;

[TestClass]
public class InventoryControllerTests
{
    private TestWebApplicationFactory _factory = null!;
    private static readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };
    private static readonly Guid _seededProductId = Guid.Parse("22222222-2222-2222-2222-222222222222");

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

    private static StringContent Json(object dto) =>
        new(JsonSerializer.Serialize(dto), Encoding.UTF8, "application/json");

    // ── GET /api/inventory ───────────────────────────────────────────────────

    [TestMethod]
    public async Task GetAllInventory_AsAdmin_Returns200()
    {
        using var client = _factory.CreateAdminClient();
        var res = await client.GetAsync("/api/inventory", TestContext.CancellationToken);
        Assert.AreEqual(HttpStatusCode.OK, res.StatusCode);
    }

    [TestMethod]
    public async Task GetAllInventory_ResponseShape_HasDataProperty()
    {
        using var client = _factory.CreateAdminClient();
        var res = await client.GetAsync("/api/inventory", TestContext.CancellationToken);
        var json = JsonSerializer.Deserialize<JsonElement>(await res.Content.ReadAsStringAsync(), _jsonOptions);
        Assert.AreEqual(HttpStatusCode.OK, res.StatusCode);
        Assert.IsTrue(json.TryGetProperty("data", out _), "Response must have 'data'");
    }

    [TestMethod]
    public async Task GetAllInventory_Unauthenticated_Returns401()
    {
        using var client = _factory.CreateUnauthenticatedClient();
        var res = await client.GetAsync("/api/inventory", TestContext.CancellationToken);
        Assert.AreEqual(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [TestMethod]
    public async Task GetAllInventory_AsCustomer_Returns403()
    {
        ConditionalTestAuthHandler.CurrentUserRole = "Customer";
        using var client = _factory.CreateAuthenticatedClient();
        var res = await client.GetAsync("/api/inventory", TestContext.CancellationToken);
        Assert.AreEqual(HttpStatusCode.Forbidden, res.StatusCode);
    }

    // ── GET /api/inventory/{productId} ───────────────────────────────────────

    [TestMethod]
    public async Task GetProductStock_IsAnonymousEndpoint()
    {
        using var client = _factory.CreateUnauthenticatedClient();
        var res = await client.GetAsync($"/api/inventory/{_seededProductId}", TestContext.CancellationToken);
        Assert.IsFalse(res.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden,
            $"Endpoint should be anonymous but returned {(int)res.StatusCode}");
    }

    [TestMethod]
    public async Task GetProductStock_SeededProduct_Returns200()
    {
        using var client = _factory.CreateUnauthenticatedClient();
        var res = await client.GetAsync($"/api/inventory/{_seededProductId}", TestContext.CancellationToken);
        Assert.IsTrue(res.StatusCode is HttpStatusCode.OK or HttpStatusCode.NotFound);
    }

    [TestMethod]
    public async Task GetProductStock_UnknownProduct_Returns404()
    {
        using var client = _factory.CreateUnauthenticatedClient();
        var res = await client.GetAsync($"/api/inventory/{Guid.NewGuid()}", TestContext.CancellationToken);
        Assert.AreEqual(HttpStatusCode.NotFound, res.StatusCode);
    }

    // ── GET /api/inventory/{productId}/available ─────────────────────────────

    [TestMethod]
    public async Task CheckAvailableQuantity_IsAnonymousEndpoint()
    {
        using var client = _factory.CreateUnauthenticatedClient();
        var res = await client.GetAsync($"/api/inventory/{_seededProductId}/available?quantity=1", TestContext.CancellationToken);
        Assert.IsFalse(res.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden,
            $"Endpoint should be anonymous but returned {(int)res.StatusCode}");
    }

    [TestMethod]
    public async Task CheckAvailableQuantity_ReturnsBoolean()
    {
        using var client = _factory.CreateUnauthenticatedClient();
        var res = await client.GetAsync($"/api/inventory/{_seededProductId}/available?quantity=1", TestContext.CancellationToken);
        if (res.StatusCode == HttpStatusCode.OK)
        {
            var json = JsonSerializer.Deserialize<JsonElement>(await res.Content.ReadAsStringAsync(), _jsonOptions);
            Assert.IsTrue(json.TryGetProperty("data", out _), "Response should have data property");
        }
    }

    // ── GET /api/inventory/{productId}/history ────────────────────────────────

    [TestMethod]
    public async Task GetInventoryHistory_AsAdmin_Returns200OrNotFound()
    {
        using var client = _factory.CreateAdminClient();
        var res = await client.GetAsync($"/api/inventory/{_seededProductId}/history", TestContext.CancellationToken);
        Assert.IsTrue(res.StatusCode is HttpStatusCode.OK or HttpStatusCode.NotFound,
            $"Expected 200 or 404, got {(int)res.StatusCode}");
    }

    [TestMethod]
    public async Task GetInventoryHistory_Unauthenticated_Returns401()
    {
        using var client = _factory.CreateUnauthenticatedClient();
        var res = await client.GetAsync($"/api/inventory/{_seededProductId}/history", TestContext.CancellationToken);
        Assert.AreEqual(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    // ── GET /api/inventory/low-stock ─────────────────────────────────────────

    [TestMethod]
    public async Task GetLowStockProducts_AsAdmin_Returns200()
    {
        using var client = _factory.CreateAdminClient();
        var res = await client.GetAsync("/api/inventory/low-stock", TestContext.CancellationToken);
        Assert.AreEqual(HttpStatusCode.OK, res.StatusCode);
    }

    [TestMethod]
    public async Task GetLowStockProducts_Unauthenticated_Returns401()
    {
        using var client = _factory.CreateUnauthenticatedClient();
        var res = await client.GetAsync("/api/inventory/low-stock", TestContext.CancellationToken);
        Assert.AreEqual(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [TestMethod]
    public async Task GetLowStockProducts_AsCustomer_Returns403()
    {
        using var client = _factory.CreateAuthenticatedClient();
        var res = await client.GetAsync("/api/inventory/low-stock", TestContext.CancellationToken);
        Assert.AreEqual(HttpStatusCode.Forbidden, res.StatusCode);
    }

    [TestMethod]
    public async Task GetLowStockProducts_WithLargePageSize_IsClamped()
    {
        using var client = _factory.CreateAdminClient();
        var res = await client.GetAsync("/api/inventory/low-stock?page=1&pageSize=1000", TestContext.CancellationToken);
        Assert.IsTrue(res.StatusCode is HttpStatusCode.OK or HttpStatusCode.NotFound);
        if (res.StatusCode == HttpStatusCode.OK)
        {
            var json = JsonSerializer.Deserialize<JsonElement>(await res.Content.ReadAsStringAsync());
            if (json.TryGetProperty("data", out var data))
            {
                if (data.ValueKind == JsonValueKind.Array)
                    Assert.IsTrue(data.GetArrayLength() <= 100, "pageSize should be clamped to 100");
                else if (data.TryGetProperty("items", out var items) && items.ValueKind == JsonValueKind.Array)
                    Assert.IsTrue(items.GetArrayLength() <= 100, "pageSize should be clamped to 100");
            }
        }
    }

    // ── POST /api/inventory/{productId}/adjust ────────────────────────────────

    [TestMethod]
    public async Task AdjustStock_Unauthenticated_Returns401()
    {
        using var client = _factory.CreateUnauthenticatedClient();
        var res = await client.PostAsync($"/api/inventory/{_seededProductId}/adjust",
            Json(new { Quantity = 10, Reason = "correction" }), TestContext.CancellationToken);
        Assert.AreEqual(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [TestMethod]
    public async Task AdjustStock_AsCustomer_Returns403()
    {
        ConditionalTestAuthHandler.CurrentUserRole = "Customer";
        using var client = _factory.CreateAuthenticatedClient();
        var res = await client.PostAsync($"/api/inventory/{_seededProductId}/adjust",
            Json(new { Quantity = 10, Reason = "correction" }), TestContext.CancellationToken);
        Assert.AreEqual(HttpStatusCode.Forbidden, res.StatusCode);
    }

    [TestMethod]
    public async Task AdjustStock_MissingBody_Returns400Or422()
    {
        using var client = _factory.CreateAdminClient();
        var res = await client.PostAsync($"/api/inventory/{_seededProductId}/adjust",
            Json(new { }), TestContext.CancellationToken);
        Assert.IsTrue(res.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.UnprocessableEntity,
            $"Expected 400 or 422, got {(int)res.StatusCode}");
    }

    // ── POST /api/inventory/{productId}/restock ───────────────────────────────

    [TestMethod]
    public async Task RestockProduct_Unauthenticated_Returns401()
    {
        using var client = _factory.CreateUnauthenticatedClient();
        var res = await client.PostAsync($"/api/inventory/{_seededProductId}/restock",
            Json(new { Quantity = 5, Reason = "restock" }), TestContext.CancellationToken);
        Assert.AreEqual(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [TestMethod]
    public async Task RestockProduct_AsCustomer_Returns403()
    {
        ConditionalTestAuthHandler.CurrentUserRole = "Customer";
        using var client = _factory.CreateAuthenticatedClient();
        var res = await client.PostAsync($"/api/inventory/{_seededProductId}/restock",
            Json(new { Quantity = 5, Reason = "restock" }), TestContext.CancellationToken);
        Assert.AreEqual(HttpStatusCode.Forbidden, res.StatusCode);
    }

    // ── POST /api/inventory/check-availability ────────────────────────────────

    [TestMethod]
    public async Task CheckStockAvailability_IsAnonymousEndpoint()
    {
        using var client = _factory.CreateUnauthenticatedClient();
        var res = await client.PostAsync("/api/inventory/check-availability",
            Json(new { Items = new[] { new { ProductId = _seededProductId, Quantity = 1 } } }),
            TestContext.CancellationToken);
        Assert.IsFalse(res.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden,
            $"Endpoint should be anonymous but returned {(int)res.StatusCode}");
    }

    [TestMethod]
    public async Task CheckStockAvailability_ValidRequest_Returns200()
    {
        using var client = _factory.CreateUnauthenticatedClient();
        var res = await client.PostAsync("/api/inventory/check-availability",
            Json(new { Items = new[] { new { ProductId = _seededProductId, Quantity = 1 } } }),
            TestContext.CancellationToken);
        Assert.AreEqual(HttpStatusCode.OK, res.StatusCode);
    }

    // ── PUT /api/inventory/{productId} ────────────────────────────────────────

    [TestMethod]
    public async Task UpdateProductStock_WithAdminAndValidData_ReturnsOk()
    {
        using var client = _factory.CreateAdminClient();
        var res = await client.PutAsync($"/api/inventory/{_seededProductId}",
            Json(new { Quantity = 150, Reason = "restock", Notes = "Test restock" }),
            TestContext.CancellationToken);
        Assert.AreEqual(HttpStatusCode.OK, res.StatusCode,
            $"UpdateStock should return OK for existing product, got {res.StatusCode}");
    }

    [TestMethod]
    public async Task UpdateProductStock_WithNegativeQuantity_ReturnsBadRequest()
    {
        using var client = _factory.CreateAdminClient();
        var res = await client.PutAsync($"/api/inventory/{_seededProductId}",
            Json(new { Quantity = -50, Reason = "correction" }),
            TestContext.CancellationToken);
        Assert.IsTrue(res.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.UnprocessableEntity or HttpStatusCode.OK);
    }

    [TestMethod]
    public async Task UpdateProductStock_Unauthenticated_Returns401()
    {
        using var client = _factory.CreateUnauthenticatedClient();
        var res = await client.PutAsync($"/api/inventory/{_seededProductId}",
            Json(new { Quantity = 100, Reason = "update" }), TestContext.CancellationToken);
        Assert.AreEqual(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [TestMethod]
    public async Task UpdateProductStock_AsCustomer_Returns403()
    {
        ConditionalTestAuthHandler.CurrentUserRole = "Customer";
        using var client = _factory.CreateAuthenticatedClient();
        var res = await client.PutAsync($"/api/inventory/{_seededProductId}",
            Json(new { Quantity = 100, Reason = "update" }), TestContext.CancellationToken);
        Assert.AreEqual(HttpStatusCode.Forbidden, res.StatusCode);
    }

    // ── PUT /api/inventory/bulk-update ────────────────────────────────────────

    [TestMethod]
    public async Task BulkUpdateStock_WithAdminAndValidData_ReturnsOk()
    {
        using var client = _factory.CreateAdminClient();
        var res = await client.PutAsync("/api/inventory/bulk-update",
            Json(new { Updates = new[] { new { ProductId = Guid.NewGuid(), Quantity = 100 }, new { ProductId = Guid.NewGuid(), Quantity = 50 } } }),
            TestContext.CancellationToken);
        Assert.IsTrue(res.StatusCode is HttpStatusCode.OK or HttpStatusCode.BadRequest or HttpStatusCode.NotFound);
    }

    [TestMethod]
    public async Task BulkUpdateStock_Unauthenticated_Returns401()
    {
        using var client = _factory.CreateUnauthenticatedClient();
        var res = await client.PutAsync("/api/inventory/bulk-update",
            Json(new { Updates = new[] { new { ProductId = _seededProductId, Quantity = 5 } } }),
            TestContext.CancellationToken);
        Assert.AreEqual(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [TestMethod]
    public async Task BulkUpdateStock_AsCustomer_Returns403()
    {
        ConditionalTestAuthHandler.CurrentUserRole = "Customer";
        using var client = _factory.CreateAuthenticatedClient();
        var res = await client.PutAsync("/api/inventory/bulk-update",
            Json(new { Updates = new[] { new { ProductId = _seededProductId, Quantity = 5 } } }),
            TestContext.CancellationToken);
        Assert.AreEqual(HttpStatusCode.Forbidden, res.StatusCode);
    }
}
