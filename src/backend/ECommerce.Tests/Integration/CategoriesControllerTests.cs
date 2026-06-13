using System.Net;
using System.Text;
using System.Text.Json;
using ECommerce.Contracts.DTOs.Common;
using ECommerce.SharedKernel.Constants;

namespace ECommerce.Tests.Integration;

[TestClass]
public class CategoriesControllerTests
{
    private static readonly TestWebApplicationFactory _factory = SharedTestInfrastructure.Factory;
    private static readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

    public TestContext TestContext { get; set; } = null!;

    private static StringContent Serialize(object dto) =>
        new(JsonSerializer.Serialize(dto), Encoding.UTF8, "application/json");

    private static async Task<ApiResponse<T>?> Deserialize<T>(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        string content = await response.Content.ReadAsStringAsync(cancellationToken);
        return JsonSerializer.Deserialize<ApiResponse<T>>(content, _jsonOptions);
    }

    private static Guid ExtractIdFromLocation(HttpResponseMessage response, string expectedPathSegment)
    {
        Assert.IsNotNull(response.Headers.Location, "Response must include Location header");
        string location = response.Headers.Location!.ToString();
        Assert.IsTrue(location.Contains(expectedPathSegment), $"Location '{location}' should contain '{expectedPathSegment}'");
        return Guid.Parse(location[(location.LastIndexOf('/') + 1)..]);
    }

    // ── GET /api/categories ──────────────────────────────────────────────────

    [TestMethod]
    public async Task GetCategories_Returns200AndData()
    {
        using var client = _factory.CreateUnauthenticatedClient();

        var res = await client.GetAsync("/api/categories", TestContext.CancellationToken);

        Assert.AreEqual(HttpStatusCode.OK, res.StatusCode);
        string body = await res.Content.ReadAsStringAsync();
        var json = JsonSerializer.Deserialize<JsonElement>(body, _jsonOptions);
        Assert.IsTrue(json.TryGetProperty("data", out _));
    }

    [TestMethod]
    public async Task GetCategoryById_Existing_Returns200()
    {
        using var client = _factory.CreateAdminClientNoRedirect();
        var create = new { Name = "CatForGet", Slug = "cat-get-" + Guid.NewGuid().ToString()[..8] };
        var cRes = await client.PostAsync("/api/categories", Serialize(create), TestContext.CancellationToken);
        Assert.AreEqual(HttpStatusCode.Created, cRes.StatusCode);
        var id = ExtractIdFromLocation(cRes, "/api/categories/");

        using var anon = _factory.CreateUnauthenticatedClient();
        var res = await anon.GetAsync($"/api/categories/{id}", TestContext.CancellationToken);

        Assert.AreEqual(HttpStatusCode.OK, res.StatusCode);
    }

    [TestMethod]
    public async Task GetCategoryById_Nonexistent_Returns404()
    {
        using var client = _factory.CreateUnauthenticatedClient();

        var res = await client.GetAsync($"/api/categories/{Guid.NewGuid()}", TestContext.CancellationToken);

        Assert.AreEqual(HttpStatusCode.NotFound, res.StatusCode);
    }

    [TestMethod]
    public async Task GetCategoryBySlug_Existing_Returns200()
    {
        using var client = _factory.CreateAdminClientNoRedirect();
        string slug = "cat-slug-" + Guid.NewGuid().ToString()[..8];
        var cRes = await client.PostAsync("/api/categories", Serialize(new { Name = "SlugCat", Slug = slug }), TestContext.CancellationToken);
        Assert.AreEqual(HttpStatusCode.Created, cRes.StatusCode);

        using var anon = _factory.CreateUnauthenticatedClient();
        var res = await anon.GetAsync($"/api/categories/slug/{slug}", TestContext.CancellationToken);

        Assert.AreEqual(HttpStatusCode.OK, res.StatusCode);
    }

    [TestMethod]
    public async Task GetCategoryBySlug_Nonexistent_Returns404()
    {
        using var client = _factory.CreateUnauthenticatedClient();

        var res = await client.GetAsync($"/api/categories/slug/{Guid.NewGuid()}", TestContext.CancellationToken);

        Assert.AreEqual(HttpStatusCode.NotFound, res.StatusCode);
    }

    // ── POST /api/categories ─────────────────────────────────────────────────

    [TestMethod]
    public async Task PostCategory_AdminValid_Returns302WithLocation()
    {
        using var client = _factory.CreateAdminClientNoRedirect();
        string slug = "cat-create-" + Guid.NewGuid().ToString()[..8];

        var res = await client.PostAsync("/api/categories", Serialize(new { Name = "CreateCat", Slug = slug }), TestContext.CancellationToken);

        Assert.AreEqual(HttpStatusCode.Created, res.StatusCode);
        Assert.AreNotEqual(Guid.Empty, ExtractIdFromLocation(res, "/api/categories/"));
    }

    [TestMethod]
    public async Task PostCategory_AdminMissingName_Returns400()
    {
        using var client = _factory.CreateAdminClientNoRedirect();

        var res = await client.PostAsync("/api/categories",
            Serialize(new { Slug = "no-name-cat-" + Guid.NewGuid().ToString()[..8] }),
            TestContext.CancellationToken);

        Assert.AreEqual(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [TestMethod]
    public async Task PostCategory_DuplicateSlug_Returns422WithCode()
    {
        using var client = _factory.CreateAdminClientNoRedirect();
        string slug = "dup-cat-" + Guid.NewGuid().ToString()[..8];

        var r1 = await client.PostAsync("/api/categories", Serialize(new { Name = "Dup1", Slug = slug }), TestContext.CancellationToken);
        Assert.AreEqual(HttpStatusCode.Created, r1.StatusCode);

        var r2 = await client.PostAsync("/api/categories", Serialize(new { Name = "Dup1", Slug = slug }), TestContext.CancellationToken);

        Assert.AreEqual((HttpStatusCode)422, r2.StatusCode);
        var api = await Deserialize<object>(r2, TestContext.CancellationToken);
        Assert.IsFalse(api!.Success);
        Assert.AreEqual(ErrorCodes.DuplicateCategorySlug, api.ErrorDetails!.Code);
    }

    [TestMethod]
    public async Task PostCategory_Unauthenticated_Returns401()
    {
        using var client = _factory.CreateUnauthenticatedClient();

        var res = await client.PostAsync("/api/categories",
            Serialize(new { Name = "U", Slug = "u-" + Guid.NewGuid().ToString()[..8] }),
            TestContext.CancellationToken);

        Assert.AreEqual(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [TestMethod]
    public async Task PostCategory_CustomerRole_Returns403()
    {
        using var client = _factory.CreateAuthenticatedClient();

        var res = await client.PostAsync("/api/categories",
            Serialize(new { Name = "U", Slug = "u-cust-" + Guid.NewGuid().ToString()[..8] }),
            TestContext.CancellationToken);

        Assert.AreEqual(HttpStatusCode.Forbidden, res.StatusCode);
    }

    // ── PUT /api/categories/{id} ─────────────────────────────────────────────

    [TestMethod]
    public async Task PutCategory_AdminExisting_Returns302()
    {
        using var client = _factory.CreateAdminClientNoRedirect();
        var cRes = await client.PostAsync("/api/categories",
            Serialize(new { Name = "PutCat", Slug = "put-cat-" + Guid.NewGuid().ToString()[..8] }),
            TestContext.CancellationToken);
        Assert.AreEqual(HttpStatusCode.Created, cRes.StatusCode);
        var id = ExtractIdFromLocation(cRes, "/api/categories/");

        var res = await client.PutAsync($"/api/categories/{id}",
            Serialize(new { Name = "PutUpdated", Slug = "put-up-" + Guid.NewGuid().ToString()[..8] }),
            TestContext.CancellationToken);

        Assert.AreEqual(HttpStatusCode.OK, res.StatusCode);
        var body = JsonSerializer.Deserialize<JsonElement>(await res.Content.ReadAsStringAsync(TestContext.CancellationToken), _jsonOptions);
        Assert.AreEqual(id, body.GetProperty("data").GetGuid());
    }

    [TestMethod]
    public async Task PutCategory_AdminNonexistent_Returns404()
    {
        using var client = _factory.CreateAdminClient();

        var res = await client.PutAsync($"/api/categories/{Guid.NewGuid()}",
            Serialize(new { Name = "No", Slug = "no-" + Guid.NewGuid().ToString()[..8] }),
            TestContext.CancellationToken);

        Assert.AreEqual(HttpStatusCode.NotFound, res.StatusCode);
    }

    [TestMethod]
    public async Task PutCategory_Unauthenticated_Returns401()
    {
        using var client = _factory.CreateUnauthenticatedClient();

        var res = await client.PutAsync($"/api/categories/{Guid.NewGuid()}",
            Serialize(new { Name = "X" }),
            TestContext.CancellationToken);

        Assert.AreEqual(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [TestMethod]
    public async Task PutCategory_CustomerRole_Returns403()
    {
        using var client = _factory.CreateAuthenticatedClient();

        var res = await client.PutAsync($"/api/categories/{Guid.NewGuid()}",
            Serialize(new { Name = "X" }),
            TestContext.CancellationToken);

        Assert.AreEqual(HttpStatusCode.Forbidden, res.StatusCode);
    }

    // ── DELETE /api/categories/{id} ──────────────────────────────────────────

    [TestMethod]
    public async Task DeleteCategory_AdminExisting_NoProducts_Returns204()
    {
        using var client = _factory.CreateAdminClientNoRedirect();
        var cRes = await client.PostAsync("/api/categories",
            Serialize(new { Name = "DelCatNoProd", Slug = "del-cat-noprod-" + Guid.NewGuid().ToString()[..8] }),
            TestContext.CancellationToken);
        Assert.AreEqual(HttpStatusCode.Created, cRes.StatusCode);
        var id = ExtractIdFromLocation(cRes, "/api/categories/");

        var res = await client.SendAsync(new HttpRequestMessage(HttpMethod.Delete, $"/api/categories/{id}"),
            TestContext.CancellationToken);

        Assert.AreEqual(HttpStatusCode.NoContent, res.StatusCode);
    }

    [TestMethod]
    public async Task DeleteCategory_AdminNonexistent_Returns404()
    {
        using var client = _factory.CreateAdminClient();

        var res = await client.SendAsync(new HttpRequestMessage(HttpMethod.Delete, $"/api/categories/{Guid.NewGuid()}"),
            TestContext.CancellationToken);

        Assert.AreEqual(HttpStatusCode.NotFound, res.StatusCode);
    }

    [TestMethod]
    public async Task DeleteCategory_WithProducts_Returns422AndCode()
    {
        using var client = _factory.CreateAdminClientNoRedirect();
        var catRes = await client.PostAsync("/api/categories",
            Serialize(new { Name = "CatHasProd", Slug = "cat-has-prod-" + Guid.NewGuid().ToString()[..8] }),
            TestContext.CancellationToken);
        Assert.AreEqual(HttpStatusCode.Created, catRes.StatusCode);
        var catId = ExtractIdFromLocation(catRes, "/api/categories/");

        var pRes = await client.PostAsync("/api/products",
            Serialize(new { Name = "PInCat", Slug = "pincat-" + Guid.NewGuid().ToString()[..8], Price = 1m, StockQuantity = 1, CategoryId = catId }),
            TestContext.CancellationToken);
        Assert.AreEqual(HttpStatusCode.Created, pRes.StatusCode);

        var del = await client.SendAsync(new HttpRequestMessage(HttpMethod.Delete, $"/api/categories/{catId}"),
            TestContext.CancellationToken);

        Assert.AreEqual((HttpStatusCode)422, del.StatusCode);
        var api = await Deserialize<object>(del, TestContext.CancellationToken);
        Assert.IsFalse(api!.Success);
        Assert.AreEqual(ErrorCodes.CategoryHasProducts, api.ErrorDetails!.Code);
    }

    [TestMethod]
    public async Task DeleteCategory_Unauthenticated_Returns401()
    {
        using var client = _factory.CreateUnauthenticatedClient();

        var res = await client.SendAsync(new HttpRequestMessage(HttpMethod.Delete, $"/api/categories/{Guid.NewGuid()}"),
            TestContext.CancellationToken);

        Assert.AreEqual(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [TestMethod]
    public async Task DeleteCategory_CustomerRole_Returns403()
    {
        using var client = _factory.CreateAuthenticatedClient();

        var res = await client.SendAsync(new HttpRequestMessage(HttpMethod.Delete, $"/api/categories/{Guid.NewGuid()}"),
            TestContext.CancellationToken);

        Assert.AreEqual(HttpStatusCode.Forbidden, res.StatusCode);
    }
}
