# Phase 3, Step 0: Inventory Characterization Tests

**Do this BEFORE touching any migration code.** These tests pin the existing HTTP contract so you can verify nothing regressed after cutover.

**Prerequisite**: Phase 2 (Identity) complete and all tests pass.

---

## Context

The existing `InventoryControllerTests.cs` has loose assertions ("OK or NotFound"). We create a dedicated `InventoryCharacterizationTests.cs` that locks down the EXACT contract: status codes, response shape, error codes, auth requirements. These tests pass against the OLD `InventoryService` before migration, and must still pass after cutover to MediatR handlers.

---

## Task: Create Characterization Tests in ECommerce.Tests

### Add to existing project — no new project needed

Files go in `src/backend/ECommerce.Tests/Integration/`.

---

### File: `InventoryCharacterizationTests.cs`

```csharp
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
        ConditionalTestAuthHandler.CurrentUserRole = "Admin";
        using var client = _factory.CreateAuthenticatedClient();

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
        // Customer role is not allowed — endpoint is Admin/SuperAdmin only
        ConditionalTestAuthHandler.CurrentUserRole = "Customer";
        using var client = _factory.CreateAuthenticatedClient();

        var res = await client.GetAsync("/api/inventory", TestContext.CancellationToken);

        Assert.AreEqual(HttpStatusCode.Forbidden, res.StatusCode);
    }

    [TestMethod]
    public async Task GetAllInventory_ResponseShape_HasDataAndPagination()
    {
        ConditionalTestAuthHandler.CurrentUserRole = "Admin";
        using var client = _factory.CreateAuthenticatedClient();

        var res = await client.GetAsync("/api/inventory", TestContext.CancellationToken);
        string body = await res.Content.ReadAsStringAsync();
        var json = JsonSerializer.Deserialize<JsonElement>(body, _jsonOptions);

        Assert.AreEqual(HttpStatusCode.OK, res.StatusCode);
        Assert.IsTrue(json.TryGetProperty("data", out _), "Response must have 'data' property");
    }

    // ── GET /api/inventory/low-stock ───────────────────────────────────────────

    [TestMethod]
    public async Task GetLowStockProducts_AsAdmin_Returns200()
    {
        ConditionalTestAuthHandler.CurrentUserRole = "Admin";
        using var client = _factory.CreateAuthenticatedClient();

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
    public async Task GetProductStock_SeededProduct_Returns200()
    {
        // Anonymous — endpoint is [AllowAnonymous]
        using var client = _factory.CreateUnauthenticatedClient();

        var res = await client.GetAsync($"/api/inventory/{SeededProductId}", TestContext.CancellationToken);

        // Seeded product must exist in InMemory DB
        Assert.IsTrue(
            res.StatusCode == HttpStatusCode.OK || res.StatusCode == HttpStatusCode.NotFound,
            $"Expected 200 or 404, got {(int)res.StatusCode}");
    }

    [TestMethod]
    public async Task GetProductStock_RandomGuid_Returns404OrOk()
    {
        using var client = _factory.CreateUnauthenticatedClient();

        var res = await client.GetAsync($"/api/inventory/{Guid.NewGuid()}", TestContext.CancellationToken);

        Assert.IsTrue(
            res.StatusCode == HttpStatusCode.NotFound || res.StatusCode == HttpStatusCode.OK,
            $"Expected 404 for unknown product, got {(int)res.StatusCode}");
    }

    // ── GET /api/inventory/{productId}/available ──────────────────────────────

    [TestMethod]
    public async Task CheckAvailableQuantity_AnonymousRequest_Returns200()
    {
        using var client = _factory.CreateUnauthenticatedClient();

        var res = await client.GetAsync(
            $"/api/inventory/{SeededProductId}/available?quantity=1",
            TestContext.CancellationToken);

        Assert.IsTrue(
            res.StatusCode == HttpStatusCode.OK || res.StatusCode == HttpStatusCode.NotFound,
            $"Expected 200 or 404, got {(int)res.StatusCode}");
    }

    // ── GET /api/inventory/{productId}/history ────────────────────────────────

    [TestMethod]
    public async Task GetInventoryHistory_AsAdmin_Returns200()
    {
        ConditionalTestAuthHandler.CurrentUserRole = "Admin";
        using var client = _factory.CreateAuthenticatedClient();

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
        var payload = new { Quantity = 10, Reason = "correction" };

        var res = await client.PostAsync(
            $"/api/inventory/{SeededProductId}/adjust",
            Json(payload),
            TestContext.CancellationToken);

        Assert.AreEqual(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [TestMethod]
    public async Task AdjustStock_AsCustomer_Returns403()
    {
        ConditionalTestAuthHandler.CurrentUserRole = "Customer";
        using var client = _factory.CreateAuthenticatedClient();
        var payload = new { Quantity = 10, Reason = "correction" };

        var res = await client.PostAsync(
            $"/api/inventory/{SeededProductId}/adjust",
            Json(payload),
            TestContext.CancellationToken);

        Assert.AreEqual(HttpStatusCode.Forbidden, res.StatusCode);
    }

    [TestMethod]
    public async Task AdjustStock_MissingBody_Returns400()
    {
        ConditionalTestAuthHandler.CurrentUserRole = "Admin";
        using var client = _factory.CreateAuthenticatedClient();

        var res = await client.PostAsync(
            $"/api/inventory/{SeededProductId}/adjust",
            new StringContent("{}", Encoding.UTF8, "application/json"),
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
        var payload = new { Quantity = 5, Reason = "restock" };

        var res = await client.PostAsync(
            $"/api/inventory/{SeededProductId}/restock",
            Json(payload),
            TestContext.CancellationToken);

        Assert.AreEqual(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    // ── POST /api/inventory/check-availability ────────────────────────────────

    [TestMethod]
    public async Task CheckStockAvailability_AnonymousValidRequest_Returns200()
    {
        using var client = _factory.CreateUnauthenticatedClient();
        var payload = new { Items = new[] { new { ProductId = SeededProductId, Quantity = 1 } } };

        var res = await client.PostAsync(
            "/api/inventory/check-availability",
            Json(payload),
            TestContext.CancellationToken);

        Assert.AreEqual(HttpStatusCode.OK, res.StatusCode);
    }

    [TestMethod]
    public async Task CheckStockAvailability_EmptyItems_Returns400OrOk()
    {
        using var client = _factory.CreateUnauthenticatedClient();
        var payload = new { Items = Array.Empty<object>() };

        var res = await client.PostAsync(
            "/api/inventory/check-availability",
            Json(payload),
            TestContext.CancellationToken);

        Assert.IsTrue(
            res.StatusCode == HttpStatusCode.BadRequest || res.StatusCode == HttpStatusCode.OK,
            $"Expected 400 or 200, got {(int)res.StatusCode}");
    }

    // ── PUT /api/inventory/{productId} ────────────────────────────────────────

    [TestMethod]
    public async Task UpdateProductStock_Unauthenticated_Returns401()
    {
        using var client = _factory.CreateUnauthenticatedClient();
        var payload = new { Quantity = 100, Reason = "update" };

        var res = await client.PutAsync(
            $"/api/inventory/{SeededProductId}",
            Json(payload),
            TestContext.CancellationToken);

        Assert.AreEqual(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [TestMethod]
    public async Task UpdateProductStock_AsCustomer_Returns403()
    {
        ConditionalTestAuthHandler.CurrentUserRole = "Customer";
        using var client = _factory.CreateAuthenticatedClient();
        var payload = new { Quantity = 100, Reason = "update" };

        var res = await client.PutAsync(
            $"/api/inventory/{SeededProductId}",
            Json(payload),
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
}
```

---

## Run Before Starting Migration

```bash
cd src/backend
dotnet test ECommerce.Tests/ECommerce.Tests.csproj \
    --filter "FullyQualifiedName~InventoryCharacterizationTests"
```

All must pass. If any fail, fix the test (wrong assumption about existing behavior) before starting migration.

---

## Acceptance Criteria

- [ ] `InventoryCharacterizationTests.cs` created in `ECommerce.Tests/Integration/`
- [ ] All characterization tests pass against the EXISTING `InventoryService` (before any migration code is written)
- [ ] Auth requirements confirmed at baseline: Admin-only endpoints return 401/403 for unauthenticated/wrong-role
- [ ] Anonymous endpoints (`GetProductStock`, `CheckAvailableQuantity`, `CheckStockAvailability`) confirmed accessible without token
- [ ] Tests recorded in the Pre-Cutover checklist in `step-4-cutover.md`
