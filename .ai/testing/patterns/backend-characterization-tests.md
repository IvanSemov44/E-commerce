# Pattern: Backend Characterization Tests

Layer 4. Written BEFORE changing code. Same infrastructure as integration tests.

---

## Purpose

A characterization test documents the current observable behavior of a piece of code before it is refactored or migrated. Its job is not to specify what should happen — it records what does happen. Then, after the change, the same test must still pass, proving no regression.

**If you are not about to change the code, you do not need a characterization test. Write a regular integration test instead.**

---

## When to write them

- Before migrating a service to a DDD command/query handler
- Before changing an endpoint's business logic
- Before changing a response shape
- Before removing or replacing infrastructure (e.g., old repository → new one)

---

## Sequence

```
1. Write characterization tests → run → ALL must PASS (documenting current behavior)
2. Make the code change
3. Run the same tests again → ALL must still PASS (no regression)
4. If any fail → the change broke existing behavior → fix before continuing
```

---

## File naming

```
src/backend/ECommerce.Tests/Integration/<Context>CharacterizationTests.cs
```

Examples:
```
ProductsCharacterizationTests.cs
OrdersCharacterizationTests.cs
ReviewsCharacterizationTests.cs
```

---

## Template

```csharp
[TestClass]
public class ProductsCharacterizationTests
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
        _adminClient   = _factory.CreateAdminClient();
        _customerClient = _factory.CreateAuthenticatedClient();
        _anonClient    = _factory.CreateUnauthenticatedClient();
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
    public async Task POST_ValidProduct_Returns201AndBody()
    {
        // Arrange
        var body = new { Name = "Char Test Product", Price = 9.99m, Sku = "CHAR-001", CategoryId = SeedData.CategoryId };

        // Act
        HttpResponseMessage response = await _adminClient.PostAsync(
            "/api/products", Serialize(body), TestContext.CancellationToken);

        // Assert
        Assert.AreEqual(HttpStatusCode.Created, response.StatusCode);
        ApiResponse<ProductDto>? api = await Deserialize<ApiResponse<ProductDto>>(response);
        Assert.IsTrue(api!.Success);
        Assert.AreNotEqual(Guid.Empty, api.Data!.Id);
    }

    [TestMethod]
    public async Task POST_MissingRequiredField_Returns400()
    {
        // Arrange — name is missing
        var body = new { Price = 9.99m, Sku = "CHAR-002" };

        // Act
        HttpResponseMessage response = await _adminClient.PostAsync(
            "/api/products", Serialize(body), TestContext.CancellationToken);

        // Assert
        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
        ApiResponse<JsonElement>? api = await Deserialize<ApiResponse<JsonElement>>(response);
        Assert.IsFalse(api!.Success);
    }

    [TestMethod]
    public async Task POST_DuplicateSku_Returns422WithErrorCode()
    {
        // Arrange — create first product
        await _adminClient.PostAsync("/api/products",
            Serialize(new { Name = "First", Price = 5m, Sku = "CHAR-DUP-001", CategoryId = SeedData.CategoryId }),
            TestContext.CancellationToken);

        // Act — create second with same SKU
        HttpResponseMessage response = await _adminClient.PostAsync("/api/products",
            Serialize(new { Name = "Second", Price = 5m, Sku = "CHAR-DUP-001", CategoryId = SeedData.CategoryId }),
            TestContext.CancellationToken);

        // Assert
        Assert.AreEqual(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        ApiResponse<JsonElement>? api = await Deserialize<ApiResponse<JsonElement>>(response);
        Assert.IsFalse(api!.Success);
        Assert.AreEqual("CATALOG_SKU_ALREADY_EXISTS", api.ErrorCode);  // ← always assert error code
    }

    [TestMethod]
    public async Task POST_Unauthenticated_Returns401()
    {
        // Act
        HttpResponseMessage response = await _anonClient.PostAsync(
            "/api/products", Serialize(new { Name = "X" }), TestContext.CancellationToken);

        // Assert
        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [TestMethod]
    public async Task POST_CustomerRole_Returns403()
    {
        // Act
        HttpResponseMessage response = await _customerClient.PostAsync(
            "/api/products", Serialize(new { Name = "X" }), TestContext.CancellationToken);

        // Assert
        Assert.AreEqual(HttpStatusCode.Forbidden, response.StatusCode);
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

## Critical rules

1. **Assert `ErrorCode` on every 422 response.** This is the most important assertion — it proves the business rule error code survives the migration.

2. **Do not skip 401 / 403 tests** — auth wiring is the first thing a migration can accidentally break.

3. **Run and pass before changing code.** If they do not pass before the change, they are useless.

4. **Do not change characterization tests to make them pass after a migration.** If they fail, fix the production code, not the test.

5. **The full matrix for every write endpoint:**
   - 201/200 happy path
   - 400 validation failure (missing required)
   - 400 validation failure (invalid value)
   - 422 business rule violation (assert error code)
   - 401 unauthenticated
   - 403 wrong role
   - 404 not found (if applicable)
