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
        var payload = new { Email = $"char-test-{Guid.NewGuid():N}@example.com", Password = "SecurePass1!", FirstName = "Jane", LastName = "Doe" };

        var res = await client.PostAsync("/api/auth/register", Json(payload), TestContext.CancellationToken);

        Assert.AreEqual(HttpStatusCode.OK, res.StatusCode);
    }

    [TestMethod]
    public async Task Register_MissingEmail_Returns400()
    {
        using var client = _factory.CreateUnauthenticatedClient();
        var payload = new { Password = "SecurePass1!", FirstName = "Jane", LastName = "Doe" };

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
        var payload = new { Email = "integration@test", Password = "SecurePass1!", FirstName = "Jane", LastName = "Doe" };

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
        // Use the seeded admin user (TestWebApplicationFactory seeds admin@test)
        using var client = _factory.CreateUnauthenticatedClient();
        var payload = new { Email = "admin@test", Password = "TestPassword123!" };

        var res = await client.PostAsync("/api/auth/login", Json(payload), TestContext.CancellationToken);
        string body = await res.Content.ReadAsStringAsync();

        Assert.AreEqual(HttpStatusCode.OK, res.StatusCode);
        // Tokens are set as httpOnly cookies, so check for cookies or success response shape
        var hasAccessTokenCookie = res.Headers.TryGetValues("Set-Cookie", out var cookies)
            && cookies.Any(c => c.Contains("accessToken"));
        var hasSuccessResponse = body.Contains("\"success\":true") || body.Contains("\"Success\":true");
        Assert.IsTrue(hasAccessTokenCookie || hasSuccessResponse,
            "Login response must contain token (as cookie) or success indicator");
    }

    [TestMethod]
    public async Task Login_WrongPassword_Returns401()
    {
        using var client = _factory.CreateUnauthenticatedClient();
        var payload = new { Email = "admin@test", Password = "WrongPass999!" };

        var res = await client.PostAsync("/api/auth/login", Json(payload), TestContext.CancellationToken);

        Assert.AreEqual(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [TestMethod]
    public async Task Login_NonexistentEmail_Returns401()
    {
        using var client = _factory.CreateUnauthenticatedClient();
        var payload = new { Email = "nobody@nowhere.com", Password = "SomePass1!" };

        var res = await client.PostAsync("/api/auth/login", Json(payload), TestContext.CancellationToken);

        // Must NOT return 404 (don't reveal if email exists — security requirement)
        Assert.AreEqual(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [TestMethod]
    public async Task Login_MissingEmail_Returns400()
    {
        using var client = _factory.CreateUnauthenticatedClient();
        var payload = new { Password = "SomePass1!" };

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
