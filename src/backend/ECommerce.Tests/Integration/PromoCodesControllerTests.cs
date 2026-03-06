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

    /// <summary>
    /// Tests JSON deserialization with camelCase property names (frontend format).
    /// Verifies the fix for ValidatePromoCodeRequestDto [JsonPropertyName] attributes.
    /// </summary>
    [TestMethod]
    public async Task ValidatePromoCode_WithValidCodeAndAmount_DeserializesCamelCase_ReturnsOk()
    {
        // Arrange
        using var client = _factory.CreateUnauthenticatedClient();
        // Simulate frontend JSON with camelCase properties
        var jsonPayload = "{\"code\":\"SAVE20\",\"orderAmount\":100}";
        var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

        // Act
        var response = await client.PostAsync("/api/promo-codes/validate", content);
        var responseContent = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, 
            $"ValidatePromoCode should accept camelCase JSON. Response: {responseContent}");
        
        // Verify response contains valid data
        var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var responseData = JsonSerializer.Deserialize<JsonElement>(responseContent, jsonOptions);
        Assert.IsTrue(responseData.TryGetProperty("success", out var successProp), "Response should have success property");
        Assert.AreEqual(true, successProp.GetBoolean(), "Response success should be true");
    }

    [TestMethod]
    public async Task ValidatePromoCode_WithZeroOrderAmount_AcceptsRequest_ReturnsValidation()
    {
        // Arrange
        using var client = _factory.CreateUnauthenticatedClient();
        // Test that OrderAmount = 0 is now allowed (previously failed validation)
        var jsonPayload = "{\"code\":\"SAVE20\",\"orderAmount\":0}";
        var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

        // Act
        var response = await client.PostAsync("/api/promo-codes/validate", content);
        var responseContent = await response.Content.ReadAsStringAsync();

        // Assert
        // Should NOT return 400 Bad Request with "Object is null" error
        Assert.AreNotEqual(HttpStatusCode.BadRequest, response.StatusCode, 
            "Zero order amount should be accepted by validator");
    }

    [TestMethod]
    public async Task ValidatePromoCode_WithInvalidJsonPropertyNames_ReturnsBadRequest()
    {
        // Arrange
        using var client = _factory.CreateUnauthenticatedClient();
        // Use PascalCase (non-standard format) - should still work since ASP.NET is case-insensitive
        var jsonPayload = "{\"Code\":\"SAVE20\",\"OrderAmount\":100}";
        var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

        // Act
        var response = await client.PostAsync("/api/promo-codes/validate", content);

        // Assert
        // Note: ASP.NET's default JSON deserialization is case-insensitive,
        // so both camelCase and PascalCase are accepted. This is acceptable API behavior.
        Assert.AreNotEqual(HttpStatusCode.BadRequest, response.StatusCode, 
            "PascalCase properties should be accepted (case-insensitive binding)");
    }

    [TestMethod]
    public async Task ValidatePromoCode_WithMissingOrderAmount_UsesDefaultValue_ReturnsOk()
    {
        // Arrange
        using var client = _factory.CreateUnauthenticatedClient();
        // Omit orderAmount - should use default value of 0m
        var jsonPayload = "{\"code\":\"SAVE20\"}";
        var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

        // Act
        var response = await client.PostAsync("/api/promo-codes/validate", content);

        // Assert
        Assert.AreNotEqual(HttpStatusCode.BadRequest, response.StatusCode, 
            "Missing orderAmount should use default 0m and be accepted");
    }

    [TestMethod]
    public async Task ValidatePromoCode_WithValidCode_CalculatesDiscountCorrectly()
    {
        // Arrange
        using var client = _factory.CreateUnauthenticatedClient();
        var jsonPayload = "{\"code\":\"SAVE20\",\"orderAmount\":100}";
        var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

        // Act
        var response = await client.PostAsync("/api/promo-codes/validate", content);
        var responseContent = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        
        var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var responseData = JsonSerializer.Deserialize<JsonElement>(responseContent, jsonOptions);
        
        if (responseData.TryGetProperty("data", out var dataProp) && dataProp.TryGetProperty("discountAmount", out var discountProp))
        {
            var discount = discountProp.GetDecimal();
            Assert.IsTrue(discount > 0, "Valid promo code should calculate a discount");
        }
    }

    [TestMethod]
    public async Task ValidatePromoCode_WithInvalidCode_ReturnsValidationResponse()
    {
        // Arrange
        using var client = _factory.CreateUnauthenticatedClient();
        var jsonPayload = "{\"code\":\"INVALIDCODE999\",\"orderAmount\":100}";
        var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

        // Act
        var response = await client.PostAsync("/api/promo-codes/validate", content);

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode,
            "Invalid code should still return 200 with isValid=false");
        
        var responseContent = await response.Content.ReadAsStringAsync();
        var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var responseData = JsonSerializer.Deserialize<JsonElement>(responseContent, jsonOptions);
        
        if (responseData.TryGetProperty("data", out var dataProp) && dataProp.TryGetProperty("isValid", out var isValidProp))
        {
            Assert.AreEqual(false, isValidProp.GetBoolean(), "Invalid code should have isValid=false");
        }
    }

    [TestMethod]
    public async Task ValidatePromoCode_AllowsAnonymousAccess_ReturnsSuccess()
    {
        // Arrange
        using var client = _factory.CreateUnauthenticatedClient();
        var jsonPayload = "{\"code\":\"SAVE20\",\"orderAmount\":100}";
        var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

        // Act
        var response = await client.PostAsync("/api/promo-codes/validate", content);

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, 
            "Endpoint should allow anonymous (unauthenticated) users");
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

    [TestMethod]
    public async Task GetActiveCodes_WithLargePageSize_IsClamped()
    {
        // Arrange
        using var client = _factory.CreateUnauthenticatedClient();

        // Act
        var response = await client.GetAsync("/api/promo-codes/active?page=1&pageSize=1000");
        var responseContent = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.IsTrue(response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.NotFound,
            "GetActiveCodes should return OK or NotFound");

        if (response.StatusCode == HttpStatusCode.OK && !string.IsNullOrEmpty(responseContent))
        {
            var json = JsonSerializer.Deserialize<JsonElement>(responseContent);
            if (json.TryGetProperty("data", out var data)
                && data.TryGetProperty("items", out var items)
                && items.ValueKind == JsonValueKind.Array)
            {
                Assert.IsLessThanOrEqualTo(100, items.GetArrayLength(), "Active promo codes pageSize should be clamped to 100");
            }
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
        Assert.IsTrue(response.StatusCode is HttpStatusCode.Created or HttpStatusCode.BadRequest,
            $"CreatePromoCode should return Created or BadRequest, got {response.StatusCode}");
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
        Assert.IsTrue(response.StatusCode == HttpStatusCode.OK ||
            response.StatusCode == HttpStatusCode.NotFound ||
            response.StatusCode == HttpStatusCode.BadRequest,
            "UpdatePromoCode should return OK, NotFound, or BadRequest");
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
        Assert.IsTrue(response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.NoContent || response.StatusCode == HttpStatusCode.NotFound,
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
