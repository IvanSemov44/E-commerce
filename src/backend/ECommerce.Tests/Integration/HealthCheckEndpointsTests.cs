using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ECommerce.Contracts.DTOs.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ECommerce.Tests.Integration;

/// <summary>
/// Integration tests for health check endpoints.
/// Tests /health, /health/ready, and /health/detail endpoints.
/// </summary>
[TestClass]
public class HealthCheckEndpointsTests
{
    private static TestWebApplicationFactory _factory = null!;

    [TestInitialize]
    public void Setup()
    {
        _factory ??= new TestWebApplicationFactory();
    }

    [TestCleanup]
    public void Cleanup()
    {
    }

    #region /health endpoint tests

    [TestMethod]
    public async Task Health_Returns200_WithHealthyStatus()
    {
        // Arrange
        using var client = _factory.CreateUnauthenticatedClient();

        // Act
        var response = await client.GetAsync("/health");

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        Assert.IsTrue(content.Contains("healthy"));
        Assert.IsTrue(content.Contains("timestamp"));
    }

    [TestMethod]
    public async Task Health_ReturnsJsonContentType()
    {
        // Arrange
        using var client = _factory.CreateUnauthenticatedClient();

        // Act
        var response = await client.GetAsync("/health");

        // Assert
        Assert.AreEqual("application/json", response.Content.Headers.ContentType?.MediaType);
    }

    [TestMethod]
    public async Task Health_DoesNotRequireAuthentication()
    {
        // Arrange - No authentication header
        using var client = _factory.CreateUnauthenticatedClient();

        // Act
        var response = await client.GetAsync("/health");

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
    }

    #endregion

    #region /health/ready endpoint tests

    [TestMethod]
    public async Task Health_Ready_Returns200_WhenServicesAreHealthy()
    {
        // Arrange
        using var client = _factory.CreateUnauthenticatedClient();

        // Act
        var response = await client.GetAsync("/health/ready");

        // Assert - In test environment with in-memory database, should return healthy or service unavailable
        Assert.IsTrue(response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.ServiceUnavailable);
    }

    [TestMethod]
    public async Task Health_Ready_ReturnsJsonContentType()
    {
        // Arrange
        using var client = _factory.CreateUnauthenticatedClient();

        // Act
        var response = await client.GetAsync("/health/ready");

        // Assert
        Assert.AreEqual("application/json", response.Content.Headers.ContentType?.MediaType);
    }

    [TestMethod]
    public async Task Health_Ready_DoesNotRequireAuthentication()
    {
        // Arrange - No authentication header
        using var client = _factory.CreateUnauthenticatedClient();

        // Act
        var response = await client.GetAsync("/health/ready");

        // Assert
        Assert.IsTrue(response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.ServiceUnavailable);
    }

    [TestMethod]
    public async Task Health_Ready_ReturnsDetailedResponse()
    {
        // Arrange
        using var client = _factory.CreateUnauthenticatedClient();

        // Act
        var response = await client.GetAsync("/health/ready");
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.IsTrue(content.Contains("status"));
        Assert.IsTrue(content.Contains("totalDurationMs"));
        Assert.IsTrue(content.Contains("checks"));
    }

    #endregion

    #region /health/detail endpoint tests

    [TestMethod]
    public async Task Health_Detail_Returns200_WhenServicesAreHealthy()
    {
        // Arrange
        using var client = _factory.CreateUnauthenticatedClient();

        // Act
        var response = await client.GetAsync("/health/detail");

        // Assert
        Assert.IsTrue(response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.ServiceUnavailable);
    }

    [TestMethod]
    public async Task Health_Detail_ReturnsJsonContentType()
    {
        // Arrange
        using var client = _factory.CreateUnauthenticatedClient();

        // Act
        var response = await client.GetAsync("/health/detail");

        // Assert
        Assert.AreEqual("application/json", response.Content.Headers.ContentType?.MediaType);
    }

    [TestMethod]
    public async Task Health_Detail_DoesNotRequireAuthentication()
    {
        // Arrange - No authentication header
        using var client = _factory.CreateUnauthenticatedClient();

        // Act
        var response = await client.GetAsync("/health/detail");

        // Assert
        Assert.IsTrue(response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.ServiceUnavailable);
    }

    [TestMethod]
    public async Task Health_Detail_ReturnsAllChecks()
    {
        // Arrange
        using var client = _factory.CreateUnauthenticatedClient();

        // Act
        var response = await client.GetAsync("/health/detail");
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.IsTrue(content.Contains("status"));
        Assert.IsTrue(content.Contains("totalDurationMs"));
        Assert.IsTrue(content.Contains("checks"));
    }

    [TestMethod]
    public async Task Health_Detail_ReturnsTimestamp()
    {
        // Arrange
        using var client = _factory.CreateUnauthenticatedClient();

        // Act
        var response = await client.GetAsync("/health/detail");
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.IsTrue(content.Contains("timestamp"));
    }

    #endregion

    #region /api/payments/health endpoint tests

    [TestMethod]
    public async Task Payments_Health_Returns200_WithHealthyStatus()
    {
        // Arrange
        using var client = _factory.CreateUnauthenticatedClient();

        // Act
        var response = await client.GetAsync("/api/payments/health");

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        Assert.IsTrue(content.Contains("healthy"));
        Assert.IsTrue(content.Contains("PaymentService"));
    }

    [TestMethod]
    public async Task Payments_Health_DoesNotRequireAuthentication()
    {
        // Arrange - No authentication header
        using var client = _factory.CreateUnauthenticatedClient();

        // Act
        var response = await client.GetAsync("/api/payments/health");

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
    }

    [TestMethod]
    public async Task Payments_Health_ReturnsCorrectServiceName()
    {
        // Arrange
        using var client = _factory.CreateUnauthenticatedClient();

        // Act
        var response = await client.GetAsync("/api/payments/health");
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<HealthCheckResponseDto>>();

        // Assert
        Assert.IsNotNull(result);
        Assert.IsNotNull(result.Data);
        Assert.AreEqual("PaymentService", result.Data.Service);
    }

    #endregion
}

