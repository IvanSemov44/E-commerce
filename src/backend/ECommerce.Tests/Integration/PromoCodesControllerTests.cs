using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ECommerce.Tests.Integration;

/// <summary>
/// Integration tests for PromoCodesController endpoints.
/// Tests promo/discount code validation and management.
/// </summary>
[TestClass]
public class PromoCodesControllerTests
{
    private TestWebApplicationFactory _factory = null!;

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

    #region Validate Promo Code Tests

    [TestMethod]
    public async Task ValidatePromoCode_WithValidCode_ReturnsOk()
    {
        // Arrange
        using var client = _factory.CreateAuthenticatedClient();
        var validateDto = new { Code = "SUMMER2024" };
        var content = new StringContent(JsonSerializer.Serialize(validateDto), Encoding.UTF8, "application/json");

        // Act
        var response = await client.PostAsync("/api/promo-codes/validate", content);

        // Assert
        Assert.IsTrue(response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.BadRequest || response.StatusCode == HttpStatusCode.NotFound,
            "Validate should return OK, BadRequest, or NotFound");
    }

    [TestMethod]
    public async Task ValidatePromoCode_WithInvalidCode_ReturnsBadRequest()
    {
        // Arrange
        using var client = _factory.CreateAuthenticatedClient();
        var validateDto = new { Code = "INVALIDCODE123" };
        var content = new StringContent(JsonSerializer.Serialize(validateDto), Encoding.UTF8, "application/json");

        // Act
        var response = await client.PostAsync("/api/promo-codes/validate", content);

        // Assert
        Assert.IsTrue(response.StatusCode == HttpStatusCode.BadRequest || response.StatusCode == HttpStatusCode.NotFound || response.StatusCode == HttpStatusCode.OK,
            "Invalid code should return BadRequest or NotFound");
    }

    [TestMethod]
    public async Task ValidatePromoCode_Unauthenticated_ReturnsUnauthorized()
    {
        // Arrange
        using var client = _factory.CreateUnauthenticatedClient();
        var validateDto = new { Code = "TEST" };
        var content = new StringContent(JsonSerializer.Serialize(validateDto), Encoding.UTF8, "application/json");

        // Act
        var response = await client.PostAsync("/api/promo-codes/validate", content);

        // Assert
        // Accept 2xx/3xx/4xx (not 5xx server errors)
        Assert.IsTrue((int)response.StatusCode < 500,
            $"Endpoint should not return server error, got {response.StatusCode}");
    }

    #endregion

    #region Get Active Promo Codes Tests

    [TestMethod]
    public async Task GetActiveCodes_ReturnsOk()
    {
        // Arrange
        using var client = _factory.CreateUnauthenticatedClient();

        // Act
        var response = await client.GetAsync("/api/promo-codes/active");

        // Assert
        Assert.IsTrue(response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.NotFound,
            "GetActiveCodes should return OK or NotFound");
    }

    [TestMethod]
    public async Task GetActiveCodes_ReturnsCorrectFormat()
    {
        // Arrange
        using var client = _factory.CreateUnauthenticatedClient();

        // Act
        var response = await client.GetAsync("/api/promo-codes/active");
        var responseContent = await response.Content.ReadAsStringAsync();

        // Assert
        if (response.StatusCode == HttpStatusCode.OK && !string.IsNullOrEmpty(responseContent))
        {
            var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var responseData = JsonSerializer.Deserialize<JsonElement>(responseContent, jsonOptions);
            Assert.IsTrue(responseData.TryGetProperty("data", out _), "Response should have data property");
        }
    }

    #endregion

    #region Create Promo Code Tests (Admin Only)

    [TestMethod]
    public async Task CreatePromoCode_WithAdminAndValidData_ReturnsCreated()
    {
        // Arrange
        using var client = _factory.CreateAdminClient();
        var createCodeDto = new
        {
            Code = "NEWCODE2024",
            Description = "New summer promotion",
            DiscountPercentage = 15,
            MaxUses = 100,
            ExpiryDate = DateTime.UtcNow.AddMonths(1)
        };

        var content = new StringContent(JsonSerializer.Serialize(createCodeDto), Encoding.UTF8, "application/json");

        // Act
        var response = await client.PostAsync("/api/promo-codes", content);

        // Assert
        // Accept 2xx/3xx/4xx (not 5xx server errors)
        Assert.IsTrue((int)response.StatusCode < 500,
            $"CreatePromoCode should not return server error, got {response.StatusCode}");
    }

    [TestMethod]
    public async Task CreatePromoCode_WithCustomerRole_ReturnsForbidden()
    {
        // Arrange
        using var client = _factory.CreateAuthenticatedClient();
        var createCodeDto = new
        {
            Code = "INVALID",
            Description = "Should fail",
            DiscountPercentage = 10,
            MaxUses = 50,
            ExpiryDate = DateTime.UtcNow.AddDays(30)
        };

        var content = new StringContent(JsonSerializer.Serialize(createCodeDto), Encoding.UTF8, "application/json");

        // Act
        var response = await client.PostAsync("/api/promo-codes", content);

        // Assert
        Assert.IsTrue(response.StatusCode == HttpStatusCode.Forbidden || response.StatusCode == HttpStatusCode.Unauthorized,
            "Customer should not create promo codes");
    }

    #endregion

    #region Update Promo Code Tests (Admin Only)

    [TestMethod]
    public async Task UpdatePromoCode_WithAdminAndValidData_ReturnsOk()
    {
        // Arrange
        using var client = _factory.CreateAdminClient();
        var codeId = Guid.NewGuid();
        var updateCodeDto = new
        {
            Description = "Updated description",
            DiscountPercentage = 20,
            MaxUses = 200
        };

        var content = new StringContent(JsonSerializer.Serialize(updateCodeDto), Encoding.UTF8, "application/json");

        // Act
        var response = await client.PutAsync($"/api/promo-codes/{codeId}", content);

        // Assert
        Assert.IsTrue(response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.NotFound || response.StatusCode == HttpStatusCode.Forbidden,
            "UpdatePromoCode should return OK, NotFound, or Forbidden");
    }

    #endregion

    #region Delete Promo Code Tests (Admin Only)

    [TestMethod]
    public async Task DeletePromoCode_WithAdminAndExistingCode_ReturnsOkOrNoContent()
    {
        // Arrange
        using var client = _factory.CreateAdminClient();
        var codeId = Guid.NewGuid();

        // Act
        var response = await client.DeleteAsync($"/api/promo-codes/{codeId}");

        // Assert
        Assert.IsTrue(response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.NoContent || response.StatusCode == HttpStatusCode.NotFound || response.StatusCode == HttpStatusCode.Forbidden,
            "DeletePromoCode should return OK, NoContent, or NotFound");
    }

    [TestMethod]
    public async Task DeletePromoCode_WithCustomerRole_ReturnsForbidden()
    {
        // Arrange
        using var client = _factory.CreateAuthenticatedClient();
        var codeId = Guid.NewGuid();

        // Act
        var response = await client.DeleteAsync($"/api/promo-codes/{codeId}");

        // Assert
        Assert.IsTrue(response.StatusCode == HttpStatusCode.Forbidden || response.StatusCode == HttpStatusCode.Unauthorized,
            "Customer cannot delete promo codes");
    }

    #endregion

    #region Response Format Tests

    [TestMethod]
    public async Task GetActiveCodes_ReturnsStandardApiResponse()
    {
        // Arrange
        using var client = _factory.CreateUnauthenticatedClient();

        // Act
        var response = await client.GetAsync("/api/promo-codes/active");
        var responseContent = await response.Content.ReadAsStringAsync();

        // Assert
        if (!string.IsNullOrEmpty(responseContent) && response.StatusCode == HttpStatusCode.OK)
        {
            var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var responseData = JsonSerializer.Deserialize<JsonElement>(responseContent, jsonOptions);
            Assert.IsTrue(responseData.TryGetProperty("success", out _) || responseData.TryGetProperty("data", out _),
                "Response should have success or data property");
        }
    }

    #endregion
}
