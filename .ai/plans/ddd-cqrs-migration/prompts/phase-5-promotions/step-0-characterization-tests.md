# Phase 5, Step 0: Promotions Characterization Tests

**Do this BEFORE touching any migration code.** These tests pin the existing HTTP contract for the PromoCodesController.

**Prerequisite**: Phase 4 (Shopping) complete and all tests pass.

---

## Context

The existing `PromoCodesController` is backed by `IPromoCodeService`. We create `PromoCodeCharacterizationTests.cs` to lock down the EXACT contract: status codes, response shapes, auth requirements, and route structure. These must pass against the OLD service before migration AND must still pass after cutover.

**Key things to pin:**
- Route is `api/promo-codes` (hyphenated, not `api/promocodes`)
- `GET /api/promo-codes/active` is anonymous and returns paginated shape
- All other endpoints except `POST /api/promo-codes/validate` require Admin or SuperAdmin
- `POST /api/promo-codes/validate` is anonymous and ALWAYS returns 200 — even for invalid codes
- `POST /api/promo-codes` returns 201 Created with a `Location` header
- `PUT /api/promo-codes/{id}/deactivate` uses the sub-route `deactivate`
- Seeded PromoCode: Id `55555555-5555-5555-5555-555555555555`, Code `SAVE20`, 20% discount, IsActive=true

---

## Task: Create Characterization Tests in ECommerce.Tests

File goes in `src/backend/ECommerce.Tests/Integration/`.

---

### File: `ECommerce.Tests/Integration/PromoCodeCharacterizationTests.cs`

```csharp
using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ECommerce.Tests.Integration;

/// <summary>
/// Characterization tests for PromoCodesController.
/// Written BEFORE Phase 5 migration — pin the exact HTTP contract.
/// Must pass against both old PromoCodeService AND new MediatR handlers.
/// </summary>
[TestClass]
public class PromoCodeCharacterizationTests
{
    private TestWebApplicationFactory _factory = null!;
    private static readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

    // Seeded PromoCode used across multiple tests
    private const string SeededPromoCodeId   = "55555555-5555-5555-5555-555555555555";
    private const string SeededPromoCodeCode = "SAVE20";

    public TestContext TestContext { get; set; } = null!;

    [TestInitialize]
    public void Setup() => _factory = new TestWebApplicationFactory();

    [TestCleanup]
    public void Cleanup()
    {
        ConditionalTestAuthHandler.IsAuthenticationEnabled = true;
        ConditionalTestAuthHandler.CurrentUserId           = ConditionalTestAuthHandler.TestUserId;
        ConditionalTestAuthHandler.CurrentUserRole         = "Customer";
        _factory?.Dispose();
    }

    private static StringContent Json(object dto) =>
        new(JsonSerializer.Serialize(dto), Encoding.UTF8, "application/json");

    /// <summary>Generates a unique promo code string safe for use in tests.</summary>
    private static string UniqueCode() =>
        ("TEST" + Guid.NewGuid().ToString("N").ToUpper())[..10]; // e.g. "TESTA1B2C3D4"

    // ── GET /api/promo-codes/active ────────────────────────────────────────────

    [TestMethod]
    public async Task GetActivePromoCodes_Anonymous_Returns200()
    {
        using var client = _factory.CreateUnauthenticatedClient();

        var res = await client.GetAsync("/api/promo-codes/active", TestContext.CancellationToken);

        Assert.AreEqual(HttpStatusCode.OK, res.StatusCode);
    }

    [TestMethod]
    public async Task GetActivePromoCodes_ResponseShape_HasPaginatedData()
    {
        using var client = _factory.CreateUnauthenticatedClient();

        var res  = await client.GetAsync("/api/promo-codes/active?page=1&pageSize=10", TestContext.CancellationToken);
        var body = await res.Content.ReadAsStringAsync();
        var json = JsonSerializer.Deserialize<JsonElement>(body, _jsonOptions);

        Assert.AreEqual(HttpStatusCode.OK, res.StatusCode);
        Assert.IsTrue(json.TryGetProperty("data", out var data), "Response must have 'data'");
        Assert.IsTrue(
            data.TryGetProperty("items", out _) || data.TryGetProperty("Items", out _),
            "Paginated data must have 'items'");
        Assert.IsTrue(
            data.TryGetProperty("totalCount", out _) || data.TryGetProperty("TotalCount", out _),
            "Paginated data must have 'totalCount'");
    }

    [TestMethod]
    public async Task GetActivePromoCodes_PageSizeClamped_To100()
    {
        // pageSize > 100 must be clamped — the endpoint must not throw or 400
        using var client = _factory.CreateUnauthenticatedClient();

        var res = await client.GetAsync("/api/promo-codes/active?page=1&pageSize=999", TestContext.CancellationToken);

        Assert.AreEqual(HttpStatusCode.OK, res.StatusCode);
    }

    // ── GET /api/promo-codes (admin list) ──────────────────────────────────────

    [TestMethod]
    public async Task GetAllPromoCodes_Unauthenticated_Returns401()
    {
        using var client = _factory.CreateUnauthenticatedClient();

        var res = await client.GetAsync("/api/promo-codes", TestContext.CancellationToken);

        Assert.AreEqual(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [TestMethod]
    public async Task GetAllPromoCodes_CustomerRole_Returns403()
    {
        // Customer is not Admin — must be forbidden
        ConditionalTestAuthHandler.CurrentUserRole = "Customer";
        using var client = _factory.CreateAuthenticatedClient();

        var res = await client.GetAsync("/api/promo-codes", TestContext.CancellationToken);

        Assert.AreEqual(HttpStatusCode.Forbidden, res.StatusCode);
    }

    [TestMethod]
    public async Task GetAllPromoCodes_AdminRole_Returns200WithPaginatedShape()
    {
        ConditionalTestAuthHandler.CurrentUserRole = "Admin";
        using var client = _factory.CreateAuthenticatedClient();

        var res  = await client.GetAsync("/api/promo-codes?page=1&pageSize=10", TestContext.CancellationToken);
        var body = await res.Content.ReadAsStringAsync();
        var json = JsonSerializer.Deserialize<JsonElement>(body, _jsonOptions);

        Assert.AreEqual(HttpStatusCode.OK, res.StatusCode);
        Assert.IsTrue(json.TryGetProperty("data", out var data), "Response must have 'data'");
        Assert.IsTrue(
            data.TryGetProperty("items", out _) || data.TryGetProperty("Items", out _),
            "Paginated response must have 'items'");
    }

    // ── GET /api/promo-codes/{id} ──────────────────────────────────────────────

    [TestMethod]
    public async Task GetPromoCodeById_Unauthenticated_Returns401()
    {
        using var client = _factory.CreateUnauthenticatedClient();

        var res = await client.GetAsync($"/api/promo-codes/{SeededPromoCodeId}", TestContext.CancellationToken);

        Assert.AreEqual(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [TestMethod]
    public async Task GetPromoCodeById_CustomerRole_Returns403()
    {
        ConditionalTestAuthHandler.CurrentUserRole = "Customer";
        using var client = _factory.CreateAuthenticatedClient();

        var res = await client.GetAsync($"/api/promo-codes/{SeededPromoCodeId}", TestContext.CancellationToken);

        Assert.AreEqual(HttpStatusCode.Forbidden, res.StatusCode);
    }

    [TestMethod]
    public async Task GetPromoCodeById_AdminRole_SeededId_Returns200WithDetailShape()
    {
        ConditionalTestAuthHandler.CurrentUserRole = "Admin";
        using var client = _factory.CreateAuthenticatedClient();

        var res  = await client.GetAsync($"/api/promo-codes/{SeededPromoCodeId}", TestContext.CancellationToken);
        var body = await res.Content.ReadAsStringAsync();
        var json = JsonSerializer.Deserialize<JsonElement>(body, _jsonOptions);

        Assert.AreEqual(HttpStatusCode.OK, res.StatusCode);
        Assert.IsTrue(json.TryGetProperty("data", out var data), "Response must have 'data'");
        // Verify code field in response
        var code = data.TryGetProperty("code", out var c) ? c.GetString() :
                   data.TryGetProperty("Code", out var cc) ? cc.GetString() : null;
        Assert.AreEqual(SeededPromoCodeCode, code, "Seeded code must be SAVE20");
    }

    [TestMethod]
    public async Task GetPromoCodeById_AdminRole_UnknownId_Returns404()
    {
        ConditionalTestAuthHandler.CurrentUserRole = "Admin";
        using var client = _factory.CreateAuthenticatedClient();

        var res = await client.GetAsync($"/api/promo-codes/{Guid.NewGuid()}", TestContext.CancellationToken);

        Assert.AreEqual(HttpStatusCode.NotFound, res.StatusCode);
    }

    // ── POST /api/promo-codes ──────────────────────────────────────────────────

    [TestMethod]
    public async Task CreatePromoCode_Unauthenticated_Returns401()
    {
        using var client = _factory.CreateUnauthenticatedClient();
        var payload = new { Code = UniqueCode(), DiscountType = "Percentage", DiscountValue = 10m };

        var res = await client.PostAsync("/api/promo-codes", Json(payload), TestContext.CancellationToken);

        Assert.AreEqual(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [TestMethod]
    public async Task CreatePromoCode_AdminRole_ValidPayload_Returns201WithLocationHeader()
    {
        ConditionalTestAuthHandler.CurrentUserRole = "Admin";
        using var client = _factory.CreateAuthenticatedClient();
        var payload = new
        {
            Code          = UniqueCode(),
            DiscountType  = "Percentage",
            DiscountValue = 15m,
            IsActive      = true
        };

        var res = await client.PostAsync("/api/promo-codes", Json(payload), TestContext.CancellationToken);

        Assert.AreEqual(HttpStatusCode.Created, res.StatusCode,
            $"Create must return 201. Body: {await res.Content.ReadAsStringAsync()}");
        Assert.IsNotNull(res.Headers.Location, "Created response must include Location header");
    }

    [TestMethod]
    public async Task CreatePromoCode_InvalidDiscountType_Returns400()
    {
        ConditionalTestAuthHandler.CurrentUserRole = "Admin";
        using var client = _factory.CreateAuthenticatedClient();
        var payload = new { Code = UniqueCode(), DiscountType = "InvalidType", DiscountValue = 10m };

        var res = await client.PostAsync("/api/promo-codes", Json(payload), TestContext.CancellationToken);

        Assert.IsTrue(
            res.StatusCode == HttpStatusCode.BadRequest
            || res.StatusCode == HttpStatusCode.UnprocessableEntity,
            $"Invalid DiscountType must return 400 or 422, got {(int)res.StatusCode}");
    }

    [TestMethod]
    public async Task CreatePromoCode_DuplicateCode_Returns409()
    {
        ConditionalTestAuthHandler.CurrentUserRole = "Admin";
        using var client = _factory.CreateAuthenticatedClient();

        // Use the seeded SAVE20 code — it already exists
        var payload = new { Code = SeededPromoCodeCode, DiscountType = "Percentage", DiscountValue = 10m };

        var res = await client.PostAsync("/api/promo-codes", Json(payload), TestContext.CancellationToken);

        Assert.AreEqual(HttpStatusCode.Conflict, res.StatusCode,
            $"Duplicate code must return 409. Body: {await res.Content.ReadAsStringAsync()}");
    }

    // ── PUT /api/promo-codes/{id} ──────────────────────────────────────────────

    [TestMethod]
    public async Task UpdatePromoCode_Unauthenticated_Returns401()
    {
        using var client = _factory.CreateUnauthenticatedClient();
        var payload = new { DiscountValue = 25m };

        var res = await client.PutAsync($"/api/promo-codes/{SeededPromoCodeId}", Json(payload), TestContext.CancellationToken);

        Assert.AreEqual(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [TestMethod]
    public async Task UpdatePromoCode_AdminRole_SeededId_Returns200()
    {
        ConditionalTestAuthHandler.CurrentUserRole = "Admin";
        using var client = _factory.CreateAuthenticatedClient();
        var payload = new { DiscountValue = (decimal?)20m }; // no change in value — just a valid update

        var res = await client.PutAsync($"/api/promo-codes/{SeededPromoCodeId}", Json(payload), TestContext.CancellationToken);

        Assert.AreEqual(HttpStatusCode.OK, res.StatusCode,
            $"Update must return 200. Body: {await res.Content.ReadAsStringAsync()}");
    }

    [TestMethod]
    public async Task UpdatePromoCode_AdminRole_UnknownId_Returns404()
    {
        ConditionalTestAuthHandler.CurrentUserRole = "Admin";
        using var client = _factory.CreateAuthenticatedClient();
        var payload = new { DiscountValue = (decimal?)10m };

        var res = await client.PutAsync($"/api/promo-codes/{Guid.NewGuid()}", Json(payload), TestContext.CancellationToken);

        Assert.AreEqual(HttpStatusCode.NotFound, res.StatusCode);
    }

    // ── PUT /api/promo-codes/{id}/deactivate ──────────────────────────────────

    [TestMethod]
    public async Task DeactivatePromoCode_Unauthenticated_Returns401()
    {
        using var client = _factory.CreateUnauthenticatedClient();

        var res = await client.PutAsync(
            $"/api/promo-codes/{SeededPromoCodeId}/deactivate",
            new StringContent("", Encoding.UTF8, "application/json"),
            TestContext.CancellationToken);

        Assert.AreEqual(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [TestMethod]
    public async Task DeactivatePromoCode_AdminRole_SeededId_Returns200()
    {
        ConditionalTestAuthHandler.CurrentUserRole = "Admin";
        using var client = _factory.CreateAuthenticatedClient();

        var res = await client.PutAsync(
            $"/api/promo-codes/{SeededPromoCodeId}/deactivate",
            new StringContent("", Encoding.UTF8, "application/json"),
            TestContext.CancellationToken);

        Assert.AreEqual(HttpStatusCode.OK, res.StatusCode,
            $"Deactivate must return 200. Body: {await res.Content.ReadAsStringAsync()}");
    }

    [TestMethod]
    public async Task DeactivatePromoCode_AdminRole_UnknownId_Returns404()
    {
        ConditionalTestAuthHandler.CurrentUserRole = "Admin";
        using var client = _factory.CreateAuthenticatedClient();

        var res = await client.PutAsync(
            $"/api/promo-codes/{Guid.NewGuid()}/deactivate",
            new StringContent("", Encoding.UTF8, "application/json"),
            TestContext.CancellationToken);

        Assert.AreEqual(HttpStatusCode.NotFound, res.StatusCode);
    }

    // ── DELETE /api/promo-codes/{id} ───────────────────────────────────────────

    [TestMethod]
    public async Task DeletePromoCode_Unauthenticated_Returns401()
    {
        using var client = _factory.CreateUnauthenticatedClient();

        var res = await client.DeleteAsync($"/api/promo-codes/{Guid.NewGuid()}", TestContext.CancellationToken);

        Assert.AreEqual(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [TestMethod]
    public async Task DeletePromoCode_CustomerRole_Returns403()
    {
        ConditionalTestAuthHandler.CurrentUserRole = "Customer";
        using var client = _factory.CreateAuthenticatedClient();

        var res = await client.DeleteAsync($"/api/promo-codes/{Guid.NewGuid()}", TestContext.CancellationToken);

        Assert.AreEqual(HttpStatusCode.Forbidden, res.StatusCode);
    }

    [TestMethod]
    public async Task DeletePromoCode_AdminRole_UnknownId_Returns404()
    {
        ConditionalTestAuthHandler.CurrentUserRole = "Admin";
        using var client = _factory.CreateAuthenticatedClient();

        var res = await client.DeleteAsync($"/api/promo-codes/{Guid.NewGuid()}", TestContext.CancellationToken);

        Assert.AreEqual(HttpStatusCode.NotFound, res.StatusCode);
    }

    [TestMethod]
    public async Task DeletePromoCode_AdminRole_CreatedCode_Returns200()
    {
        ConditionalTestAuthHandler.CurrentUserRole = "Admin";
        using var client = _factory.CreateAuthenticatedClient();

        // Create a code so we have something to delete
        var createPayload = new { Code = UniqueCode(), DiscountType = "Percentage", DiscountValue = 5m, IsActive = true };
        var createRes = await client.PostAsync("/api/promo-codes", Json(createPayload), TestContext.CancellationToken);
        Assert.AreEqual(HttpStatusCode.Created, createRes.StatusCode, "Pre-condition: create must succeed");

        var location = createRes.Headers.Location!.ToString();
        var id       = location.Split('/').Last();

        var deleteRes = await client.DeleteAsync($"/api/promo-codes/{id}", TestContext.CancellationToken);

        Assert.AreEqual(HttpStatusCode.OK, deleteRes.StatusCode,
            $"Delete must return 200. Body: {await deleteRes.Content.ReadAsStringAsync()}");
    }

    // ── POST /api/promo-codes/validate ─────────────────────────────────────────

    [TestMethod]
    public async Task ValidatePromoCode_Anonymous_AlwaysReturns200()
    {
        // This endpoint is always 200 — even for invalid codes
        using var client = _factory.CreateUnauthenticatedClient();
        var payload = new { code = "COMPLETELY-INVALID-CODE-99", orderAmount = 100m };

        var res = await client.PostAsync("/api/promo-codes/validate", Json(payload), TestContext.CancellationToken);

        Assert.AreEqual(HttpStatusCode.OK, res.StatusCode,
            "Validate must always return 200 regardless of code validity");
    }

    [TestMethod]
    public async Task ValidatePromoCode_UnknownCode_Returns200WithIsValidFalse()
    {
        using var client = _factory.CreateUnauthenticatedClient();
        var payload = new { code = "DOES-NOT-EXIST", orderAmount = 100m };

        var res  = await client.PostAsync("/api/promo-codes/validate", Json(payload), TestContext.CancellationToken);
        var body = await res.Content.ReadAsStringAsync();
        var json = JsonSerializer.Deserialize<JsonElement>(body, _jsonOptions);

        Assert.AreEqual(HttpStatusCode.OK, res.StatusCode);
        Assert.IsTrue(json.TryGetProperty("data", out var data), "Response must have 'data'");
        var isValid = data.TryGetProperty("isValid", out var iv) ? iv.GetBoolean() :
                      data.TryGetProperty("IsValid", out var iv2) ? iv2.GetBoolean() : true;
        Assert.IsFalse(isValid, "Unknown code must produce isValid=false");
    }

    [TestMethod]
    public async Task ValidatePromoCode_Seeded_SAVE20_Returns200WithIsValidTrueAndCorrectDiscount()
    {
        // SAVE20 = 20% off. 20% of 100 = 20
        using var client = _factory.CreateUnauthenticatedClient();
        var payload = new { code = SeededPromoCodeCode, orderAmount = 100m };

        var res  = await client.PostAsync("/api/promo-codes/validate", Json(payload), TestContext.CancellationToken);
        var body = await res.Content.ReadAsStringAsync();
        var json = JsonSerializer.Deserialize<JsonElement>(body, _jsonOptions);

        Assert.AreEqual(HttpStatusCode.OK, res.StatusCode);
        Assert.IsTrue(json.TryGetProperty("data", out var data), "Response must have 'data'");

        var isValid = data.TryGetProperty("isValid", out var iv) ? iv.GetBoolean() :
                      data.TryGetProperty("IsValid", out var iv2) && iv2.GetBoolean();
        Assert.IsTrue(isValid, "SAVE20 must be valid");

        var discount = data.TryGetProperty("discountAmount", out var da) ? da.GetDecimal() :
                       data.TryGetProperty("DiscountAmount", out var da2) ? da2.GetDecimal() : -1m;
        Assert.AreEqual(20m, discount, "20% of 100 = 20");
    }

    [TestMethod]
    public async Task ValidatePromoCode_CodeIsCaseInsensitive()
    {
        // "save20" lowercase must behave the same as "SAVE20"
        using var client = _factory.CreateUnauthenticatedClient();
        var payloadLower = new { code = "save20", orderAmount = 100m };

        var res  = await client.PostAsync("/api/promo-codes/validate", Json(payloadLower), TestContext.CancellationToken);
        var body = await res.Content.ReadAsStringAsync();
        var json = JsonSerializer.Deserialize<JsonElement>(body, _jsonOptions);

        Assert.AreEqual(HttpStatusCode.OK, res.StatusCode);
        Assert.IsTrue(json.TryGetProperty("data", out var data), "Response must have 'data'");
        var isValid = data.TryGetProperty("isValid", out var iv) ? iv.GetBoolean() :
                      data.TryGetProperty("IsValid", out var iv2) && iv2.GetBoolean();
        Assert.IsTrue(isValid, "Code lookup must be case-insensitive");
    }

    [TestMethod]
    public async Task ValidatePromoCode_MinOrderAmountNotMet_Returns200WithIsValidFalse()
    {
        // Create a code with minOrderAmount=500, then validate with orderAmount=50
        ConditionalTestAuthHandler.CurrentUserRole = "Admin";
        using var adminClient = _factory.CreateAuthenticatedClient();

        var code = UniqueCode();
        var createPayload = new
        {
            Code             = code,
            DiscountType     = "Percentage",
            DiscountValue    = 10m,
            MinOrderAmount   = 500m,
            IsActive         = true
        };
        var createRes = await adminClient.PostAsync(
            "/api/promo-codes", Json(createPayload), TestContext.CancellationToken);
        Assert.AreEqual(HttpStatusCode.Created, createRes.StatusCode,
            "Pre-condition: create must succeed");

        // Now validate as anonymous with orderAmount=50 (below minimum)
        using var anonClient = _factory.CreateUnauthenticatedClient();
        var validatePayload = new { code, orderAmount = 50m };

        var res  = await anonClient.PostAsync("/api/promo-codes/validate", Json(validatePayload), TestContext.CancellationToken);
        var body = await res.Content.ReadAsStringAsync();
        var json = JsonSerializer.Deserialize<JsonElement>(body, _jsonOptions);

        Assert.AreEqual(HttpStatusCode.OK, res.StatusCode,
            "Validate must still return 200 even when min order not met");
        Assert.IsTrue(json.TryGetProperty("data", out var data), "Response must have 'data'");
        var isValid = data.TryGetProperty("isValid", out var iv) ? iv.GetBoolean() :
                      data.TryGetProperty("IsValid", out var iv2) && iv2.GetBoolean();
        Assert.IsFalse(isValid, "Min order not met must produce isValid=false");
    }
}
```

---

## Run Before Starting Migration

```bash
cd src/backend
dotnet test ECommerce.Tests/ECommerce.Tests.csproj \
    --filter "FullyQualifiedName~PromoCodeCharacterizationTests"
```

All must pass. If any fail, fix the test assumption before starting migration.

---

## Acceptance Criteria

- [ ] `PromoCodeCharacterizationTests.cs` created in `ECommerce.Tests/Integration/`
- [ ] All tests pass against the EXISTING `PromoCodeService` (before migration)
- [ ] Anonymous access confirmed: `GET /api/promo-codes/active` and `POST /api/promo-codes/validate` return 200 without token
- [ ] Auth invariants confirmed: all other endpoints return 401 without token, 403 for Customer role
- [ ] `POST /api/promo-codes` confirmed to return 201 with `Location` header
- [ ] `POST /api/promo-codes/validate` confirmed to ALWAYS return 200 regardless of code validity
- [ ] `data.isValid=true` and `data.discountAmount=20` confirmed for SAVE20 + orderAmount=100
- [ ] `data.isValid=false` confirmed for unknown code (still 200)
- [ ] `data.isValid=false` confirmed when minOrderAmount not met (still 200)
- [ ] Case-insensitive code lookup confirmed: `save20` == `SAVE20`
- [ ] Duplicate code → 409 confirmed
