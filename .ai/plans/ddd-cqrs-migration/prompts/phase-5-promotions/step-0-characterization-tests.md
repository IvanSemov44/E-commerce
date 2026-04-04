# Phase 5, Step 0: Characterization Tests

**Prerequisite**: Phase 4 complete. Old `PromoCodeService` still in place. Do NOT modify any production code in this step.

Write integration tests that pin the existing HTTP contract. These tests run against the OLD service now and must pass again — without modification — after cutover to new MediatR handlers.

Seeded test data (from `TestWebApplicationFactory`):
- PromoCode Id: `55555555-5555-5555-5555-555555555555`, Code: `SAVE20`, DiscountType: `Percentage`, DiscountValue: `20`, IsActive: `true`, MaxUses: `null`, UsedCount: `0`, MinOrderAmount: `null`, MaxDiscountAmount: `null`

---

## Create the file

`src/backend/ECommerce.Tests/Integration/PromoCodeCharacterizationTests.cs`

```csharp
using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ECommerce.Tests.Integration;

[TestClass]
public class PromoCodeCharacterizationTests
{
    private static TestWebApplicationFactory _factory = null!;
    private static readonly JsonSerializerOptions _json = new() { PropertyNameCaseInsensitive = true };
    private static readonly Guid SeededPromoCodeId = Guid.Parse("55555555-5555-5555-5555-555555555555");

    [TestInitialize]
    public void Setup()
    {
        _factory ??= new TestWebApplicationFactory();
    }

    [TestCleanup]
    public void Cleanup()
    {
        ConditionalTestAuthHandler.IsAuthenticationEnabled = true;
        ConditionalTestAuthHandler.CurrentUserId = ConditionalTestAuthHandler.TestUserId;
        ConditionalTestAuthHandler.CurrentUserRole = "Customer";
    }

    // ────────────────────────────────────────────────
    // GET /api/promo-codes/active  (anonymous)
    // ────────────────────────────────────────────────

    [TestMethod]
    public async Task GetActiveCodes_Anonymous_Returns200()
    {
        using var client = _factory.CreateUnauthenticatedClient();
        var response = await client.GetAsync("/api/promo-codes/active");
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
    }

    [TestMethod]
    public async Task GetActiveCodes_ResponseShape_HasDataWithItemsAndTotalCount()
    {
        using var client = _factory.CreateUnauthenticatedClient();
        var body = JsonSerializer.Deserialize<JsonElement>(
            await (await client.GetAsync("/api/promo-codes/active")).Content.ReadAsStringAsync(), _json);

        Assert.IsTrue(body.TryGetProperty("data", out var data));
        Assert.IsTrue(data.TryGetProperty("items", out _));
        Assert.IsTrue(data.TryGetProperty("totalCount", out _));
        Assert.IsTrue(data.TryGetProperty("page", out _));
        Assert.IsTrue(data.TryGetProperty("pageSize", out _));
    }

    [TestMethod]
    public async Task GetActiveCodes_PageSizeClamped_To100()
    {
        using var client = _factory.CreateUnauthenticatedClient();
        var response = await client.GetAsync("/api/promo-codes/active?page=1&pageSize=500");
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

        var body = JsonSerializer.Deserialize<JsonElement>(
            await response.Content.ReadAsStringAsync(), _json);
        if (body.TryGetProperty("data", out var data) &&
            data.TryGetProperty("items", out var items) &&
            items.ValueKind == JsonValueKind.Array)
        {
            Assert.IsTrue(items.GetArrayLength() <= 100, "pageSize must be clamped to 100");
        }
    }

    // ────────────────────────────────────────────────
    // GET /api/promo-codes  (admin only)
    // ────────────────────────────────────────────────

    [TestMethod]
    public async Task GetAllPromoCodes_Anonymous_Returns401()
    {
        using var client = _factory.CreateUnauthenticatedClient();
        var response = await client.GetAsync("/api/promo-codes");
        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [TestMethod]
    public async Task GetAllPromoCodes_CustomerRole_Returns403()
    {
        using var client = _factory.CreateAuthenticatedClient();
        var response = await client.GetAsync("/api/promo-codes");
        Assert.AreEqual(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [TestMethod]
    public async Task GetAllPromoCodes_AdminRole_Returns200WithPaginatedShape()
    {
        using var client = _factory.CreateAdminClient();
        var body = JsonSerializer.Deserialize<JsonElement>(
            await (await client.GetAsync("/api/promo-codes?page=1&pageSize=10")).Content.ReadAsStringAsync(), _json);
        Assert.IsTrue(body.TryGetProperty("data", out var data));
        Assert.IsTrue(data.TryGetProperty("items", out _));
        Assert.IsTrue(data.TryGetProperty("totalCount", out _));
    }

    [TestMethod]
    public async Task GetAllPromoCodes_SearchFilter_Returns200()
    {
        using var client = _factory.CreateAdminClient();
        var response = await client.GetAsync("/api/promo-codes?search=SAVE");
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
    }

    [TestMethod]
    public async Task GetAllPromoCodes_IsActiveFilter_Returns200()
    {
        using var client = _factory.CreateAdminClient();
        var response = await client.GetAsync("/api/promo-codes?isActive=true");
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
    }

    // ────────────────────────────────────────────────
    // GET /api/promo-codes/{id}  (admin only)
    // ────────────────────────────────────────────────

    [TestMethod]
    public async Task GetPromoCodeById_Anonymous_Returns401()
    {
        using var client = _factory.CreateUnauthenticatedClient();
        var response = await client.GetAsync($"/api/promo-codes/{SeededPromoCodeId}");
        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [TestMethod]
    public async Task GetPromoCodeById_CustomerRole_Returns403()
    {
        using var client = _factory.CreateAuthenticatedClient();
        var response = await client.GetAsync($"/api/promo-codes/{SeededPromoCodeId}");
        Assert.AreEqual(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [TestMethod]
    public async Task GetPromoCodeById_Admin_SeededId_Returns200WithCorrectShape()
    {
        using var client = _factory.CreateAdminClient();
        var response = await client.GetAsync($"/api/promo-codes/{SeededPromoCodeId}");
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

        var data = JsonSerializer.Deserialize<JsonElement>(
            await response.Content.ReadAsStringAsync(), _json).GetProperty("data");

        Assert.AreEqual("SAVE20", data.GetProperty("code").GetString());
        Assert.IsTrue(data.GetProperty("isActive").GetBoolean());
        Assert.IsTrue(data.TryGetProperty("discountType", out _));
        Assert.IsTrue(data.TryGetProperty("discountValue", out _));
        Assert.IsTrue(data.TryGetProperty("usedCount", out _));
        Assert.IsTrue(data.TryGetProperty("createdAt", out _));
        Assert.IsTrue(data.TryGetProperty("updatedAt", out _));
    }

    [TestMethod]
    public async Task GetPromoCodeById_Admin_UnknownId_Returns404WithErrorCode()
    {
        using var client = _factory.CreateAdminClient();
        var response = await client.GetAsync($"/api/promo-codes/{Guid.NewGuid()}");
        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);

        var body = JsonSerializer.Deserialize<JsonElement>(
            await response.Content.ReadAsStringAsync(), _json);
        Assert.IsTrue(body.TryGetProperty("errorCode", out var code));
        Assert.AreEqual("PROMO_CODE_NOT_FOUND", code.GetString());
    }

    // ────────────────────────────────────────────────
    // POST /api/promo-codes  (admin only, returns 201 Created)
    // ────────────────────────────────────────────────

    [TestMethod]
    public async Task CreatePromoCode_Anonymous_Returns401()
    {
        using var client = _factory.CreateUnauthenticatedClient();
        var response = await client.PostAsync("/api/promo-codes", BuildCreateBody(UniqueCode(), "Percentage", 10));
        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [TestMethod]
    public async Task CreatePromoCode_CustomerRole_Returns403()
    {
        using var client = _factory.CreateAuthenticatedClient();
        var response = await client.PostAsync("/api/promo-codes", BuildCreateBody(UniqueCode(), "Percentage", 10));
        Assert.AreEqual(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [TestMethod]
    public async Task CreatePromoCode_Admin_ValidData_Returns201WithLocationHeader()
    {
        using var client = _factory.CreateAdminClient();
        var response = await client.PostAsync("/api/promo-codes", BuildCreateBody(UniqueCode(), "Percentage", 15));
        Assert.AreEqual(HttpStatusCode.Created, response.StatusCode,
            $"Body: {await response.Content.ReadAsStringAsync()}");
        Assert.IsNotNull(response.Headers.Location, "201 must include Location header");
    }

    [TestMethod]
    public async Task CreatePromoCode_Admin_CodeNormalized_ToUppercase()
    {
        using var client = _factory.CreateAdminClient();
        var rawCode = "lower-" + Guid.NewGuid().ToString("N")[..5];
        var response = await client.PostAsync("/api/promo-codes", BuildCreateBody(rawCode, "Percentage", 10));
        Assert.AreEqual(HttpStatusCode.Created, response.StatusCode);

        var data = JsonSerializer.Deserialize<JsonElement>(
            await response.Content.ReadAsStringAsync(), _json).GetProperty("data");
        Assert.AreEqual(rawCode.Trim().ToUpperInvariant(), data.GetProperty("code").GetString());
    }

    [TestMethod]
    public async Task CreatePromoCode_Admin_DuplicateCode_Returns409WithDuplicateErrorCode()
    {
        using var client = _factory.CreateAdminClient();
        var code = UniqueCode();
        var first = await client.PostAsync("/api/promo-codes", BuildCreateBody(code, "Percentage", 10));
        Assert.AreEqual(HttpStatusCode.Created, first.StatusCode);

        var second = await client.PostAsync("/api/promo-codes", BuildCreateBody(code, "Percentage", 10));
        Assert.AreEqual(HttpStatusCode.Conflict, second.StatusCode);

        var body = JsonSerializer.Deserialize<JsonElement>(
            await second.Content.ReadAsStringAsync(), _json);
        Assert.AreEqual("DUPLICATE_PROMO_CODE", body.GetProperty("errorCode").GetString());
    }

    [TestMethod]
    public async Task CreatePromoCode_Admin_FixedDiscount_Returns201()
    {
        using var client = _factory.CreateAdminClient();
        var response = await client.PostAsync("/api/promo-codes", BuildCreateBody(UniqueCode(), "Fixed", 25));
        Assert.AreEqual(HttpStatusCode.Created, response.StatusCode);
    }

    // ────────────────────────────────────────────────
    // PUT /api/promo-codes/{id}  (admin only)
    // ────────────────────────────────────────────────

    [TestMethod]
    public async Task UpdatePromoCode_Anonymous_Returns401()
    {
        using var client = _factory.CreateUnauthenticatedClient();
        var response = await client.PutAsync($"/api/promo-codes/{SeededPromoCodeId}",
            new StringContent("{}", Encoding.UTF8, "application/json"));
        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [TestMethod]
    public async Task UpdatePromoCode_Admin_SeededId_Returns200()
    {
        using var client = _factory.CreateAdminClient();
        var dto = new { IsActive = true };
        var response = await client.PutAsync($"/api/promo-codes/{SeededPromoCodeId}",
            new StringContent(JsonSerializer.Serialize(dto), Encoding.UTF8, "application/json"));
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
    }

    [TestMethod]
    public async Task UpdatePromoCode_Admin_UnknownId_Returns404()
    {
        using var client = _factory.CreateAdminClient();
        var dto = new { IsActive = false };
        var response = await client.PutAsync($"/api/promo-codes/{Guid.NewGuid()}",
            new StringContent(JsonSerializer.Serialize(dto), Encoding.UTF8, "application/json"));
        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ────────────────────────────────────────────────
    // PUT /api/promo-codes/{id}/deactivate  (admin only)
    // ────────────────────────────────────────────────

    [TestMethod]
    public async Task DeactivatePromoCode_Anonymous_Returns401()
    {
        using var client = _factory.CreateUnauthenticatedClient();
        var response = await client.PutAsync($"/api/promo-codes/{SeededPromoCodeId}/deactivate", null);
        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [TestMethod]
    public async Task DeactivatePromoCode_Admin_ExistingCode_Returns200()
    {
        using var client = _factory.CreateAdminClient();
        // Create a fresh code to deactivate — don't deactivate SAVE20 as it would break other tests
        var code = UniqueCode();
        var created = await client.PostAsync("/api/promo-codes", BuildCreateBody(code, "Percentage", 5));
        Assert.AreEqual(HttpStatusCode.Created, created.StatusCode);
        var id = JsonSerializer.Deserialize<JsonElement>(
            await created.Content.ReadAsStringAsync(), _json)
            .GetProperty("data").GetProperty("id").GetGuid();

        var response = await client.PutAsync($"/api/promo-codes/{id}/deactivate", null);
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
    }

    [TestMethod]
    public async Task DeactivatePromoCode_Admin_UnknownId_Returns404()
    {
        using var client = _factory.CreateAdminClient();
        var response = await client.PutAsync($"/api/promo-codes/{Guid.NewGuid()}/deactivate", null);
        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ────────────────────────────────────────────────
    // DELETE /api/promo-codes/{id}  (admin only)
    // ────────────────────────────────────────────────

    [TestMethod]
    public async Task DeletePromoCode_Anonymous_Returns401()
    {
        using var client = _factory.CreateUnauthenticatedClient();
        var response = await client.DeleteAsync($"/api/promo-codes/{Guid.NewGuid()}");
        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [TestMethod]
    public async Task DeletePromoCode_CustomerRole_Returns403()
    {
        using var client = _factory.CreateAuthenticatedClient();
        var response = await client.DeleteAsync($"/api/promo-codes/{Guid.NewGuid()}");
        Assert.AreEqual(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [TestMethod]
    public async Task DeletePromoCode_Admin_UnknownId_Returns404()
    {
        using var client = _factory.CreateAdminClient();
        var response = await client.DeleteAsync($"/api/promo-codes/{Guid.NewGuid()}");
        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
    }

    [TestMethod]
    public async Task DeletePromoCode_Admin_ExistingCode_Returns200()
    {
        using var client = _factory.CreateAdminClient();
        var code = UniqueCode();
        var created = await client.PostAsync("/api/promo-codes", BuildCreateBody(code, "Fixed", 5));
        Assert.AreEqual(HttpStatusCode.Created, created.StatusCode);
        var id = JsonSerializer.Deserialize<JsonElement>(
            await created.Content.ReadAsStringAsync(), _json)
            .GetProperty("data").GetProperty("id").GetGuid();

        var response = await client.DeleteAsync($"/api/promo-codes/{id}");
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
    }

    // ────────────────────────────────────────────────
    // POST /api/promo-codes/validate  (anonymous, always 200)
    // ────────────────────────────────────────────────

    [TestMethod]
    public async Task ValidatePromoCode_Anonymous_AlwaysReturns200_EvenForUnknownCode()
    {
        using var client = _factory.CreateUnauthenticatedClient();
        var response = await client.PostAsync("/api/promo-codes/validate",
            new StringContent("{\"code\":\"DOESNOTEXIST\",\"orderAmount\":100}", Encoding.UTF8, "application/json"));
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
    }

    [TestMethod]
    public async Task ValidatePromoCode_ValidCode_IsValidTrue_CorrectDiscountAmount()
    {
        using var client = _factory.CreateUnauthenticatedClient();
        var response = await client.PostAsync("/api/promo-codes/validate",
            new StringContent("{\"code\":\"SAVE20\",\"orderAmount\":100}", Encoding.UTF8, "application/json"));
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

        var data = JsonSerializer.Deserialize<JsonElement>(
            await response.Content.ReadAsStringAsync(), _json).GetProperty("data");
        Assert.IsTrue(data.GetProperty("isValid").GetBoolean());
        Assert.AreEqual(20m, data.GetProperty("discountAmount").GetDecimal(), "20% of 100 = 20");
    }

    [TestMethod]
    public async Task ValidatePromoCode_UnknownCode_IsValidFalse_DiscountZero()
    {
        using var client = _factory.CreateUnauthenticatedClient();
        var response = await client.PostAsync("/api/promo-codes/validate",
            new StringContent("{\"code\":\"FAKECODE999\",\"orderAmount\":100}", Encoding.UTF8, "application/json"));

        var data = JsonSerializer.Deserialize<JsonElement>(
            await response.Content.ReadAsStringAsync(), _json).GetProperty("data");
        Assert.IsFalse(data.GetProperty("isValid").GetBoolean());
        Assert.AreEqual(0m, data.GetProperty("discountAmount").GetDecimal());
    }

    [TestMethod]
    public async Task ValidatePromoCode_LowercaseCode_MatchesCaseInsensitively()
    {
        using var client = _factory.CreateUnauthenticatedClient();
        var response = await client.PostAsync("/api/promo-codes/validate",
            new StringContent("{\"code\":\"save20\",\"orderAmount\":100}", Encoding.UTF8, "application/json"));

        var data = JsonSerializer.Deserialize<JsonElement>(
            await response.Content.ReadAsStringAsync(), _json).GetProperty("data");
        Assert.IsTrue(data.GetProperty("isValid").GetBoolean(), "Code lookup must be case-insensitive");
    }

    [TestMethod]
    public async Task ValidatePromoCode_BelowMinOrderAmount_IsValidFalse()
    {
        using var client = _factory.CreateAdminClient();
        var code = UniqueCode();
        var dto = new { Code = code, DiscountType = "Percentage", DiscountValue = 10, MinOrderAmount = 50m };
        var created = await client.PostAsync("/api/promo-codes",
            new StringContent(JsonSerializer.Serialize(dto), Encoding.UTF8, "application/json"));
        Assert.AreEqual(HttpStatusCode.Created, created.StatusCode);

        using var anon = _factory.CreateUnauthenticatedClient();
        var response = await anon.PostAsync("/api/promo-codes/validate",
            new StringContent($"{{\"code\":\"{code}\",\"orderAmount\":30}}", Encoding.UTF8, "application/json"));
        var data = JsonSerializer.Deserialize<JsonElement>(
            await response.Content.ReadAsStringAsync(), _json).GetProperty("data");
        Assert.IsFalse(data.GetProperty("isValid").GetBoolean());
    }

    [TestMethod]
    public async Task ValidatePromoCode_ResponseShape_HasIsValidDiscountAmountMessage()
    {
        using var client = _factory.CreateUnauthenticatedClient();
        var response = await client.PostAsync("/api/promo-codes/validate",
            new StringContent("{\"code\":\"SAVE20\",\"orderAmount\":100}", Encoding.UTF8, "application/json"));
        var data = JsonSerializer.Deserialize<JsonElement>(
            await response.Content.ReadAsStringAsync(), _json).GetProperty("data");

        Assert.IsTrue(data.TryGetProperty("isValid", out _));
        Assert.IsTrue(data.TryGetProperty("discountAmount", out _));
        Assert.IsTrue(data.TryGetProperty("message", out _));
    }

    // ────────────────────────────────────────────────
    // Helpers
    // ────────────────────────────────────────────────

    private static StringContent BuildCreateBody(string code, string discountType, decimal discountValue)
    {
        var dto = new { Code = code, DiscountType = discountType, DiscountValue = discountValue };
        return new StringContent(JsonSerializer.Serialize(dto), Encoding.UTF8, "application/json");
    }

    /// Generates a unique uppercase code that satisfies the 3–20 char, A-Z0-9 rule.
    private static string UniqueCode() =>
        ("T" + Guid.NewGuid().ToString("N")[..11]).ToUpperInvariant();
}
```

---

## Run before migration

```bash
cd src/backend
dotnet test ECommerce.Tests --filter "FullyQualifiedName~PromoCodeCharacterizationTests" --logger "console;verbosity=normal"
```

All tests must be green before you touch any production code.

---

## Acceptance Criteria

- [ ] All tests pass against the OLD `PromoCodeService`
- [ ] `SAVE20` seeded data confirmed: GetById 200, validate isValid=true, discountAmount=20 for orderAmount=100
- [ ] Auth boundaries pinned: anonymous → 401, Customer → 403 on admin endpoints
- [ ] `POST /validate` always returns 200 regardless of code validity
- [ ] Code normalization pinned: lowercase stored and matched as uppercase
- [ ] `DUPLICATE_PROMO_CODE` → 409, `PROMO_CODE_NOT_FOUND` → 404
- [ ] 201 Created with Location header on successful create
