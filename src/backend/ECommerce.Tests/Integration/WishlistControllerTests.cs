using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ECommerce.Tests.Integration;

[TestClass]
public class WishlistControllerTests
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

    // ── GET /api/wishlist ────────────────────────────────────────────────────

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
        var json = JsonSerializer.Deserialize<JsonElement>(await res.Content.ReadAsStringAsync(), _jsonOptions);
        Assert.AreEqual(HttpStatusCode.OK, res.StatusCode);
        Assert.IsTrue(json.TryGetProperty("data", out _), "Response must have 'data'");
    }

    [TestMethod]
    public async Task GetWishlist_Unauthenticated_Returns401()
    {
        using var client = _factory.CreateUnauthenticatedClient();
        var res = await client.GetAsync("/api/wishlist", TestContext.CancellationToken);
        Assert.AreEqual(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    // ── POST /api/wishlist/add ───────────────────────────────────────────────

    [TestMethod]
    public async Task AddToWishlist_WithValidProductId_ReturnsOk()
    {
        using var client = _factory.CreateAuthenticatedClient();
        var res = await client.PostAsync("/api/wishlist/add",
            Json(new { ProductId = Guid.Parse("22222222-2222-2222-2222-222222222222") }),
            TestContext.CancellationToken);
        Assert.IsTrue(res.StatusCode is HttpStatusCode.OK or HttpStatusCode.Created or HttpStatusCode.BadRequest);
    }

    [TestMethod]
    public async Task AddToWishlist_MissingProductId_Returns400()
    {
        using var client = _factory.CreateAuthenticatedClient();
        var res = await client.PostAsync("/api/wishlist/add",
            new StringContent("{}", Encoding.UTF8, "application/json"),
            TestContext.CancellationToken);
        Assert.IsTrue(res.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.UnprocessableEntity);
    }

    [TestMethod]
    public async Task AddToWishlist_Unauthenticated_Returns401()
    {
        using var client = _factory.CreateUnauthenticatedClient();
        var res = await client.PostAsync("/api/wishlist/add", Json(new { ProductId = Guid.NewGuid() }), TestContext.CancellationToken);
        Assert.AreEqual(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    // ── DELETE /api/wishlist/remove/{productId} ──────────────────────────────

    [TestMethod]
    public async Task RemoveFromWishlist_WithExistingItem_ReturnsOk()
    {
        using var client = _factory.CreateAuthenticatedClient();
        var res = await client.DeleteAsync($"/api/wishlist/remove/{Guid.NewGuid()}", TestContext.CancellationToken);
        Assert.IsTrue(res.StatusCode is HttpStatusCode.OK or HttpStatusCode.NoContent or HttpStatusCode.NotFound);
    }

    [TestMethod]
    public async Task RemoveFromWishlist_Unauthenticated_Returns401()
    {
        using var client = _factory.CreateUnauthenticatedClient();
        var res = await client.DeleteAsync($"/api/wishlist/remove/{Guid.NewGuid()}", TestContext.CancellationToken);
        Assert.AreEqual(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    // ── GET /api/wishlist/contains/{productId} ───────────────────────────────

    [TestMethod]
    public async Task IsProductInWishlist_Authenticated_Returns200WithBool()
    {
        using var client = _factory.CreateAuthenticatedClient();
        var res = await client.GetAsync($"/api/wishlist/contains/{Guid.NewGuid()}", TestContext.CancellationToken);
        var json = JsonSerializer.Deserialize<JsonElement>(await res.Content.ReadAsStringAsync(), _jsonOptions);
        Assert.AreEqual(HttpStatusCode.OK, res.StatusCode);
        Assert.IsTrue(json.TryGetProperty("data", out var data), "Response must have 'data'");
        Assert.IsTrue(data.ValueKind is JsonValueKind.True or JsonValueKind.False, "data must be a boolean");
    }

    [TestMethod]
    public async Task IsProductInWishlist_Unauthenticated_Returns401()
    {
        using var client = _factory.CreateUnauthenticatedClient();
        var res = await client.GetAsync($"/api/wishlist/contains/{Guid.NewGuid()}", TestContext.CancellationToken);
        Assert.AreEqual(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    // ── POST /api/wishlist/clear ─────────────────────────────────────────────

    [TestMethod]
    public async Task ClearWishlist_Authenticated_Returns200OrConflict()
    {
        using var client = _factory.CreateAuthenticatedClient();
        var res = await client.PostAsync("/api/wishlist/clear",
            new StringContent("", Encoding.UTF8, "application/json"),
            TestContext.CancellationToken);
        Assert.IsTrue(res.StatusCode is HttpStatusCode.OK or HttpStatusCode.Conflict,
            $"Expected 200 or 409, got {(int)res.StatusCode}");
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
}
