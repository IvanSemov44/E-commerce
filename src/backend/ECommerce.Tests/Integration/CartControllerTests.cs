using System.Net;
using System.Text;
using System.Text.Json;
using Shouldly;

namespace ECommerce.Tests.Integration;

[TestClass]
public class CartControllerTests
{
    private TestWebApplicationFactory _factory = null!;
    private static readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };
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

    private static async Task<JsonElement> ReadData(HttpResponseMessage res) =>
        JsonSerializer.Deserialize<JsonElement>(await res.Content.ReadAsStringAsync(), _jsonOptions)
            .GetProperty("data");

    // ── GET /api/cart ─────────────────────────────────────────────────────────

    [TestMethod]
    public async Task GetCart_Authenticated_NoCartSeeded_Returns404()
    {
        using var client = _factory.CreateAuthenticatedClient();

        var res = await client.GetAsync("/api/cart", TestContext.CancellationToken);

        res.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [TestMethod]
    public async Task GetCart_AfterAddingItem_Returns200WithItem()
    {
        using var client = _factory.CreateAuthenticatedClient();

        await client.PostAsync("/api/cart/add-item",
            Json(new { ProductId = SeededProductId, Quantity = 2 }),
            TestContext.CancellationToken);

        var res = await client.GetAsync("/api/cart", TestContext.CancellationToken);

        res.StatusCode.ShouldBe(HttpStatusCode.OK);
        var data = await ReadData(res);
        var items = data.GetProperty("items");
        items.GetArrayLength().ShouldBeGreaterThan(0);
        items[0].GetProperty("productId").GetGuid().ShouldBe(SeededProductId);
        items[0].GetProperty("quantity").GetInt32().ShouldBe(2);
    }

    [TestMethod]
    public async Task GetCart_Unauthenticated_Returns401()
    {
        using var client = _factory.CreateUnauthenticatedClient();

        var res = await client.GetAsync("/api/cart", TestContext.CancellationToken);

        res.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    // ── POST /api/cart/get-or-create ──────────────────────────────────────────

    [TestMethod]
    public async Task GetOrCreateCart_Authenticated_Returns200WithEmptyCart()
    {
        using var client = _factory.CreateAuthenticatedClient();

        var res = await client.PostAsync("/api/cart/get-or-create", null, TestContext.CancellationToken);

        res.StatusCode.ShouldBe(HttpStatusCode.OK);
        var data = await ReadData(res);
        data.GetProperty("items").GetArrayLength().ShouldBe(0);
    }

    [TestMethod]
    public async Task GetOrCreateCart_Anonymous_Returns200()
    {
        using var client = _factory.CreateUnauthenticatedClient();

        var res = await client.PostAsync("/api/cart/get-or-create", null, TestContext.CancellationToken);

        res.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    // ── POST /api/cart/add-item ───────────────────────────────────────────────

    [TestMethod]
    public async Task AddItemToCart_SeededProduct_Returns204()
    {
        using var client = _factory.CreateAuthenticatedClient();

        var res = await client.PostAsync("/api/cart/add-item",
            Json(new { ProductId = SeededProductId, Quantity = 1 }),
            TestContext.CancellationToken);

        res.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    [TestMethod]
    public async Task AddItemToCart_ZeroQuantity_Returns422()
    {
        using var client = _factory.CreateAuthenticatedClient();

        var res = await client.PostAsync("/api/cart/add-item",
            Json(new { ProductId = SeededProductId, Quantity = 0 }),
            TestContext.CancellationToken);

        res.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
    }

    [TestMethod]
    public async Task AddItemToCart_NegativeQuantity_Returns422()
    {
        using var client = _factory.CreateAuthenticatedClient();

        var res = await client.PostAsync("/api/cart/add-item",
            Json(new { ProductId = SeededProductId, Quantity = -3 }),
            TestContext.CancellationToken);

        res.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
    }

    [TestMethod]
    public async Task AddItemToCart_NonexistentProduct_Returns404()
    {
        using var client = _factory.CreateAuthenticatedClient();

        var res = await client.PostAsync("/api/cart/add-item",
            Json(new { ProductId = Guid.NewGuid(), Quantity = 1 }),
            TestContext.CancellationToken);

        res.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    // ── PUT /api/cart/items/{cartItemId} ──────────────────────────────────────

    [TestMethod]
    public async Task UpdateCartItem_NonexistentItem_Returns404()
    {
        using var client = _factory.CreateAuthenticatedClient();

        var res = await client.PutAsync($"/api/cart/items/{Guid.NewGuid()}",
            Json(new { Quantity = 3 }),
            TestContext.CancellationToken);

        res.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [TestMethod]
    public async Task UpdateCartItem_ZeroQuantity_Returns422()
    {
        using var client = _factory.CreateAuthenticatedClient();

        var res = await client.PutAsync($"/api/cart/items/{Guid.NewGuid()}",
            Json(new { Quantity = 0 }),
            TestContext.CancellationToken);

        res.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
    }

    [TestMethod]
    public async Task UpdateCartItem_AfterAdd_Returns204()
    {
        using var client = _factory.CreateAuthenticatedClient();

        await client.PostAsync("/api/cart/add-item",
            Json(new { ProductId = SeededProductId, Quantity = 1 }),
            TestContext.CancellationToken);

        var cartRes = await client.GetAsync("/api/cart", TestContext.CancellationToken);
        var cartData = await ReadData(cartRes);
        var cartItemId = cartData.GetProperty("items")[0].GetProperty("id").GetGuid();

        var res = await client.PutAsync($"/api/cart/items/{cartItemId}",
            Json(new { Quantity = 5 }),
            TestContext.CancellationToken);

        res.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    // ── DELETE /api/cart/items/{cartItemId} ───────────────────────────────────

    [TestMethod]
    public async Task RemoveCartItem_NonexistentItem_Returns404()
    {
        using var client = _factory.CreateAuthenticatedClient();

        var res = await client.DeleteAsync($"/api/cart/items/{Guid.NewGuid()}",
            TestContext.CancellationToken);

        res.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [TestMethod]
    public async Task RemoveCartItem_AfterAdd_Returns204()
    {
        using var client = _factory.CreateAuthenticatedClient();

        await client.PostAsync("/api/cart/add-item",
            Json(new { ProductId = SeededProductId, Quantity = 1 }),
            TestContext.CancellationToken);

        var cartRes = await client.GetAsync("/api/cart", TestContext.CancellationToken);
        var cartData = await ReadData(cartRes);
        var cartItemId = cartData.GetProperty("items")[0].GetProperty("id").GetGuid();

        var res = await client.DeleteAsync($"/api/cart/items/{cartItemId}",
            TestContext.CancellationToken);

        res.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    // ── DELETE /api/cart ──────────────────────────────────────────────────────

    [TestMethod]
    public async Task ClearCart_Always_Returns204()
    {
        using var client = _factory.CreateAuthenticatedClient();

        var res = await client.DeleteAsync("/api/cart", TestContext.CancellationToken);

        res.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }
}
