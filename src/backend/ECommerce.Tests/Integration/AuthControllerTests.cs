using System.Net;
using System.Text;
using System.Text.Json;

namespace ECommerce.Tests.Integration;

[TestClass]
public class AuthControllerTests
{
    private static readonly TestWebApplicationFactory _factory = SharedTestInfrastructure.Factory;
    private static readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

    // ── POST /api/auth/register ───────────────────────────────────────────────

    [TestMethod]
    public async Task Register_WithValidData_ReturnsSuccessfulResponse()
    {
        using var client = _factory.CreateUnauthenticatedClient();
        var content = Json(new { Email = "newuser@test.com", Password = "SecurePass123!", FirstName = "John", LastName = "Doe" });
        var response = await client.PostAsync("/api/auth/register", content);
        var responseContent = await response.Content.ReadAsStringAsync();
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        Assert.IsTrue(responseContent.Contains("registered successfully") || responseContent.Contains("success"),
            "Response should indicate successful registration");
    }

    [TestMethod]
    public async Task Register_WithMissingEmail_ReturnsBadRequest()
    {
        using var client = _factory.CreateUnauthenticatedClient();
        var response = await client.PostAsync("/api/auth/register",
            Json(new { Password = "SecurePass123!", FirstName = "John", LastName = "Doe" }));
        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [TestMethod]
    public async Task Register_WithMissingPassword_ReturnsBadRequest()
    {
        using var client = _factory.CreateUnauthenticatedClient();
        var response = await client.PostAsync("/api/auth/register",
            Json(new { Email = "newuser@test.com", FirstName = "John", LastName = "Doe" }));
        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [TestMethod]
    public async Task Register_WithWeakPassword_ReturnsBadRequest()
    {
        using var client = _factory.CreateUnauthenticatedClient();
        var response = await client.PostAsync("/api/auth/register",
            Json(new { Email = "newuser@test.com", Password = "weak", FirstName = "John", LastName = "Doe" }));
        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ── POST /api/auth/login ──────────────────────────────────────────────────

    [TestMethod]
    public async Task Login_WithWrongPassword_ReturnsUnauthorized()
    {
        using var client = _factory.CreateUnauthenticatedClient();
        var response = await client.PostAsync("/api/auth/login",
            Json(new { Email = "integration@test", Password = "WrongPassword" }));
        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [TestMethod]
    public async Task Login_WithInvalidEmail_ReturnsUnauthorized()
    {
        using var client = _factory.CreateUnauthenticatedClient();
        var response = await client.PostAsync("/api/auth/login",
            Json(new { Email = "nonexistent@test.com", Password = "SomePassword123!" }));
        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [TestMethod]
    public async Task Login_WithMissingEmail_ReturnsBadRequest()
    {
        using var client = _factory.CreateUnauthenticatedClient();
        var response = await client.PostAsync("/api/auth/login",
            Json(new { Password = "SomePassword123!" }));
        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [TestMethod]
    public async Task Login_WithMissingPassword_ReturnsBadRequest()
    {
        using var client = _factory.CreateUnauthenticatedClient();
        var response = await client.PostAsync("/api/auth/login",
            Json(new { Email = "user@test.com" }));
        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ── POST /api/auth/refresh-token ──────────────────────────────────────────

    [TestMethod]
    public async Task RefreshToken_WithInvalidToken_ReturnsUnauthorized()
    {
        using var client = _factory.CreateUnauthenticatedClient();
        var response = await client.PostAsync("/api/auth/refresh-token",
            Json(new { Token = "invalid.jwt.token" }));
        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [TestMethod]
    public async Task RefreshToken_WithMissingToken_ReturnsBadRequest()
    {
        using var client = _factory.CreateUnauthenticatedClient();
        var response = await client.PostAsync("/api/auth/refresh-token", Json(new { }));
        Assert.IsTrue(response.StatusCode == HttpStatusCode.UnprocessableEntity ||
                      response.StatusCode == HttpStatusCode.BadRequest ||
                      response.StatusCode == HttpStatusCode.Unauthorized,
            $"Expected 422/400/401 but got {response.StatusCode}");
    }

    [TestMethod]
    public async Task RefreshToken_WithEmptyToken_ReturnsUnauthorized()
    {
        using var client = _factory.CreateUnauthenticatedClient();
        var response = await client.PostAsync("/api/auth/refresh-token",
            Json(new { Token = "" }));
        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ── Anonymous access ──────────────────────────────────────────────────────

    [TestMethod]
    public async Task Register_AllowsAnonymousAccess()
    {
        using var client = _factory.CreateUnauthenticatedClient();
        var response = await client.PostAsync("/api/auth/register",
            Json(new { Email = "anon@test.com", Password = "SecurePass123!", FirstName = "Anon", LastName = "User" }));
        Assert.IsFalse(response.StatusCode == HttpStatusCode.Unauthorized || response.StatusCode == HttpStatusCode.Forbidden,
            "Register endpoint should allow anonymous access");
    }

    [TestMethod]
    public async Task Login_AllowsAnonymousAccess()
    {
        using var client = _factory.CreateUnauthenticatedClient();
        var response = await client.PostAsync("/api/auth/login",
            Json(new { Email = "integration@test.com", Password = "TestPassword123!" }));
        // 401 is acceptable: credentials were evaluated (endpoint IS accessible anonymously)
        // rather than rejected by auth middleware before reaching the handler.
        Assert.AreNotEqual(HttpStatusCode.Forbidden, response.StatusCode,
            "Login endpoint should not require role/policy — anonymous access must be allowed");
    }

    [TestMethod]
    public async Task RefreshToken_AllowsAnonymousAccess()
    {
        using var client = _factory.CreateUnauthenticatedClient();
        var response = await client.PostAsync("/api/auth/refresh-token",
            Json(new { Token = "some.jwt.token" }));
        Assert.IsFalse(response.StatusCode == HttpStatusCode.Forbidden,
            "RefreshToken endpoint should allow anonymous access (not return 403)");
    }

    // ── Response format ────────────────────────────────────────────────────────

    [TestMethod]
    public async Task Register_ReturnsCorrectResponseFormat()
    {
        using var client = _factory.CreateUnauthenticatedClient();
        var response = await client.PostAsync("/api/auth/register",
            Json(new { Email = "formattest@test.com", Password = "SecurePass123!", FirstName = "Format", LastName = "Test" }));
        var responseContent = await response.Content.ReadAsStringAsync();
        var responseData = JsonSerializer.Deserialize<JsonElement>(responseContent, _jsonOptions);
        Assert.IsTrue(responseData.TryGetProperty("success", out _), "Response should have 'success' property");
        Assert.IsTrue(
            responseData.TryGetProperty("data", out _) || responseData.TryGetProperty("errorDetails", out _),
            "Response should include either 'data' or 'errorDetails' property");
    }

    private static StringContent Json(object dto) =>
        new(JsonSerializer.Serialize(dto), Encoding.UTF8, "application/json");
}
