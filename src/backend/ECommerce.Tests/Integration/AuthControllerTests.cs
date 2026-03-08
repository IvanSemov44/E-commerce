using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ECommerce.Application.DTOs.Auth;
using ECommerce.Application.DTOs.Common;

namespace ECommerce.Tests.Integration;

/// <summary>
/// Integration tests for AuthController endpoints.
/// Tests registration, login, token refresh, and authorization scenarios.
/// </summary>
[TestClass]
public class AuthControllerTests
{
    private TestWebApplicationFactory _factory = null!;
    private static readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

    [TestInitialize]
    public void Setup()
    {
        _factory = new TestWebApplicationFactory();
    }

    [TestCleanup]
    public void Cleanup()
    {
        _factory?.Dispose();
    }

    #region Register Tests

    [TestMethod]
    public async Task Register_WithValidData_ReturnsSuccessfulResponse()
    {
        // Arrange
        using var client = _factory.CreateUnauthenticatedClient();
        var registerDto = new
        {
            Email = "newuser@test.com",
            Password = "SecurePass123!",
            FirstName = "John",
            LastName = "Doe"
        };

        var content = new StringContent(JsonSerializer.Serialize(registerDto), Encoding.UTF8, "application/json");

        // Act
        var response = await client.PostAsync("/api/auth/register", content);
        var responseContent = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        Assert.IsTrue(responseContent.Contains("registered successfully") || responseContent.Contains("success"), "Response should indicate successful registration");
    }

    [TestMethod]
    public async Task Register_WithMissingEmail_ReturnsBadRequest()
    {
        // Arrange
        using var client = _factory.CreateUnauthenticatedClient();
        var registerDto = new
        {
            Password = "SecurePass123!",
            FirstName = "John",
            LastName = "Doe"
        };

        var content = new StringContent(JsonSerializer.Serialize(registerDto), Encoding.UTF8, "application/json");

        // Act
        var response = await client.PostAsync("/api/auth/register", content);

        // Assert
        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [TestMethod]
    public async Task Register_WithMissingPassword_ReturnsBadRequest()
    {
        // Arrange
        using var client = _factory.CreateUnauthenticatedClient();
        var registerDto = new
        {
            Email = "newuser@test.com",
            FirstName = "John",
            LastName = "Doe"
        };

        var content = new StringContent(JsonSerializer.Serialize(registerDto), Encoding.UTF8, "application/json");

        // Act
        var response = await client.PostAsync("/api/auth/register", content);

        // Assert
        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [TestMethod]
    public async Task Register_WithWeakPassword_ReturnsBadRequest()
    {
        // Arrange
        using var client = _factory.CreateUnauthenticatedClient();
        var registerDto = new
        {
            Email = "newuser@test.com",
            Password = "weak",  // Too weak
            FirstName = "John",
            LastName = "Doe"
        };

        var content = new StringContent(JsonSerializer.Serialize(registerDto), Encoding.UTF8, "application/json");

        // Act
        var response = await client.PostAsync("/api/auth/register", content);

        // Assert
        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
    }

    #endregion

    #region Login Tests

    [TestMethod]
    public async Task Login_WithWrongPassword_ReturnsUnauthorized()
    {
        // Arrange - Use unauthenticated client for login
        using var client = _factory.CreateUnauthenticatedClient();
        var loginDto = new
        {
            Email = "integration@test",
            Password = "WrongPassword"
        };

        var content = new StringContent(JsonSerializer.Serialize(loginDto), Encoding.UTF8, "application/json");

        // Act
        var response = await client.PostAsync("/api/auth/login", content);

        // Assert - Should fail due to wrong password
        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [TestMethod]
    public async Task Login_WithInvalidEmail_ReturnsUnauthorized()
    {
        // Arrange
        using var client = _factory.CreateUnauthenticatedClient();
        var loginDto = new
        {
            Email = "nonexistent@test.com",
            Password = "SomePassword123!"
        };

        var content = new StringContent(JsonSerializer.Serialize(loginDto), Encoding.UTF8, "application/json");

        // Act
        var response = await client.PostAsync("/api/auth/login", content);

        // Assert
        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [TestMethod]
    public async Task Login_WithMissingEmail_ReturnsBadRequest()
    {
        // Arrange
        using var client = _factory.CreateUnauthenticatedClient();
        var loginDto = new
        {
            Password = "SomePassword123!"
        };

        var content = new StringContent(JsonSerializer.Serialize(loginDto), Encoding.UTF8, "application/json");

        // Act
        var response = await client.PostAsync("/api/auth/login", content);

        // Assert
        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [TestMethod]
    public async Task Login_WithMissingPassword_ReturnsBadRequest()
    {
        // Arrange
        using var client = _factory.CreateUnauthenticatedClient();
        var loginDto = new
        {
            Email = "user@test.com"
        };

        var content = new StringContent(JsonSerializer.Serialize(loginDto), Encoding.UTF8, "application/json");

        // Act
        var response = await client.PostAsync("/api/auth/login", content);

        // Assert
        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
    }

    #endregion

    #region Refresh Token Tests

    [TestMethod]
    public async Task RefreshToken_WithInvalidToken_ReturnsUnauthorized()
    {
        // Arrange
        using var client = _factory.CreateUnauthenticatedClient();
        var refreshRequest = new
        {
            Token = "invalid.jwt.token"
        };

        var content = new StringContent(JsonSerializer.Serialize(refreshRequest), Encoding.UTF8, "application/json");

        // Act
        var response = await client.PostAsync("/api/auth/refresh-token", content);

        // Assert
        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [TestMethod]
    public async Task RefreshToken_WithMissingToken_ReturnsBadRequest()
    {
        // Arrange
        using var client = _factory.CreateUnauthenticatedClient();
        var refreshRequest = new { };

        var content = new StringContent(JsonSerializer.Serialize(refreshRequest), Encoding.UTF8, "application/json");

        // Act
        var response = await client.PostAsync("/api/auth/refresh-token", content);

        // Assert
        // Missing token is a validation error (422 Unprocessable Entity)
        // or a service error returning 401 Unauthorized
        Assert.IsTrue(response.StatusCode == HttpStatusCode.UnprocessableEntity ||
                      response.StatusCode == HttpStatusCode.BadRequest ||
                      response.StatusCode == HttpStatusCode.Unauthorized,
            $"Expected 422/400/401 but got {response.StatusCode}");
    }

    [TestMethod]
    public async Task RefreshToken_WithEmptyToken_ReturnsUnauthorized()
    {
        // Arrange
        using var client = _factory.CreateUnauthenticatedClient();
        var refreshRequest = new
        {
            Token = ""
        };

        var content = new StringContent(JsonSerializer.Serialize(refreshRequest), Encoding.UTF8, "application/json");

        // Act
        var response = await client.PostAsync("/api/auth/refresh-token", content);

        // Assert
        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    #endregion

    #region Authorization Tests

    [TestMethod]
    public async Task Register_AllowsAnonymousAccess()
    {
        // Arrange
        using var client = _factory.CreateUnauthenticatedClient();
        var registerDto = new
        {
            Email = "anon@test.com",
            Password = "SecurePass123!",
            FirstName = "Anon",
            LastName = "User"
        };

        var content = new StringContent(JsonSerializer.Serialize(registerDto), Encoding.UTF8, "application/json");

        // Act
        var response = await client.PostAsync("/api/auth/register", content);

        // Assert
        Assert.IsFalse(response.StatusCode == HttpStatusCode.Unauthorized || response.StatusCode == HttpStatusCode.Forbidden,
            "Register endpoint should allow anonymous access");
    }

    [TestMethod]
    public async Task Login_AllowsAnonymousAccess()
    {
        // Arrange - Use unauthenticated client for login
        using var client = _factory.CreateUnauthenticatedClient();
        var loginDto = new
        {
            Email = "integration@test",  // Use seeded test user
            Password = "TestPassword123!"
        };

        var content = new StringContent(JsonSerializer.Serialize(loginDto), Encoding.UTF8, "application/json");

        // Act
        var response = await client.PostAsync("/api/auth/login", content);

        // Assert - Login endpoint should allow anonymous access and return 200 with valid credentials
        Assert.IsFalse(response.StatusCode == HttpStatusCode.Unauthorized || response.StatusCode == HttpStatusCode.Forbidden,
            "Login endpoint should allow anonymous access");
    }

    [TestMethod]
    public async Task RefreshToken_AllowsAnonymousAccess()
    {
        // Arrange
        using var client = _factory.CreateUnauthenticatedClient();
        var refreshRequest = new
        {
            Token = "some.jwt.token"
        };

        var content = new StringContent(JsonSerializer.Serialize(refreshRequest), Encoding.UTF8, "application/json");

        // Act
        var response = await client.PostAsync("/api/auth/refresh-token", content);

        // Assert
        // Endpoint allows anonymous access (no authentication required to call it)
        // Invalid tokens will return 401, which is expected behavior
        // Success would be any response other than 403 Forbidden
        Assert.IsFalse(response.StatusCode == HttpStatusCode.Forbidden,
            "RefreshToken endpoint should allow anonymous access (not return 403)");
    }

    #endregion

    #region Response Format Tests

    [TestMethod]
    public async Task Register_ReturnsCorrectResponseFormat()
    {
        // Arrange
        using var client = _factory.CreateUnauthenticatedClient();
        var registerDto = new
        {
            Email = "formattest@test.com",
            Password = "SecurePass123!",
            FirstName = "Format",
            LastName = "Test"
        };

        var content = new StringContent(JsonSerializer.Serialize(registerDto), Encoding.UTF8, "application/json");

        // Act
        var response = await client.PostAsync("/api/auth/register", content);
        var responseContent = await response.Content.ReadAsStringAsync();

        // Assert
        var jsonOptions = _jsonOptions;
        var responseData = JsonSerializer.Deserialize<JsonElement>(responseContent, jsonOptions);

        // Verify ApiResponse<AuthResponseDto> structure
        Assert.IsTrue(responseData.TryGetProperty("success", out var success), "Response should have 'success' property");
        Assert.IsTrue(
            responseData.TryGetProperty("data", out var data) || responseData.TryGetProperty("errorDetails", out _),
            "Response should include either 'data' or 'errorDetails' property");
    }

    #endregion
}
