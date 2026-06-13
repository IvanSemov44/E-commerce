using System.Net;
using System.Text;
using System.Text.Json;

namespace ECommerce.Tests.Integration;

[TestClass]
public class WishlistControllerTests
{
    private static readonly TestWebApplicationFactory _factory = SharedTestInfrastructure.Factory;
    private static readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };
    private static readonly Guid _seededProductId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    public TestContext TestContext { get; set; } = null!;

    private static StringContent Json(object dto) =>
        new(JsonSerializer.Serialize(dto), Encoding.UTF8, "application/json");

    private static async Task<JsonElement> ReadData(HttpResponseMessage res) =>
        JsonSerializer.Deserialize<JsonElement>(await res.Content.ReadAsStringAsync(), _jsonOptions)
            .GetProperty("data");

    // ── GET /api/wishlist ────────────────────────────────────────────────────

    [TestMethod]
    public async Task GetWishlist_Authenticated_Returns200WithEmptyList()
    {
        using var client = _factory.CreateFreshAuthenticatedClient();

        var res = await client.GetAsync("/api/wishlist", TestContext.CancellationToken);

        res.StatusCode.ShouldBe(HttpStatusCode.OK);
        var data = await ReadData(res);
        data.GetProperty("productIds").GetArrayLength().ShouldBe(0);
    }

    [TestMethod]
    public async Task GetWishlist_Unauthenticated_Returns401()
    {
        using var client = _factory.CreateUnauthenticatedClient();

        var res = await client.GetAsync("/api/wishlist", TestContext.CancellationToken);

        res.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    // ── POST /api/wishlist/add ───────────────────────────────────────────────

    [TestMethod]
    public async Task AddToWishlist_SeededProduct_Returns204()
    {
        using var client = _factory.CreateFreshAuthenticatedClient();

        var res = await client.PostAsync("/api/wishlist/add",
            Json(new { ProductId = _seededProductId }),
            TestContext.CancellationToken);

        res.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    [TestMethod]
    public async Task AddToWishlist_ThenGetWishlist_ContainsProduct()
    {
        using var client = _factory.CreateFreshAuthenticatedClient();

        await client.PostAsync("/api/wishlist/add",
            Json(new { ProductId = _seededProductId }),
            TestContext.CancellationToken);

        var res = await client.GetAsync("/api/wishlist", TestContext.CancellationToken);

        res.StatusCode.ShouldBe(HttpStatusCode.OK);
        var data = await ReadData(res);
        var productIds = data.GetProperty("productIds");
        productIds.GetArrayLength().ShouldBe(1);
        productIds[0].GetGuid().ShouldBe(_seededProductId);
    }

    [TestMethod]
    public async Task AddToWishlist_Unauthenticated_Returns401()
    {
        using var client = _factory.CreateUnauthenticatedClient();

        var res = await client.PostAsync("/api/wishlist/add",
            Json(new { ProductId = _seededProductId }),
            TestContext.CancellationToken);

        res.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    // ── DELETE /api/wishlist/remove/{productId} ──────────────────────────────

    [TestMethod]
    public async Task RemoveFromWishlist_AnyProductId_Returns204()
    {
        using var client = _factory.CreateFreshAuthenticatedClient();

        var res = await client.DeleteAsync($"/api/wishlist/remove/{Guid.NewGuid()}",
            TestContext.CancellationToken);

        res.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    [TestMethod]
    public async Task RemoveFromWishlist_AfterAdd_ProductNoLongerInList()
    {
        using var client = _factory.CreateFreshAuthenticatedClient();

        await client.PostAsync("/api/wishlist/add",
            Json(new { ProductId = _seededProductId }),
            TestContext.CancellationToken);

        await client.DeleteAsync($"/api/wishlist/remove/{_seededProductId}",
            TestContext.CancellationToken);

        var res = await client.GetAsync("/api/wishlist", TestContext.CancellationToken);
        var data = await ReadData(res);
        data.GetProperty("productIds").GetArrayLength().ShouldBe(0);
    }

    [TestMethod]
    public async Task RemoveFromWishlist_Unauthenticated_Returns401()
    {
        using var client = _factory.CreateUnauthenticatedClient();

        var res = await client.DeleteAsync($"/api/wishlist/remove/{Guid.NewGuid()}",
            TestContext.CancellationToken);

        res.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    // ── GET /api/wishlist/contains/{productId} ───────────────────────────────

    [TestMethod]
    public async Task IsProductInWishlist_ProductNotAdded_ReturnsFalse()
    {
        using var client = _factory.CreateFreshAuthenticatedClient();

        var res = await client.GetAsync($"/api/wishlist/contains/{Guid.NewGuid()}",
            TestContext.CancellationToken);

        res.StatusCode.ShouldBe(HttpStatusCode.OK);
        var data = await ReadData(res);
        data.GetBoolean().ShouldBeFalse();
    }

    [TestMethod]
    public async Task IsProductInWishlist_AfterAdd_ReturnsTrue()
    {
        using var client = _factory.CreateFreshAuthenticatedClient();

        await client.PostAsync("/api/wishlist/add",
            Json(new { ProductId = _seededProductId }),
            TestContext.CancellationToken);

        var res = await client.GetAsync($"/api/wishlist/contains/{_seededProductId}",
            TestContext.CancellationToken);

        res.StatusCode.ShouldBe(HttpStatusCode.OK);
        var data = await ReadData(res);
        data.GetBoolean().ShouldBeTrue();
    }

    [TestMethod]
    public async Task IsProductInWishlist_Unauthenticated_Returns401()
    {
        using var client = _factory.CreateUnauthenticatedClient();

        var res = await client.GetAsync($"/api/wishlist/contains/{Guid.NewGuid()}",
            TestContext.CancellationToken);

        res.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    // ── POST /api/wishlist/clear ─────────────────────────────────────────────

    [TestMethod]
    public async Task ClearWishlist_Authenticated_Returns204()
    {
        using var client = _factory.CreateFreshAuthenticatedClient();

        var res = await client.PostAsync("/api/wishlist/clear", null, TestContext.CancellationToken);

        res.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    [TestMethod]
    public async Task ClearWishlist_AfterAdd_WishlistIsEmpty()
    {
        using var client = _factory.CreateFreshAuthenticatedClient();

        await client.PostAsync("/api/wishlist/add",
            Json(new { ProductId = _seededProductId }),
            TestContext.CancellationToken);

        await client.PostAsync("/api/wishlist/clear", null, TestContext.CancellationToken);

        var res = await client.GetAsync("/api/wishlist", TestContext.CancellationToken);
        var data = await ReadData(res);
        data.GetProperty("productIds").GetArrayLength().ShouldBe(0);
    }

    [TestMethod]
    public async Task ClearWishlist_Unauthenticated_Returns401()
    {
        using var client = _factory.CreateUnauthenticatedClient();

        var res = await client.PostAsync("/api/wishlist/clear", null, TestContext.CancellationToken);

        res.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }
}
