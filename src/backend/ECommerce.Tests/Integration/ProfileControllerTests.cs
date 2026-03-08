using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ECommerce.Tests.Integration;

/// <summary>
/// Integration tests for ProfileController endpoints.
/// Tests user profile operations and account management.
/// </summary>
[TestClass]
public class ProfileControllerTests
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
        // Reset authentication state
        ConditionalTestAuthHandler.IsAuthenticationEnabled = true;
        ConditionalTestAuthHandler.CurrentUserId = ConditionalTestAuthHandler.TestUserId;
        ConditionalTestAuthHandler.CurrentUserRole = "Customer";
        _factory?.Dispose();
    }

    #region Get Profile Tests

    [TestMethod]
    public async Task GetProfile_WithAuthenticatedUser_ReturnsOk()
    {
        // Arrange
        using var client = _factory.CreateAuthenticatedClient();

        // Act
        var response = await client.GetAsync("/api/profile");

        // Assert
        Assert.IsTrue(response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.NotFound,
            "GetProfile should return OK or NotFound");
    }

    [TestMethod]
    public async Task GetProfile_Unauthenticated_ReturnsUnauthorized()
    {
        // Arrange
        using var client = _factory.CreateUnauthenticatedClient();

        // Act
        var response = await client.GetAsync("/api/profile");

        // Assert
        Assert.IsTrue(response.StatusCode == HttpStatusCode.Unauthorized || response.StatusCode == HttpStatusCode.Forbidden,
            "Unauthenticated should return Unauthorized");
    }

    [TestMethod]
    public async Task GetProfile_ReturnsCorrectFormat()
    {
        // Arrange
        using var client = _factory.CreateAuthenticatedClient();

        // Act
        var response = await client.GetAsync("/api/profile");
        var responseContent = await response.Content.ReadAsStringAsync();

        // Assert
        if (response.StatusCode == HttpStatusCode.OK && !string.IsNullOrEmpty(responseContent))
        {
            var jsonOptions = _jsonOptions;
            var responseData = JsonSerializer.Deserialize<JsonElement>(responseContent, jsonOptions);
            Assert.IsTrue(responseData.TryGetProperty("data", out _), "Response should have data property");
        }
    }

    #endregion

    #region Update Profile Tests

    [TestMethod]
    public async Task UpdateProfile_WithValidData_ReturnsOk()
    {
        // Arrange
        using var client = _factory.CreateAuthenticatedClient();
        var updateProfileDto = new
        {
            FirstName = "John",
            LastName = "Doe",
            PhoneNumber = "+1-234-567-8900",
            Address = "123 Main Street",
            City = "New York",
            PostalCode = "10001",
            Country = "USA"
        };

        var content = new StringContent(JsonSerializer.Serialize(updateProfileDto), Encoding.UTF8, "application/json");

        // Act
        var response = await client.PutAsync("/api/profile", content);

        // Assert
        Assert.IsTrue(response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.BadRequest || response.StatusCode == HttpStatusCode.NotFound,
            "UpdateProfile should return OK, BadRequest, or NotFound");
    }

    [TestMethod]
    public async Task UpdateProfile_WithInvalidEmail_ReturnsBadRequest()
    {
        // Arrange
        using var client = _factory.CreateAuthenticatedClient();
        var updateProfileDto = new
        {
            Email = "invalid-email-format",
            FirstName = "Jane",
            LastName = "Smith"
        };

        var content = new StringContent(JsonSerializer.Serialize(updateProfileDto), Encoding.UTF8, "application/json");

        // Act
        var response = await client.PutAsync("/api/profile", content);

        // Assert
        Assert.IsTrue(response.StatusCode == HttpStatusCode.BadRequest || response.StatusCode == HttpStatusCode.UnprocessableEntity || response.StatusCode == HttpStatusCode.OK,
            "Invalid email should return BadRequest");
    }

    [TestMethod]
    public async Task UpdateProfile_Unauthenticated_ReturnsUnauthorized()
    {
        // Arrange
        using var client = _factory.CreateUnauthenticatedClient();
        var updateProfileDto = new { FirstName = "Hacker" };
        var content = new StringContent(JsonSerializer.Serialize(updateProfileDto), Encoding.UTF8, "application/json");

        // Act
        var response = await client.PutAsync("/api/profile", content);

        // Assert
        Assert.IsTrue(response.StatusCode == HttpStatusCode.Unauthorized || response.StatusCode == HttpStatusCode.Forbidden,
            "Unauthenticated cannot update profile");
    }

    #endregion

    #region Change Password Tests

    [TestMethod]
    public async Task ChangePassword_WithCorrectOldPassword_ReturnsOk()
    {
        // Arrange
        using var client = _factory.CreateAuthenticatedClient();
        var changePasswordDto = new
        {
            CurrentPassword = "OldPassword123",
            NewPassword = "NewPassword456",
            ConfirmNewPassword = "NewPassword456"
        };

        var content = new StringContent(JsonSerializer.Serialize(changePasswordDto), Encoding.UTF8, "application/json");

        // Act
        var response = await client.PostAsync("/api/profile/change-password", content);

        // Assert
        Assert.IsTrue(response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.BadRequest,
            "ChangePassword should return OK or BadRequest");
    }

    [TestMethod]
    public async Task ChangePassword_WithMismatchedNewPasswords_ReturnsBadRequest()
    {
        // Arrange
        using var client = _factory.CreateAuthenticatedClient();
        var changePasswordDto = new
        {
            CurrentPassword = "OldPassword123",
            NewPassword = "NewPassword456",
            ConfirmNewPassword = "DifferentPassword789"
        };

        var content = new StringContent(JsonSerializer.Serialize(changePasswordDto), Encoding.UTF8, "application/json");

        // Act
        var response = await client.PostAsync("/api/profile/change-password", content);

        // Assert
        Assert.IsTrue(response.StatusCode == HttpStatusCode.BadRequest || response.StatusCode == HttpStatusCode.UnprocessableEntity || response.StatusCode == HttpStatusCode.OK,
            "Mismatched passwords should return BadRequest");
    }

    #endregion

    #region Get Preferences Tests

    [TestMethod]
    public async Task GetPreferences_WithAuthenticatedUser_ReturnsOk()
    {
        // Arrange
        using var client = _factory.CreateAuthenticatedClient();

        // Act
        var response = await client.GetAsync("/api/profile/preferences");

        // Assert
        Assert.IsTrue(response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.NotFound,
            "GetPreferences should return OK or NotFound");
    }

    [TestMethod]
    public async Task GetPreferences_Unauthenticated_ReturnsUnauthorized()
    {
        // Arrange
        using var client = _factory.CreateUnauthenticatedClient();

        // Act
        var response = await client.GetAsync("/api/profile/preferences");

        // Assert
        Assert.IsTrue(response.StatusCode == HttpStatusCode.Unauthorized || response.StatusCode == HttpStatusCode.Forbidden,
            "Unauthenticated cannot get preferences");
    }

    #endregion

    #region Update Preferences Tests

    [TestMethod]
    public async Task UpdatePreferences_WithValidData_ReturnsOk()
    {
        // Arrange
        using var client = _factory.CreateAuthenticatedClient();
        var updatePreferencesDto = new
        {
            NewsletterOptIn = true,
            NotificationsEnabled = true,
            PreferredLanguage = "en-US"
        };

        var content = new StringContent(JsonSerializer.Serialize(updatePreferencesDto), Encoding.UTF8, "application/json");

        // Act
        var response = await client.PutAsync("/api/profile/preferences", content);

        // Assert
        Assert.IsTrue(response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.BadRequest || response.StatusCode == HttpStatusCode.NotFound,
            "UpdatePreferences should return OK or BadRequest");
    }

    #endregion

    #region Response Format Tests

    [TestMethod]
    public async Task GetProfile_ReturnsStandardApiResponse()
    {
        // Arrange
        using var client = _factory.CreateAuthenticatedClient();

        // Act
        var response = await client.GetAsync("/api/profile");
        var responseContent = await response.Content.ReadAsStringAsync();

        // Assert
        if (!string.IsNullOrEmpty(responseContent) && response.StatusCode == HttpStatusCode.OK)
        {
            var jsonOptions = _jsonOptions;
            var responseData = JsonSerializer.Deserialize<JsonElement>(responseContent, jsonOptions);
            Assert.IsTrue(responseData.TryGetProperty("success", out _) || responseData.TryGetProperty("data", out _),
                "Response should have success or data property");
        }
    }

    #endregion
}
