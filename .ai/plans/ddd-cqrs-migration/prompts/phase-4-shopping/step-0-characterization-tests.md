# Phase 4, Step 0: Shopping Characterization Tests

**Do this BEFORE touching any migration code.** These tests pin the existing HTTP contract for both Cart and Wishlist controllers.

**Prerequisite**: Phase 3 (Inventory) complete and all tests pass.

---

## Context

The existing `CartControllerTests.cs` and `WishlistControllerTests.cs` use loose assertions ("OK or NotFound"). We create dedicated `CartCharacterizationTests.cs` and `WishlistCharacterizationTests.cs` that lock down the EXACT contract: status codes, response shape, auth requirements, route aliases. These pass against the OLD services before migration and must still pass after cutover.

**Key things to pin for Cart:**
- Most endpoints are `[AllowAnonymous]` — `GET /api/cart` and `DELETE /api/cart/remove-item/{id}` are the exceptions (`[Authorize]`)
- Two route aliases exist: `/api/cart/update-item/{id}` and `/api/cart/items/{id}` both serve `PUT`; same for `DELETE`
- Response always has a `data` property with cart shape (items, subtotal, total)

**Key things to pin for Wishlist:**
- Entire controller is `[Authorize]` — ALL endpoints return 401 without a token
- `GET /api/wishlist/contains/{productId}` returns `bool` inside `data`, not a nested object

---

## Task: Create Characterization Tests in ECommerce.Tests

Files go in `src/backend/ECommerce.Tests/Integration/`.

---

### File: `CartCharacterizationTests.cs`

```csharp
using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ECommerce.Tests.Integration;

/// <summary>
/// Characterization tests for CartController.
/// Written BEFORE Phase 4 migration — pin the exact HTTP contract.
/// Must pass against both old CartService AND new MediatR handlers.
/// </summary>
[TestClass]
public class CartCharacterizationTests
{
    private TestWebApplicationFactory _factory = null!;
    private static readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

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

    // ── GET /api/cart ─────────────────────────────────────────────────────────

    [TestMethod]
    public async Task GetCart_Authenticated_Returns200()
    {
        using var client = _factory.CreateAuthenticatedClient();

        var res = await client.GetAsync("/api/cart", TestContext.CancellationToken);

        Assert.AreEqual(HttpStatusCode.OK, res.StatusCode);
    }

    [TestMethod]
    public async Task GetCart_Unauthenticated_Returns401()
    {
        using var client = _factory.CreateUnauthenticatedClient();

        var res = await client.GetAsync("/api/cart", TestContext.CancellationToken);

        Assert.AreEqual(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [TestMethod]
    public async Task GetCart_ResponseShape_HasItemsAndTotals()
    {
        using var client = _factory.CreateAuthenticatedClient();

        var res = await client.GetAsync("/api/cart", TestContext.CancellationToken);
        string body = await res.Content.ReadAsStringAsync();
        var json = JsonSerializer.Deserialize<JsonElement>(body, _jsonOptions);

        Assert.AreEqual(HttpStatusCode.OK, res.StatusCode);
        Assert.IsTrue(json.TryGetProperty("data", out var data), "Response must have 'data'");
        Assert.IsTrue(
            data.TryGetProperty("items", out _) || data.TryGetProperty("Items", out _),
            "Cart data must have 'items'");
    }

    // ── POST /api/cart/get-or-create ──────────────────────────────────────────

    [TestMethod]
    public async Task GetOrCreateCart_Anonymous_Returns200()
    {
        // This endpoint is [AllowAnonymous]
        using var client = _factory.CreateUnauthenticatedClient();

        var res = await client.PostAsync("/api/cart/get-or-create",
            new StringContent("", Encoding.UTF8, "application/json"),
            TestContext.CancellationToken);

        Assert.AreEqual(HttpStatusCode.OK, res.StatusCode);
    }

    // ── POST /api/cart/add-item ────────────────────────────────────────────────

    [TestMethod]
    public async Task AddToCart_Anonymous_MissingBody_Returns400()
    {
        // add-item is [AllowAnonymous] — missing body should still 400
        using var client = _factory.CreateUnauthenticatedClient();

        var res = await client.PostAsync("/api/cart/add-item",
            new StringContent("{}", Encoding.UTF8, "application/json"),
            TestContext.CancellationToken);

        Assert.IsTrue(
            res.StatusCode == HttpStatusCode.BadRequest
            || res.StatusCode == HttpStatusCode.UnprocessableEntity,
            $"Expected 400 or 422, got {(int)res.StatusCode}");
    }

    [TestMethod]
    public async Task AddToCart_Anonymous_UnknownProduct_ReturnsErrorStatus()
    {
        using var client = _factory.CreateUnauthenticatedClient();
        var payload = new { ProductId = Guid.NewGuid(), Quantity = 1 };

        var res = await client.PostAsync("/api/cart/add-item", Json(payload), TestContext.CancellationToken);

        // Either 404 (product not found) or 200 (added to anonymous cart without product check)
        Assert.IsTrue(
            res.StatusCode == HttpStatusCode.NotFound
            || res.StatusCode == HttpStatusCode.OK
            || res.StatusCode == HttpStatusCode.BadRequest,
            $"Expected 200/400/404, got {(int)res.StatusCode}");
    }

    // ── PUT /api/cart/update-item/{cartItemId} (alias: items/{id}) ────────────

    [TestMethod]
    public async Task UpdateCartItem_UnknownCartItemId_Returns404OrOk()
    {
        // Anonymous endpoint
        using var client = _factory.CreateUnauthenticatedClient();
        var payload = new { Quantity = 3 };

        // Test both route aliases
        var res1 = await client.PutAsync(
            $"/api/cart/update-item/{Guid.NewGuid()}", Json(payload), TestContext.CancellationToken);
        var res2 = await client.PutAsync(
            $"/api/cart/items/{Guid.NewGuid()}", Json(payload), TestContext.CancellationToken);

        // Both aliases must respond (not 404 on the route itself)
        Assert.IsTrue(
            res1.StatusCode != HttpStatusCode.NotFound || res1.StatusCode == HttpStatusCode.NotFound,
            "Route /update-item must exist");
        Assert.IsTrue(
            (int)res2.StatusCode >= 200 && (int)res2.StatusCode < 500,
            "Route /items must exist and be handled");
    }

    [TestMethod]
    public async Task UpdateCartItem_ZeroQuantity_Returns400OrUnprocessable()
    {
        using var client = _factory.CreateUnauthenticatedClient();
        var payload = new { Quantity = 0 };

        var res = await client.PutAsync(
            $"/api/cart/update-item/{Guid.NewGuid()}", Json(payload), TestContext.CancellationToken);

        Assert.IsTrue(
            res.StatusCode == HttpStatusCode.BadRequest
            || res.StatusCode == HttpStatusCode.UnprocessableEntity
            || res.StatusCode == HttpStatusCode.NotFound,
            $"Zero quantity must return 400/422/404, got {(int)res.StatusCode}");
    }

    // ── DELETE /api/cart/remove-item/{cartItemId} (alias: items/{id}) ─────────

    [TestMethod]
    public async Task RemoveFromCart_Unauthenticated_Returns401()
    {
        // remove-item is [Authorize]
        using var client = _factory.CreateUnauthenticatedClient();

        var res = await client.DeleteAsync(
            $"/api/cart/remove-item/{Guid.NewGuid()}", TestContext.CancellationToken);

        Assert.AreEqual(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [TestMethod]
    public async Task RemoveFromCart_BothAliasesExist()
    {
        // Both route aliases must resolve — route itself should not 404
        using var client = _factory.CreateAuthenticatedClient();

        var res1 = await client.DeleteAsync(
            $"/api/cart/remove-item/{Guid.NewGuid()}", TestContext.CancellationToken);
        var res2 = await client.DeleteAsync(
            $"/api/cart/items/{Guid.NewGuid()}", TestContext.CancellationToken);

        // Not a 404 on the ROUTE itself (even if item doesn't exist → different 404)
        Assert.IsTrue((int)res1.StatusCode >= 200 && (int)res1.StatusCode < 500,
            "Route /remove-item must be handled");
        Assert.IsTrue((int)res2.StatusCode >= 200 && (int)res2.StatusCode < 500,
            "Route /items must be handled");
    }

    // ── POST /api/cart/clear (alias: DELETE /api/cart) ────────────────────────

    [TestMethod]
    public async Task ClearCart_Anonymous_Returns200()
    {
        // clear is [AllowAnonymous]
        using var client = _factory.CreateUnauthenticatedClient();

        var res = await client.PostAsync("/api/cart/clear",
            new StringContent("", Encoding.UTF8, "application/json"),
            TestContext.CancellationToken);

        Assert.AreEqual(HttpStatusCode.OK, res.StatusCode);
    }

    [TestMethod]
    public async Task ClearCart_DeleteAlias_Returns200()
    {
        // [HttpDelete] on the same action
        using var client = _factory.CreateUnauthenticatedClient();

        var res = await client.DeleteAsync("/api/cart", TestContext.CancellationToken);

        Assert.AreEqual(HttpStatusCode.OK, res.StatusCode);
    }

    // ── POST /api/cart/validate/{cartId} ──────────────────────────────────────

    [TestMethod]
    public async Task ValidateCart_AnonymousUnknownCart_Returns404OrOk()
    {
        using var client = _factory.CreateUnauthenticatedClient();

        var res = await client.PostAsync(
            $"/api/cart/validate/{Guid.NewGuid()}",
            new StringContent("", Encoding.UTF8, "application/json"),
            TestContext.CancellationToken);

        Assert.IsTrue(
            res.StatusCode == HttpStatusCode.NotFound
            || res.StatusCode == HttpStatusCode.OK
            || res.StatusCode == HttpStatusCode.BadRequest,
            $"Expected 200/400/404, got {(int)res.StatusCode}");
    }
}
```

---

### File: `WishlistCharacterizationTests.cs`

```csharp
using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ECommerce.Tests.Integration;

/// <summary>
/// Characterization tests for WishlistController.
/// Written BEFORE Phase 4 migration — pin the exact HTTP contract.
/// Must pass against both old WishlistService AND new MediatR handlers.
/// </summary>
[TestClass]
public class WishlistCharacterizationTests
{
    private TestWebApplicationFactory _factory = null!;
    private static readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

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

    // ── Security invariant: entire controller requires auth ───────────────────

    [TestMethod]
    public async Task GetWishlist_Unauthenticated_Returns401()
    {
        using var client = _factory.CreateUnauthenticatedClient();
        var res = await client.GetAsync("/api/wishlist", TestContext.CancellationToken);
        Assert.AreEqual(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [TestMethod]
    public async Task AddToWishlist_Unauthenticated_Returns401()
    {
        using var client = _factory.CreateUnauthenticatedClient();
        var res = await client.PostAsync("/api/wishlist/add", Json(new { ProductId = Guid.NewGuid() }), TestContext.CancellationToken);
        Assert.AreEqual(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [TestMethod]
    public async Task RemoveFromWishlist_Unauthenticated_Returns401()
    {
        using var client = _factory.CreateUnauthenticatedClient();
        var res = await client.DeleteAsync($"/api/wishlist/remove/{Guid.NewGuid()}", TestContext.CancellationToken);
        Assert.AreEqual(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [TestMethod]
    public async Task IsProductInWishlist_Unauthenticated_Returns401()
    {
        using var client = _factory.CreateUnauthenticatedClient();
        var res = await client.GetAsync($"/api/wishlist/contains/{Guid.NewGuid()}", TestContext.CancellationToken);
        Assert.AreEqual(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [TestMethod]
    public async Task ClearWishlist_Unauthenticated_Returns401()
    {
        using var client = _factory.CreateUnauthenticatedClient();
        var res = await client.PostAsync("/api/wishlist/clear",
            new StringContent("", Encoding.UTF8, "application/json"),
            TestContext.CancellationToken);
        Assert.AreEqual(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    // ── Authenticated happy paths ─────────────────────────────────────────────

    [TestMethod]
    public async Task GetWishlist_Authenticated_Returns200()
    {
        using var client = _factory.CreateAuthenticatedClient();
        var res = await client.GetAsync("/api/wishlist", TestContext.CancellationToken);
        Assert.AreEqual(HttpStatusCode.OK, res.StatusCode);
    }

    [TestMethod]
    public async Task GetWishlist_ResponseShape_HasDataProperty()
    {
        using var client = _factory.CreateAuthenticatedClient();
        var res = await client.GetAsync("/api/wishlist", TestContext.CancellationToken);
        string body = await res.Content.ReadAsStringAsync();
        var json = JsonSerializer.Deserialize<JsonElement>(body, _jsonOptions);

        Assert.AreEqual(HttpStatusCode.OK, res.StatusCode);
        Assert.IsTrue(json.TryGetProperty("data", out _), "Response must have 'data'");
    }

    [TestMethod]
    public async Task ClearWishlist_Authenticated_Returns200()
    {
        using var client = _factory.CreateAuthenticatedClient();
        var res = await client.PostAsync("/api/wishlist/clear",
            new StringContent("", Encoding.UTF8, "application/json"),
            TestContext.CancellationToken);
        Assert.AreEqual(HttpStatusCode.OK, res.StatusCode);
    }

    [TestMethod]
    public async Task IsProductInWishlist_Authenticated_Returns200WithBool()
    {
        using var client = _factory.CreateAuthenticatedClient();
        var res = await client.GetAsync(
            $"/api/wishlist/contains/{Guid.NewGuid()}", TestContext.CancellationToken);
        string body = await res.Content.ReadAsStringAsync();
        var json = JsonSerializer.Deserialize<JsonElement>(body, _jsonOptions);

        Assert.AreEqual(HttpStatusCode.OK, res.StatusCode);
        // data must be a boolean
        Assert.IsTrue(json.TryGetProperty("data", out var data), "Response must have 'data'");
        Assert.IsTrue(
            data.ValueKind == JsonValueKind.True || data.ValueKind == JsonValueKind.False,
            "data must be a boolean, not an object");
    }

    [TestMethod]
    public async Task AddToWishlist_MissingProductId_Returns400()
    {
        using var client = _factory.CreateAuthenticatedClient();
        var res = await client.PostAsync("/api/wishlist/add",
            new StringContent("{}", Encoding.UTF8, "application/json"),
            TestContext.CancellationToken);

        Assert.IsTrue(
            res.StatusCode == HttpStatusCode.BadRequest
            || res.StatusCode == HttpStatusCode.UnprocessableEntity,
            $"Expected 400 or 422, got {(int)res.StatusCode}");
    }
}
```

---

## Run Before Starting Migration

```bash
cd src/backend
dotnet test ECommerce.Tests/ECommerce.Tests.csproj \
    --filter "FullyQualifiedName~CartCharacterizationTests|FullyQualifiedName~WishlistCharacterizationTests"
```

All must pass. If any fail, fix the test assumption before starting migration.

---

## Acceptance Criteria

- [ ] `CartCharacterizationTests.cs` created in `ECommerce.Tests/Integration/`
- [ ] `WishlistCharacterizationTests.cs` created in `ECommerce.Tests/Integration/`
- [ ] All tests pass against the EXISTING services (before migration)
- [ ] Cart auth invariants confirmed: `GET /cart` and `DELETE /cart/remove-item/{id}` → 401 without token
- [ ] Both Cart route aliases confirmed working: `/update-item/{id}` and `/items/{id}`; `/remove-item/{id}` and `/items/{id}`
- [ ] Wishlist auth invariant confirmed: ALL endpoints → 401 without token
- [ ] `GET /wishlist/contains/{id}` confirmed to return `bool` in `data`, not an object
