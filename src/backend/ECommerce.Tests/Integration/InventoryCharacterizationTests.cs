using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ECommerce.Tests.Integration;

/// <summary>
/// Characterization tests for InventoryController.
/// Written BEFORE Phase 3 migration — pin the exact HTTP contract.
/// These tests must pass against both old InventoryService AND new MediatR handlers.
/// </summary>
[TestClass]
public class InventoryCharacterizationTests
{
    private TestWebApplicationFactory _factory = null!;
    private static readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

    // Seeded product ID from TestWebApplicationFactory (matches seed data)
    private static readonly Guid SeededProductId = Guid.Parse("22222222-2222-2222-2222-222222222222");

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

    // ── GET /api/inventory ─────────────────────────────────────────────────────

    [TestMethod]
    public async Task GetAllInventory_AsAdmin_Returns200()
    {
        using var client = _factory.CreateAdminClient();

        var res = await client.GetAsync("/api/inventory", TestContext.CancellationToken);

        Assert.AreEqual(HttpStatusCode.OK, res.StatusCode);
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

    [TestMethod]
    public async Task GetAllInventory_ResponseShape_HasDataProperty()
    {
        using var client = _factory.CreateAdminClient();

        var res = await client.GetAsync("/api/inventory", TestContext.CancellationToken);
        var body = await res.Content.ReadAsStringAsync();
        var json = JsonSerializer.Deserialize<JsonElement>(body, _jsonOptions);

        Assert.AreEqual(HttpStatusCode.OK, res.StatusCode);
        Assert.IsTrue(json.TryGetProperty("data", out _), "Response must have 'data' property");
    }

    // ── GET /api/inventory/low-stock ───────────────────────────────────────────

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

    // ── GET /api/inventory/{productId} ────────────────────────────────────────

    [TestMethod]
    public async Task GetProductStock_IsAnonymousEndpoint()
    {
        // [AllowAnonymous] — must not return 401/403
        using var client = _factory.CreateUnauthenticatedClient();

        var res = await client.GetAsync($"/api/inventory/{SeededProductId}", TestContext.CancellationToken);

        Assert.IsFalse(
            res.StatusCode == HttpStatusCode.Unauthorized || res.StatusCode == HttpStatusCode.Forbidden,
            $"Endpoint should be anonymous but returned {(int)res.StatusCode}");
    }

    [TestMethod]
    public async Task GetProductStock_UnknownProduct_Returns404()
    {
        using var client = _factory.CreateUnauthenticatedClient();

        var res = await client.GetAsync($"/api/inventory/{Guid.NewGuid()}", TestContext.CancellationToken);

        Assert.AreEqual(HttpStatusCode.NotFound, res.StatusCode);
    }

    // ── GET /api/inventory/{productId}/available ──────────────────────────────

    [TestMethod]
    public async Task CheckAvailableQuantity_IsAnonymousEndpoint()
    {
        // [AllowAnonymous] — must not return 401/403
        using var client = _factory.CreateUnauthenticatedClient();

        var res = await client.GetAsync(
            $"/api/inventory/{SeededProductId}/available?quantity=1",
            TestContext.CancellationToken);

        Assert.IsFalse(
            res.StatusCode == HttpStatusCode.Unauthorized || res.StatusCode == HttpStatusCode.Forbidden,
            $"Endpoint should be anonymous but returned {(int)res.StatusCode}");
    }

    // ── GET /api/inventory/{productId}/history ────────────────────────────────

    [TestMethod]
    public async Task GetInventoryHistory_AsAdmin_Returns200OrNotFound()
    {
        using var client = _factory.CreateAdminClient();

        var res = await client.GetAsync(
            $"/api/inventory/{SeededProductId}/history",
            TestContext.CancellationToken);

        Assert.IsTrue(
            res.StatusCode == HttpStatusCode.OK || res.StatusCode == HttpStatusCode.NotFound,
            $"Expected 200 or 404, got {(int)res.StatusCode}");
    }

    [TestMethod]
    public async Task GetInventoryHistory_Unauthenticated_Returns401()
    {
        using var client = _factory.CreateUnauthenticatedClient();

        var res = await client.GetAsync(
            $"/api/inventory/{SeededProductId}/history",
            TestContext.CancellationToken);

        Assert.AreEqual(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    // ── POST /api/inventory/{productId}/adjust ────────────────────────────────

    [TestMethod]
    public async Task AdjustStock_Unauthenticated_Returns401()
    {
        using var client = _factory.CreateUnauthenticatedClient();

        var res = await client.PostAsync(
            $"/api/inventory/{SeededProductId}/adjust",
            Json(new { Quantity = 10, Reason = "correction" }),
            TestContext.CancellationToken);

        Assert.AreEqual(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [TestMethod]
    public async Task AdjustStock_AsCustomer_Returns403()
    {
        ConditionalTestAuthHandler.CurrentUserRole = "Customer";
        using var client = _factory.CreateAuthenticatedClient();

        var res = await client.PostAsync(
            $"/api/inventory/{SeededProductId}/adjust",
            Json(new { Quantity = 10, Reason = "correction" }),
            TestContext.CancellationToken);

        Assert.AreEqual(HttpStatusCode.Forbidden, res.StatusCode);
    }

    [TestMethod]
    public async Task AdjustStock_MissingBody_Returns400Or422()
    {
        using var client = _factory.CreateAdminClient();

        var res = await client.PostAsync(
            $"/api/inventory/{SeededProductId}/adjust",
            Json(new { }),
            TestContext.CancellationToken);

        Assert.IsTrue(
            res.StatusCode == HttpStatusCode.BadRequest || res.StatusCode == HttpStatusCode.UnprocessableEntity,
            $"Expected 400 or 422, got {(int)res.StatusCode}");
    }

    // ── POST /api/inventory/{productId}/restock ───────────────────────────────

    [TestMethod]
    public async Task RestockProduct_Unauthenticated_Returns401()
    {
        using var client = _factory.CreateUnauthenticatedClient();

        var res = await client.PostAsync(
            $"/api/inventory/{SeededProductId}/restock",
            Json(new { Quantity = 5, Reason = "restock" }),
            TestContext.CancellationToken);

        Assert.AreEqual(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [TestMethod]
    public async Task RestockProduct_AsCustomer_Returns403()
    {
        ConditionalTestAuthHandler.CurrentUserRole = "Customer";
        using var client = _factory.CreateAuthenticatedClient();

        var res = await client.PostAsync(
            $"/api/inventory/{SeededProductId}/restock",
            Json(new { Quantity = 5, Reason = "restock" }),
            TestContext.CancellationToken);

        Assert.AreEqual(HttpStatusCode.Forbidden, res.StatusCode);
    }

    // ── POST /api/inventory/check-availability ────────────────────────────────

    [TestMethod]
    public async Task CheckStockAvailability_IsAnonymousEndpoint()
    {
        // [AllowAnonymous] — must not return 401/403
        using var client = _factory.CreateUnauthenticatedClient();
        var payload = new { Items = new[] { new { ProductId = SeededProductId, Quantity = 1 } } };

        var res = await client.PostAsync(
            "/api/inventory/check-availability",
            Json(payload),
            TestContext.CancellationToken);

        Assert.IsFalse(
            res.StatusCode == HttpStatusCode.Unauthorized || res.StatusCode == HttpStatusCode.Forbidden,
            $"Endpoint should be anonymous but returned {(int)res.StatusCode}");
    }

    [TestMethod]
    public async Task CheckStockAvailability_ValidRequest_Returns200()
    {
        using var client = _factory.CreateUnauthenticatedClient();
        var payload = new { Items = new[] { new { ProductId = SeededProductId, Quantity = 1 } } };

        var res = await client.PostAsync(
            "/api/inventory/check-availability",
            Json(payload),
            TestContext.CancellationToken);

        Assert.AreEqual(HttpStatusCode.OK, res.StatusCode);
    }

    // ── PUT /api/inventory/{productId} ────────────────────────────────────────

    [TestMethod]
    public async Task UpdateProductStock_Unauthenticated_Returns401()
    {
        using var client = _factory.CreateUnauthenticatedClient();

        var res = await client.PutAsync(
            $"/api/inventory/{SeededProductId}",
            Json(new { Quantity = 100, Reason = "update" }),
            TestContext.CancellationToken);

        Assert.AreEqual(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [TestMethod]
    public async Task UpdateProductStock_AsCustomer_Returns403()
    {
        ConditionalTestAuthHandler.CurrentUserRole = "Customer";
        using var client = _factory.CreateAuthenticatedClient();

        var res = await client.PutAsync(
            $"/api/inventory/{SeededProductId}",
            Json(new { Quantity = 100, Reason = "update" }),
            TestContext.CancellationToken);

        Assert.AreEqual(HttpStatusCode.Forbidden, res.StatusCode);
    }

    // ── PUT /api/inventory/bulk-update ────────────────────────────────────────

    [TestMethod]
    public async Task BulkUpdateStock_Unauthenticated_Returns401()
    {
        using var client = _factory.CreateUnauthenticatedClient();
        var payload = new { Updates = new[] { new { ProductId = SeededProductId, Quantity = 5 } } };

        var res = await client.PutAsync(
            "/api/inventory/bulk-update",
            Json(payload),
            TestContext.CancellationToken);

        Assert.AreEqual(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [TestMethod]
    public async Task BulkUpdateStock_AsCustomer_Returns403()
    {
        ConditionalTestAuthHandler.CurrentUserRole = "Customer";
        using var client = _factory.CreateAuthenticatedClient();
        var payload = new { Updates = new[] { new { ProductId = SeededProductId, Quantity = 5 } } };

        var res = await client.PutAsync(
            "/api/inventory/bulk-update",
            Json(payload),
            TestContext.CancellationToken);

        Assert.AreEqual(HttpStatusCode.Forbidden, res.StatusCode);
    }
}
