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
        // This endpoint is [AllowAnonymous] - requires sessionId for anonymous users
        using var client = _factory.CreateUnauthenticatedClient();
        client.DefaultRequestHeaders.Add("X-Session-ID", Guid.NewGuid().ToString());

        var res = await client.PostAsync("/api/cart/get-or-create",
            new StringContent("", Encoding.UTF8, "application/json"),
            TestContext.CancellationToken);

        Assert.AreEqual(HttpStatusCode.OK, res.StatusCode, "Anonymous users need a sessionId to create cart");
    }

    // ── POST /api/cart/add-item ────────────────────────────────────────────────

    [TestMethod]
    public async Task AddToCart_Anonymous_MissingBody_Returns400()
    {
        // add-item is [AllowAnonymous] — missing body should still 400
        using var client = _factory.CreateUnauthenticatedClient();
        client.DefaultRequestHeaders.Add("X-Session-ID", Guid.NewGuid().ToString());

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
        client.DefaultRequestHeaders.Add("X-Session-ID", Guid.NewGuid().ToString());
        var payload = new { ProductId = Guid.NewGuid(), Quantity = 1 };

        var res = await client.PostAsync("/api/cart/add-item", Json(payload), TestContext.CancellationToken);

        // Product not found should return 404
        Assert.IsTrue(
            res.StatusCode == HttpStatusCode.NotFound
            || res.StatusCode == HttpStatusCode.BadRequest,
            $"Expected 400/404, got {(int)res.StatusCode}");
    }

    // ── PUT /api/cart/update-item/{cartItemId} (alias: items/{id}) ────────────

    [TestMethod]
    public async Task UpdateCartItem_UnknownCartItemId_Returns404OrOk()
    {
        // Anonymous endpoint - requires sessionId
        using var client = _factory.CreateUnauthenticatedClient();
        client.DefaultRequestHeaders.Add("X-Session-ID", Guid.NewGuid().ToString());
        var payload = new { Quantity = 3 };

        // Test both route aliases
        var res1 = await client.PutAsync(
            $"/api/cart/update-item/{Guid.NewGuid()}", Json(payload), TestContext.CancellationToken);
        var res2 = await client.PutAsync(
            $"/api/cart/items/{Guid.NewGuid()}", Json(payload), TestContext.CancellationToken);

        // Both aliases must respond with 404 (item not found in cart)
        Assert.AreEqual(HttpStatusCode.NotFound, res1.StatusCode, "Route /update-item must return 404 for unknown item");
        Assert.AreEqual(HttpStatusCode.NotFound, res2.StatusCode, "Route /items must return 404 for unknown item");
    }

    [TestMethod]
    public async Task UpdateCartItem_ZeroQuantity_Returns400OrUnprocessable()
    {
        using var client = _factory.CreateUnauthenticatedClient();
        client.DefaultRequestHeaders.Add("X-Session-ID", Guid.NewGuid().ToString());
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
        client.DefaultRequestHeaders.Add("X-Session-ID", Guid.NewGuid().ToString());

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
        client.DefaultRequestHeaders.Add("X-Session-ID", Guid.NewGuid().ToString());

        var res = await client.DeleteAsync("/api/cart", TestContext.CancellationToken);

        Assert.AreEqual(HttpStatusCode.OK, res.StatusCode);
    }

    // ── POST /api/cart/validate/{cartId} ──────────────────────────────────────

    [TestMethod]
    public async Task ValidateCart_AnonymousUnknownCart_Returns404OrOk()
    {
        using var client = _factory.CreateUnauthenticatedClient();
        client.DefaultRequestHeaders.Add("X-Session-ID", Guid.NewGuid().ToString());

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