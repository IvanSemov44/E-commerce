# Pattern: Backend Integration Tests

Layer 3. Full HTTP stack. InMemory EF Core. Tests one endpoint scenario per method.

---

## Project structure

```
src/backend/ECommerce.Tests/
└── Integration/
    ├── TestWebApplicationFactory.cs   ← shared infrastructure
    ├── <Context>ControllerTests.cs    ← regular endpoint tests
    └── <Context>CharacterizationTests.cs  ← characterization (see separate doc)
```

---

## Standard test class template

```csharp
[TestClass]
public class ProductsControllerTests
{
    private static TestWebApplicationFactory _factory = null!;
    private static HttpClient _adminClient = null!;
    private static HttpClient _customerClient = null!;
    private static HttpClient _anonClient = null!;

    public TestContext TestContext { get; set; } = null!;

    [ClassInitialize]
    public static void ClassInit(TestContext _)
    {
        _factory = new TestWebApplicationFactory();
        _adminClient = _factory.CreateAdminClient();
        _customerClient = _factory.CreateAuthenticatedClient();
        _anonClient = _factory.CreateUnauthenticatedClient();
    }

    [ClassCleanup]
    public static async Task ClassCleanup()
    {
        _adminClient.Dispose();
        _customerClient.Dispose();
        _anonClient.Dispose();
        await _factory.DisposeAsync();
    }

    // ── POST /api/products ─────────────────────────────────────────────────────

    [TestMethod]
    public async Task POST_ValidProduct_Returns201WithId()
    {
        // Arrange
        var body = new { Name = "Widget Pro", Price = 29.99m, Sku = "SKU-TEST-001", CategoryId = SeedData.CategoryId };

        // Act
        HttpResponseMessage response = await _adminClient.PostAsync(
            "/api/products", Serialize(body), TestContext.CancellationToken);

        // Assert
        Assert.AreEqual(HttpStatusCode.Created, response.StatusCode);
        ApiResponse<ProductDto>? api = await Deserialize<ApiResponse<ProductDto>>(response);
        Assert.IsNotNull(api);
        Assert.IsTrue(api.Success);
        Assert.AreNotEqual(Guid.Empty, api.Data!.Id);
        Assert.AreEqual("Widget Pro", api.Data.Name);
    }

    [TestMethod]
    public async Task POST_MissingName_Returns400()
    {
        // Arrange
        var body = new { Price = 29.99m, Sku = "SKU-TEST-002" };

        // Act
        HttpResponseMessage response = await _adminClient.PostAsync(
            "/api/products", Serialize(body), TestContext.CancellationToken);

        // Assert
        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
        ApiResponse<JsonElement>? api = await Deserialize<ApiResponse<JsonElement>>(response);
        Assert.IsNotNull(api);
        Assert.IsFalse(api.Success);
    }

    [TestMethod]
    public async Task POST_Unauthenticated_Returns401()
    {
        // Arrange
        var body = new { Name = "Widget", Price = 10m, Sku = "SKU-TEST-003" };

        // Act
        HttpResponseMessage response = await _anonClient.PostAsync(
            "/api/products", Serialize(body), TestContext.CancellationToken);

        // Assert
        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [TestMethod]
    public async Task POST_CustomerRole_Returns403()
    {
        // Arrange
        var body = new { Name = "Widget", Price = 10m, Sku = "SKU-TEST-004" };

        // Act
        HttpResponseMessage response = await _customerClient.PostAsync(
            "/api/products", Serialize(body), TestContext.CancellationToken);

        // Assert
        Assert.AreEqual(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // ── GET /api/products/{id} ─────────────────────────────────────────────────

    [TestMethod]
    public async Task GET_ExistingProduct_Returns200WithCorrectShape()
    {
        // Arrange — create the product inside this test
        HttpResponseMessage created = await _adminClient.PostAsync(
            "/api/products",
            Serialize(new { Name = "Shape Test", Price = 5m, Sku = "SKU-SHAPE-001", CategoryId = SeedData.CategoryId }),
            TestContext.CancellationToken);
        ApiResponse<ProductDto>? createApi = await Deserialize<ApiResponse<ProductDto>>(created);
        Guid id = createApi!.Data!.Id;

        // Act
        HttpResponseMessage response = await _customerClient.GetAsync(
            $"/api/products/{id}", TestContext.CancellationToken);

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        ApiResponse<ProductDto>? api = await Deserialize<ApiResponse<ProductDto>>(response);
        Assert.IsNotNull(api?.Data);
        Assert.AreEqual(id, api.Data.Id);
        Assert.AreEqual("Shape Test", api.Data.Name);
    }

    [TestMethod]
    public async Task GET_NonExistentId_Returns404()
    {
        // Act
        HttpResponseMessage response = await _customerClient.GetAsync(
            $"/api/products/{Guid.NewGuid()}", TestContext.CancellationToken);

        // Assert
        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ── Helpers ────────────────────────────────────────────────────────────────

    private static readonly JsonSerializerOptions _jsonOptions =
        new() { PropertyNameCaseInsensitive = true };

    private static StringContent Serialize(object dto) =>
        new(JsonSerializer.Serialize(dto), Encoding.UTF8, "application/json");

    private static async Task<T?> Deserialize<T>(HttpResponseMessage response)
    {
        string content = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<T>(content, _jsonOptions);
    }
}
```

---

## Test matrix per endpoint type

### Write endpoints (POST / PUT / PATCH / DELETE)

| Scenario | Expected | Assert |
|---|---|---|
| Valid request, authorised | 200 / 201 / 204 | Status + response body (id, fields) |
| Missing required field | 400 | `success == false` |
| Invalid field value | 400 | `success == false` |
| Business rule violation | 422 | `success == false` + **error code** |
| Resource not found | 404 | Status code |
| Unauthenticated | 401 | Status code |
| Wrong role | 403 | Status code |

### Read endpoints (GET)

| Scenario | Expected | Assert |
|---|---|---|
| Existing resource | 200 | Status + full shape of `data` object |
| Non-existent ID | 404 | Status code |
| Unauthenticated (if protected) | 401 | Status code |
| List / paginated | 200 | Status + `data` array not null + pagination fields |

---

## Rules

1. **Use `TestContext.CancellationToken`** for every HTTP call — enables test timeout handling.

2. **Create test data inside the test** — do not rely on seed data for write tests. Seed data is only for tests that verify read-only behaviour on pre-existing state.

3. **Use `[ClassInitialize]`** not `[TestInitialize]` for the factory and clients — avoids rebuilding the web host per test.

4. **Keep the factory disposal in `[ClassCleanup]`** — `DisposeAsync` must be awaited.

5. **One test class per controller** — `ProductsControllerTests.cs`, not `ApiTests.cs`.

6. **Explicit `HttpStatusCode` enum values** — not magic numbers:
   ```csharp
   Assert.AreEqual(HttpStatusCode.Created, response.StatusCode); // GOOD
   Assert.AreEqual(201, (int)response.StatusCode);               // BAD
   ```
