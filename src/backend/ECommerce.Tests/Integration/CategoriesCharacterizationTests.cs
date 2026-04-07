using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ECommerce.Contracts.DTOs.Common;
using ECommerce.SharedKernel.Constants;

namespace ECommerce.Tests.Integration;

[TestClass]
public class CategoriesCharacterizationTests
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

    private static StringContent Serialize(object dto) =>
        new(JsonSerializer.Serialize(dto), Encoding.UTF8, "application/json");

    private static async Task<ApiResponse<T>?> Deserialize<T>(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        string content = await response.Content.ReadAsStringAsync(cancellationToken);
        return JsonSerializer.Deserialize<ApiResponse<T>>(content, _jsonOptions);
    }

    // ---------- CATEGORIES ----------

    [TestMethod]
    public async Task GetCategories_Returns200AndData()
    {
        // Arrange
        using HttpClient client = _factory.CreateUnauthenticatedClient();

        // Act
        HttpResponseMessage res = await client.GetAsync("/api/categories", TestContext.CancellationToken);

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, res.StatusCode);
        string body = await res.Content.ReadAsStringAsync();
        JsonElement json = JsonSerializer.Deserialize<JsonElement>(body, _jsonOptions);
        Assert.IsTrue(json.TryGetProperty("data", out _));
    }

    [TestMethod]
    public async Task GetCategoryById_Existing_Returns200()
    {
        // Arrange — create category prerequisite
        using HttpClient client = _factory.CreateAdminClient();
        var create = new { Name = "CatForGet", Slug = "cat-get-" + Guid.NewGuid().ToString().Substring(0,8) };
        HttpResponseMessage cRes = await client.PostAsync("/api/categories", Serialize(create), TestContext.CancellationToken);
        Assert.AreEqual(HttpStatusCode.Created, cRes.StatusCode);
        ApiResponse<JsonElement>? created = await Deserialize<JsonElement>(cRes, TestContext.CancellationToken);
        Guid id = Guid.Parse(created!.Data!.GetProperty("id").GetString()!);

        // Act
        using HttpClient anon = _factory.CreateUnauthenticatedClient();
        HttpResponseMessage res = await anon.GetAsync($"/api/categories/{id}", TestContext.CancellationToken);

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, res.StatusCode);
    }

    [TestMethod]
    public async Task GetCategoryById_Nonexistent_Returns404()
    {
        // Arrange
        using HttpClient client = _factory.CreateUnauthenticatedClient();

        // Act
        HttpResponseMessage res = await client.GetAsync($"/api/categories/{Guid.NewGuid()}", TestContext.CancellationToken);

        // Assert
        Assert.AreEqual(HttpStatusCode.NotFound, res.StatusCode);
    }

    [TestMethod]
    public async Task GetCategoryBySlug_Existing_Returns200()
    {
        // Arrange
        using HttpClient client = _factory.CreateAdminClient();
        string slug = "cat-slug-" + Guid.NewGuid().ToString().Substring(0,8);
        var create = new { Name = "SlugCat", Slug = slug };
        HttpResponseMessage cRes = await client.PostAsync("/api/categories", Serialize(create), TestContext.CancellationToken);
        Assert.AreEqual(HttpStatusCode.Created, cRes.StatusCode);

        // Act
        using HttpClient anon = _factory.CreateUnauthenticatedClient();
        HttpResponseMessage res = await anon.GetAsync($"/api/categories/slug/{slug}", TestContext.CancellationToken);

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, res.StatusCode);
    }

    [TestMethod]
    public async Task GetCategoryBySlug_Nonexistent_Returns404()
    {
        // Arrange
        using HttpClient client = _factory.CreateUnauthenticatedClient();

        // Act
        HttpResponseMessage res = await client.GetAsync($"/api/categories/slug/{Guid.NewGuid()}", TestContext.CancellationToken);

        // Assert
        Assert.AreEqual(HttpStatusCode.NotFound, res.StatusCode);
    }

    [TestMethod]
    public async Task PostCategory_AdminValid_Returns201AndMatches()
    {
        // Arrange
        using HttpClient client = _factory.CreateAdminClient();
        string slug = "cat-create-" + Guid.NewGuid().ToString().Substring(0,8);
        var create = new { Name = "CreateCat", Slug = slug };

        // Act
        HttpResponseMessage res = await client.PostAsync("/api/categories", Serialize(create), TestContext.CancellationToken);

        // Assert
        Assert.AreEqual(HttpStatusCode.Created, res.StatusCode);
        ApiResponse<JsonElement>? api = await Deserialize<JsonElement>(res, TestContext.CancellationToken);
        Assert.AreEqual("CreateCat", api!.Data!.GetProperty("name").GetString());
    }

    [TestMethod]
    public async Task PostCategory_AdminMissingName_Returns400()
    {
        // Arrange
        using HttpClient client = _factory.CreateAdminClient();
        var create = new { Slug = "no-name-cat-" + Guid.NewGuid().ToString().Substring(0,8) };

        // Act
        HttpResponseMessage res = await client.PostAsync("/api/categories", Serialize(create), TestContext.CancellationToken);

        // Assert
        Assert.AreEqual(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [TestMethod]
    public async Task PostCategory_DuplicateSlug_Returns422WithCode()
    {
        // Arrange
        using HttpClient client = _factory.CreateAdminClient();
        string slug = "dup-cat-" + Guid.NewGuid().ToString().Substring(0,8);
        var create = new { Name = "Dup1", Slug = slug };

        // Act
        HttpResponseMessage r1 = await client.PostAsync("/api/categories", Serialize(create), TestContext.CancellationToken);

        // Assert first created
        Assert.AreEqual(HttpStatusCode.Created, r1.StatusCode);

        // Act (create duplicate)
        HttpResponseMessage r2 = await client.PostAsync("/api/categories", Serialize(create), TestContext.CancellationToken);

        // Assert
        Assert.AreEqual((HttpStatusCode)422, r2.StatusCode);
        ApiResponse<object>? api = await Deserialize<object>(r2, TestContext.CancellationToken);
        Assert.IsFalse(api!.Success);
        Assert.AreEqual(ErrorCodes.DuplicateCategorySlug, api.ErrorDetails!.Code);
    }

    [TestMethod]
    public async Task PostCategory_Unauthenticated_Returns401()
    {
        // Arrange
        using HttpClient client = _factory.CreateUnauthenticatedClient();
        var create = new { Name = "U", Slug = "u-" + Guid.NewGuid().ToString().Substring(0,8) };

        // Act
        HttpResponseMessage res = await client.PostAsync("/api/categories", Serialize(create), TestContext.CancellationToken);

        // Assert
        Assert.AreEqual(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [TestMethod]
    public async Task PostCategory_CustomerRole_Returns403()
    {
        // Arrange
        using HttpClient client = _factory.CreateAuthenticatedClient();
        var create = new { Name = "U", Slug = "u-cust-" + Guid.NewGuid().ToString().Substring(0,8) };

        // Act
        HttpResponseMessage res = await client.PostAsync("/api/categories", Serialize(create), TestContext.CancellationToken);

        // Assert
        Assert.AreEqual(HttpStatusCode.Forbidden, res.StatusCode);
    }

    [TestMethod]
    public async Task PutCategory_AdminExisting_Returns200()
    {
        // Arrange
        using HttpClient client = _factory.CreateAdminClient();
        var create = new { Name = "PutCat", Slug = "put-cat-" + Guid.NewGuid().ToString().Substring(0,8) };
        HttpResponseMessage cRes = await client.PostAsync("/api/categories", Serialize(create), TestContext.CancellationToken);
        Assert.AreEqual(HttpStatusCode.Created, cRes.StatusCode);
        ApiResponse<JsonElement>? created = await Deserialize<JsonElement>(cRes, TestContext.CancellationToken);
        Guid id = Guid.Parse(created!.Data!.GetProperty("id").GetString()!);

        var update = new { Name = "PutUpdated", Slug = "put-up-" + Guid.NewGuid().ToString().Substring(0,8) };

        // Act
        HttpResponseMessage res = await client.PutAsync($"/api/categories/{id}", Serialize(update), TestContext.CancellationToken);

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, res.StatusCode);
    }

    [TestMethod]
    public async Task PutCategory_AdminNonexistent_Returns404()
    {
        // Arrange
        using HttpClient client = _factory.CreateAdminClient();
        var update = new { Name = "No", Slug = "no-" + Guid.NewGuid().ToString().Substring(0,8) };

        // Act
        HttpResponseMessage res = await client.PutAsync($"/api/categories/{Guid.NewGuid()}", Serialize(update), TestContext.CancellationToken);

        // Assert
        Assert.AreEqual(HttpStatusCode.NotFound, res.StatusCode);
    }

    [TestMethod]
    public async Task PutCategory_Unauthenticated_Returns401()
    {
        // Arrange
        using HttpClient client = _factory.CreateUnauthenticatedClient();

        // Act
        HttpResponseMessage res = await client.PutAsync($"/api/categories/{Guid.NewGuid()}", Serialize(new { Name = "X" }), TestContext.CancellationToken);

        // Assert
        Assert.AreEqual(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [TestMethod]
    public async Task PutCategory_CustomerRole_Returns403()
    {
        // Arrange
        using HttpClient client = _factory.CreateAuthenticatedClient();

        // Act
        HttpResponseMessage res = await client.PutAsync($"/api/categories/{Guid.NewGuid()}", Serialize(new { Name = "X" }), TestContext.CancellationToken);

        // Assert
        Assert.AreEqual(HttpStatusCode.Forbidden, res.StatusCode);
    }

    [TestMethod]
    public async Task DeleteCategory_AdminExisting_NoProducts_ReturnsSuccess()
    {
        // Arrange
        using HttpClient client = _factory.CreateAdminClient();
        var create = new { Name = "DelCatNoProd", Slug = "del-cat-noprod-" + Guid.NewGuid().ToString().Substring(0,8) };
        HttpResponseMessage cRes = await client.PostAsync("/api/categories", Serialize(create), TestContext.CancellationToken);
        Assert.AreEqual(HttpStatusCode.Created, cRes.StatusCode);
        ApiResponse<JsonElement>? created = await Deserialize<JsonElement>(cRes, TestContext.CancellationToken);
        Guid id = Guid.Parse(created!.Data!.GetProperty("id").GetString()!);

        // Act
        HttpResponseMessage res = await client.SendAsync(new HttpRequestMessage(HttpMethod.Delete, $"/api/categories/{id}"), TestContext.CancellationToken);

        // Assert
        Assert.IsTrue(res.StatusCode == HttpStatusCode.OK || res.StatusCode == HttpStatusCode.NoContent);
    }

    [TestMethod]
    public async Task DeleteCategory_AdminNonexistent_Returns404()
    {
        // Arrange
        using HttpClient client = _factory.CreateAdminClient();

        // Act
        HttpResponseMessage res = await client.SendAsync(new HttpRequestMessage(HttpMethod.Delete, $"/api/categories/{Guid.NewGuid()}"), TestContext.CancellationToken);

        // Assert
        Assert.AreEqual(HttpStatusCode.NotFound, res.StatusCode);
    }

    [TestMethod]
    public async Task DeleteCategory_WithProducts_Returns422AndCode()
    {
        // Arrange
        using HttpClient client = _factory.CreateAdminClient();
        // create category
        var createCat = new { Name = "CatHasProd", Slug = "cat-has-prod-" + Guid.NewGuid().ToString().Substring(0,8) };
        HttpResponseMessage cRes = await client.PostAsync("/api/categories", Serialize(createCat), TestContext.CancellationToken);
        Assert.AreEqual(HttpStatusCode.Created, cRes.StatusCode);
        ApiResponse<JsonElement>? created = await Deserialize<JsonElement>(cRes, TestContext.CancellationToken);
        Guid catId = Guid.Parse(created!.Data!.GetProperty("id").GetString()!);

        // create product in that category
        var createProd = new { Name = "PInCat", Slug = "pincat-" + Guid.NewGuid().ToString().Substring(0,8), Price = 1m, StockQuantity = 1, CategoryId = catId };
        HttpResponseMessage pRes = await client.PostAsync("/api/products", Serialize(createProd), TestContext.CancellationToken);
        Assert.AreEqual(HttpStatusCode.Created, pRes.StatusCode);

        // Act
        HttpResponseMessage del = await client.SendAsync(new HttpRequestMessage(HttpMethod.Delete, $"/api/categories/{catId}"), TestContext.CancellationToken);

        // Assert
        Assert.AreEqual((HttpStatusCode)422, del.StatusCode);
        ApiResponse<object>? api = await Deserialize<object>(del, TestContext.CancellationToken);
        Assert.IsFalse(api!.Success);
        Assert.AreEqual(ErrorCodes.CategoryHasProducts, api.ErrorDetails!.Code);
    }

    [TestMethod]
    public async Task DeleteCategory_Unauthenticated_Returns401()
    {
        // Arrange
        using HttpClient client = _factory.CreateUnauthenticatedClient();

        // Act
        HttpResponseMessage res = await client.SendAsync(new HttpRequestMessage(HttpMethod.Delete, $"/api/categories/{Guid.NewGuid()}"), TestContext.CancellationToken);

        // Assert
        Assert.AreEqual(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [TestMethod]
    public async Task DeleteCategory_CustomerRole_Returns403()
    {
        // Arrange
        using HttpClient client = _factory.CreateAuthenticatedClient();

        // Act
        HttpResponseMessage res = await client.SendAsync(new HttpRequestMessage(HttpMethod.Delete, $"/api/categories/{Guid.NewGuid()}"), TestContext.CancellationToken);

        // Assert
        Assert.AreEqual(HttpStatusCode.Forbidden, res.StatusCode);
    }
}

