# Phase 2, Step 0: Identity Characterization Tests

**Do this BEFORE touching any migration code.** These tests pin the existing HTTP contract so you can verify nothing regressed after cutover.

**Prerequisite**: Phase 1 (Catalog) complete and all tests pass.

---

## Context

The existing `AuthControllerTests.cs` and `ProfileControllerTests.cs` already exist but some assertions are loose (e.g. "OK or NotFound"). We create dedicated `AuthCharacterizationTests.cs` and `ProfileCharacterizationTests.cs` that lock down the EXACT contract: status codes, response shape, error codes. These pass against the OLD `AuthService`/`UserService` before migration, and must still pass after cutover to MediatR handlers.

---

## Task: Create Characterization Tests in ECommerce.Tests

### Add to existing project — no new project needed

Files go in `src/backend/ECommerce.Tests/Integration/`.

---

### File: `AuthCharacterizationTests.cs`

```csharp
using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ECommerce.Tests.Integration;

/// <summary>
/// Characterization tests for AuthController.
/// Written BEFORE Phase 2 migration — pin the exact HTTP contract.
/// These tests must pass against both old AuthService AND new MediatR handlers.
/// </summary>
[TestClass]
public class AuthCharacterizationTests
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

    // ── Register ─────────────────────────────────────────────────────────────

    [TestMethod]
    public async Task Register_ValidData_Returns200()
    {
        using var client = _factory.CreateUnauthenticatedClient();
        var payload = new { Email = $"char-test-{Guid.NewGuid():N}@example.com", Password = "SecurePass1", FirstName = "Jane", LastName = "Doe" };

        var res = await client.PostAsync("/api/auth/register", Json(payload), TestContext.CancellationToken);

        Assert.AreEqual(HttpStatusCode.OK, res.StatusCode);
    }

    [TestMethod]
    public async Task Register_MissingEmail_Returns400()
    {
        using var client = _factory.CreateUnauthenticatedClient();
        var payload = new { Password = "SecurePass1", FirstName = "Jane", LastName = "Doe" };

        var res = await client.PostAsync("/api/auth/register", Json(payload), TestContext.CancellationToken);

        Assert.AreEqual(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [TestMethod]
    public async Task Register_MissingPassword_Returns400()
    {
        using var client = _factory.CreateUnauthenticatedClient();
        var payload = new { Email = "test@example.com", FirstName = "Jane", LastName = "Doe" };

        var res = await client.PostAsync("/api/auth/register", Json(payload), TestContext.CancellationToken);

        Assert.AreEqual(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [TestMethod]
    public async Task Register_WeakPassword_Returns400()
    {
        using var client = _factory.CreateUnauthenticatedClient();
        var payload = new { Email = "test@example.com", Password = "abc", FirstName = "Jane", LastName = "Doe" };

        var res = await client.PostAsync("/api/auth/register", Json(payload), TestContext.CancellationToken);

        Assert.AreEqual(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [TestMethod]
    public async Task Register_DuplicateEmail_ReturnsConflictOrUnprocessable()
    {
        using var client = _factory.CreateUnauthenticatedClient();
        // Use the seeded test user email (already exists in TestWebApplicationFactory seed)
        var payload = new { Email = "integration@test.com", Password = "SecurePass1", FirstName = "Jane", LastName = "Doe" };

        var res = await client.PostAsync("/api/auth/register", Json(payload), TestContext.CancellationToken);

        Assert.IsTrue(
            res.StatusCode == HttpStatusCode.Conflict
            || res.StatusCode == HttpStatusCode.UnprocessableEntity,
            $"Expected 409 or 422, got {(int)res.StatusCode}");
    }

    // ── Login ─────────────────────────────────────────────────────────────────

    [TestMethod]
    public async Task Login_ValidCredentials_Returns200WithToken()
    {
        // Use the seeded admin user (TestWebApplicationFactory seeds admin@test.com)
        using var client = _factory.CreateUnauthenticatedClient();
        var payload = new { Email = "admin@test.com", Password = "Admin123!" };

        var res = await client.PostAsync("/api/auth/login", Json(payload), TestContext.CancellationToken);
        string body = await res.Content.ReadAsStringAsync();

        Assert.AreEqual(HttpStatusCode.OK, res.StatusCode);
        // Response must contain a token field somewhere
        Assert.IsTrue(body.Contains("token") || body.Contains("accessToken"), "Login response must contain a token");
    }

    [TestMethod]
    public async Task Login_WrongPassword_Returns401()
    {
        using var client = _factory.CreateUnauthenticatedClient();
        var payload = new { Email = "admin@test.com", Password = "WrongPass999" };

        var res = await client.PostAsync("/api/auth/login", Json(payload), TestContext.CancellationToken);

        Assert.AreEqual(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [TestMethod]
    public async Task Login_NonexistentEmail_Returns401()
    {
        using var client = _factory.CreateUnauthenticatedClient();
        var payload = new { Email = "nobody@nowhere.com", Password = "SomePass1" };

        var res = await client.PostAsync("/api/auth/login", Json(payload), TestContext.CancellationToken);

        // Must NOT return 404 (don't reveal if email exists — security requirement)
        Assert.AreEqual(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [TestMethod]
    public async Task Login_MissingEmail_Returns400()
    {
        using var client = _factory.CreateUnauthenticatedClient();
        var payload = new { Password = "SomePass1" };

        var res = await client.PostAsync("/api/auth/login", Json(payload), TestContext.CancellationToken);

        Assert.AreEqual(HttpStatusCode.BadRequest, res.StatusCode);
    }

    // ── Auth required endpoints ───────────────────────────────────────────────

    [TestMethod]
    public async Task GetCurrentUser_Authenticated_Returns200()
    {
        using var client = _factory.CreateAuthenticatedClient();

        var res = await client.GetAsync("/api/auth/me", TestContext.CancellationToken);

        Assert.AreEqual(HttpStatusCode.OK, res.StatusCode);
    }

    [TestMethod]
    public async Task GetCurrentUser_Unauthenticated_Returns401()
    {
        using var client = _factory.CreateUnauthenticatedClient();

        var res = await client.GetAsync("/api/auth/me", TestContext.CancellationToken);

        Assert.AreEqual(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [TestMethod]
    public async Task ForgotPassword_AnyEmail_Returns200()
    {
        // ALWAYS returns 200 regardless — security: don't reveal if email exists
        using var client = _factory.CreateUnauthenticatedClient();
        var payload = new { Email = "doesnotexist@nowhere.com" };

        var res = await client.PostAsync("/api/auth/forgot-password", Json(payload), TestContext.CancellationToken);

        Assert.AreEqual(HttpStatusCode.OK, res.StatusCode);
    }

    [TestMethod]
    public async Task ForgotPassword_MissingEmail_Returns400()
    {
        using var client = _factory.CreateUnauthenticatedClient();
        var payload = new { };

        var res = await client.PostAsync("/api/auth/forgot-password", Json(payload), TestContext.CancellationToken);

        Assert.AreEqual(HttpStatusCode.BadRequest, res.StatusCode);
    }
}
```

---

### File: `ProfileCharacterizationTests.cs`

```csharp
using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ECommerce.Tests.Integration;

/// <summary>
/// Characterization tests for ProfileController.
/// Written BEFORE Phase 2 migration — pin the exact HTTP contract.
/// These tests must pass against both old UserService AND new MediatR handlers.
/// </summary>
[TestClass]
public class ProfileCharacterizationTests
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

    // ── Get Profile ───────────────────────────────────────────────────────────

    [TestMethod]
    public async Task GetProfile_Authenticated_Returns200()
    {
        using var client = _factory.CreateAuthenticatedClient();

        var res = await client.GetAsync("/api/profile", TestContext.CancellationToken);

        Assert.AreEqual(HttpStatusCode.OK, res.StatusCode);
    }

    [TestMethod]
    public async Task GetProfile_Unauthenticated_Returns401()
    {
        using var client = _factory.CreateUnauthenticatedClient();

        var res = await client.GetAsync("/api/profile", TestContext.CancellationToken);

        Assert.AreEqual(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [TestMethod]
    public async Task GetProfile_ResponseShape_HasExpectedFields()
    {
        using var client = _factory.CreateAuthenticatedClient();

        var res  = await client.GetAsync("/api/profile", TestContext.CancellationToken);
        string body = await res.Content.ReadAsStringAsync();
        var json = JsonSerializer.Deserialize<JsonElement>(body, _jsonOptions);

        Assert.AreEqual(HttpStatusCode.OK, res.StatusCode);
        Assert.IsTrue(json.TryGetProperty("data", out var data), "Response must have 'data' property");
        // Profile must expose at minimum: id, email, firstName, lastName
        Assert.IsTrue(
            data.TryGetProperty("email", out _) || data.TryGetProperty("Email", out _),
            "Profile data must contain email");
    }

    // ── Update Profile ────────────────────────────────────────────────────────

    [TestMethod]
    public async Task UpdateProfile_ValidData_Returns200()
    {
        using var client = _factory.CreateAuthenticatedClient();
        var payload = new { FirstName = "Updated", LastName = "User", PhoneNumber = "+1234567890" };

        var res = await client.PutAsync("/api/profile", Json(payload), TestContext.CancellationToken);

        Assert.AreEqual(HttpStatusCode.OK, res.StatusCode);
    }

    [TestMethod]
    public async Task UpdateProfile_MissingFirstName_Returns400()
    {
        using var client = _factory.CreateAuthenticatedClient();
        var payload = new { LastName = "User" };

        var res = await client.PutAsync("/api/profile", Json(payload), TestContext.CancellationToken);

        Assert.AreEqual(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [TestMethod]
    public async Task UpdateProfile_Unauthenticated_Returns401()
    {
        using var client = _factory.CreateUnauthenticatedClient();
        var payload = new { FirstName = "Test", LastName = "User" };

        var res = await client.PutAsync("/api/profile", Json(payload), TestContext.CancellationToken);

        Assert.AreEqual(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    // ── Change Password ───────────────────────────────────────────────────────

    [TestMethod]
    public async Task ChangePassword_WrongOldPassword_Returns400OrUnauthorized()
    {
        using var client = _factory.CreateAuthenticatedClient();
        var payload = new { OldPassword = "WrongOld1", NewPassword = "NewPass1" };

        var res = await client.PostAsync("/api/profile/change-password", Json(payload), TestContext.CancellationToken);

        Assert.IsTrue(
            res.StatusCode == HttpStatusCode.BadRequest
            || res.StatusCode == HttpStatusCode.Unauthorized
            || res.StatusCode == HttpStatusCode.UnprocessableEntity,
            $"Expected 400/401/422, got {(int)res.StatusCode}");
    }

    [TestMethod]
    public async Task ChangePassword_MissingFields_Returns400()
    {
        using var client = _factory.CreateAuthenticatedClient();
        var payload = new { };

        var res = await client.PostAsync("/api/profile/change-password", Json(payload), TestContext.CancellationToken);

        Assert.AreEqual(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [TestMethod]
    public async Task ChangePassword_Unauthenticated_Returns401()
    {
        using var client = _factory.CreateUnauthenticatedClient();
        var payload = new { OldPassword = "Old1", NewPassword = "New1" };

        var res = await client.PostAsync("/api/profile/change-password", Json(payload), TestContext.CancellationToken);

        Assert.AreEqual(HttpStatusCode.Unauthorized, res.StatusCode);
    }
}
```

---

## Run Before Starting Migration

```bash
cd src/backend
dotnet test ECommerce.Tests/ECommerce.Tests.csproj \
    --filter "FullyQualifiedName~AuthCharacterizationTests|FullyQualifiedName~ProfileCharacterizationTests"
```

All must pass. If any fail, fix the test (wrong assumption about existing behavior) before starting migration.

---

## Acceptance Criteria

- [ ] `AuthCharacterizationTests.cs` created in `ECommerce.Tests/Integration/`
- [ ] `ProfileCharacterizationTests.cs` created in `ECommerce.Tests/Integration/`
- [ ] All characterization tests pass against the EXISTING service (before any migration code is written)
- [ ] Tests recorded in the Pre-Cutover checklist in `step-4-cutover.md`
