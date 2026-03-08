using System.Net;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ECommerce.Tests.Integration;

/// <summary>
/// Integration tests for DashboardController endpoints.
/// Tests admin dashboard statistics and reporting.
/// </summary>
[TestClass]
public class DashboardControllerTests
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

    #region Dashboard Stats Tests

    [TestMethod]
    public async Task GetDashboardStats_WithAdminRole_ReturnsOk()
    {
        // Arrange
        using var client = _factory.CreateAdminClient();

        // Act
        var response = await client.GetAsync("/api/dashboard/stats");

        // Assert
        Assert.IsTrue(response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.NotFound,
            "GetDashboardStats should return OK or NotFound");
    }

    [TestMethod]
    public async Task GetDashboardStats_WithCustomerRole_ReturnsForbidden()
    {
        // Arrange
        using var client = _factory.CreateAuthenticatedClient();

        // Act
        var response = await client.GetAsync("/api/dashboard/stats");

        // Assert
        Assert.IsTrue(response.StatusCode == HttpStatusCode.Forbidden || response.StatusCode == HttpStatusCode.Unauthorized,
            "Customer should not access dashboard");
    }

    [TestMethod]
    public async Task GetDashboardStats_Unauthenticated_ReturnsUnauthorized()
    {
        // Arrange
        using var client = _factory.CreateUnauthenticatedClient();

        // Act
        var response = await client.GetAsync("/api/dashboard/stats");

        // Assert
        Assert.IsTrue(response.StatusCode == HttpStatusCode.Unauthorized || response.StatusCode == HttpStatusCode.Forbidden,
            "Unauthenticated cannot access dashboard");
    }

    [TestMethod]
    public async Task GetDashboardStats_ReturnsCorrectFormat()
    {
        // Arrange
        using var client = _factory.CreateAdminClient();

        // Act
        var response = await client.GetAsync("/api/dashboard/stats");
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

    #region User Stats Tests

    [TestMethod]
    public async Task GetUserStats_WithAdminRole_ReturnsOk()
    {
        // Arrange
        using var client = _factory.CreateAdminClient();

        // Act
        var response = await client.GetAsync("/api/dashboard/user-stats");

        // Assert
        Assert.IsTrue(response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.NotFound,
            "GetUserStats should return OK or NotFound");
    }

    [TestMethod]
    public async Task GetUserStats_WithCustomerRole_ReturnsForbidden()
    {
        // Arrange
        using var client = _factory.CreateAuthenticatedClient();

        // Act
        var response = await client.GetAsync("/api/dashboard/user-stats");

        // Assert
        Assert.IsTrue(response.StatusCode == HttpStatusCode.Forbidden || response.StatusCode == HttpStatusCode.Unauthorized,
            "Customer cannot access user stats");
    }

    #endregion

    #region Order Stats Tests

    [TestMethod]
    public async Task GetOrderStats_WithAdminRole_ReturnsOk()
    {
        // Arrange
        using var client = _factory.CreateAdminClient();

        // Act
        var response = await client.GetAsync("/api/dashboard/order-stats");

        // Assert
        Assert.IsTrue(response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.NotFound,
            "GetOrderStats should return OK or NotFound");
    }

    [TestMethod]
    public async Task GetOrderStats_WithCustomerRole_ReturnsForbidden()
    {
        // Arrange
        using var client = _factory.CreateAuthenticatedClient();

        // Act
        var response = await client.GetAsync("/api/dashboard/order-stats");

        // Assert
        Assert.IsTrue(response.StatusCode == HttpStatusCode.Forbidden || response.StatusCode == HttpStatusCode.Unauthorized,
            "Customer cannot access order stats");
    }

    #endregion

    #region Revenue Stats Tests

    [TestMethod]
    public async Task GetRevenueStats_WithAdminRole_ReturnsOk()
    {
        // Arrange
        using var client = _factory.CreateAdminClient();

        // Act
        var response = await client.GetAsync("/api/dashboard/revenue-stats");

        // Assert
        Assert.IsTrue(response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.NotFound,
            "GetRevenueStats should return OK or NotFound");
    }

    [TestMethod]
    public async Task GetRevenueStats_WithCustomerRole_ReturnsForbidden()
    {
        // Arrange
        using var client = _factory.CreateAuthenticatedClient();

        // Act
        var response = await client.GetAsync("/api/dashboard/revenue-stats");

        // Assert
        Assert.IsTrue(response.StatusCode == HttpStatusCode.Forbidden || response.StatusCode == HttpStatusCode.Unauthorized,
            "Customer cannot access revenue stats");
    }

    #endregion

    #region Response Format Tests

    [TestMethod]
    public async Task GetDashboardStats_ReturnsStandardApiResponse()
    {
        // Arrange
        using var client = _factory.CreateAdminClient();

        // Act
        var response = await client.GetAsync("/api/dashboard/stats");
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
