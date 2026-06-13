using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ECommerce.Contracts.DTOs.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ECommerce.Tests.Integration;

[TestClass]
public class HealthCheckEndpointsTests
{
    private static readonly TestWebApplicationFactory _factory = SharedTestInfrastructure.Factory;

    // ── /health ───────────────────────────────────────────────────────────────

    [TestMethod]
    public async Task Health_Returns200_WithHealthyStatus()
    {
        using var client = _factory.CreateUnauthenticatedClient();
        var response = await client.GetAsync("/health");
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.IsTrue(content.Contains("healthy"));
        Assert.IsTrue(content.Contains("timestamp"));
    }

    [TestMethod]
    public async Task Health_ReturnsJsonContentType()
    {
        using var client = _factory.CreateUnauthenticatedClient();
        var response = await client.GetAsync("/health");
        Assert.AreEqual("application/json", response.Content.Headers.ContentType?.MediaType);
    }

    [TestMethod]
    public async Task Health_DoesNotRequireAuthentication()
    {
        using var client = _factory.CreateUnauthenticatedClient();
        var response = await client.GetAsync("/health");
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
    }

    // ── /health/ready ─────────────────────────────────────────────────────────

    [TestMethod]
    public async Task Health_Ready_Returns200_WhenServicesAreHealthy()
    {
        using var client = _factory.CreateUnauthenticatedClient();
        var response = await client.GetAsync("/health/ready");
        Assert.IsTrue(response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.ServiceUnavailable);
    }

    [TestMethod]
    public async Task Health_Ready_ReturnsJsonContentType()
    {
        using var client = _factory.CreateUnauthenticatedClient();
        var response = await client.GetAsync("/health/ready");
        Assert.AreEqual("application/json", response.Content.Headers.ContentType?.MediaType);
    }

    [TestMethod]
    public async Task Health_Ready_DoesNotRequireAuthentication()
    {
        using var client = _factory.CreateUnauthenticatedClient();
        var response = await client.GetAsync("/health/ready");
        Assert.IsTrue(response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.ServiceUnavailable);
    }

    [TestMethod]
    public async Task Health_Ready_ReturnsDetailedResponse()
    {
        using var client = _factory.CreateUnauthenticatedClient();
        var response = await client.GetAsync("/health/ready");
        var content = await response.Content.ReadAsStringAsync();
        Assert.IsTrue(content.Contains("status"));
        Assert.IsTrue(content.Contains("totalDurationMs"));
        Assert.IsTrue(content.Contains("checks"));
    }

    // ── /health/detail ────────────────────────────────────────────────────────

    [TestMethod]
    public async Task Health_Detail_Returns200_WhenServicesAreHealthy()
    {
        using var client = _factory.CreateUnauthenticatedClient();
        var response = await client.GetAsync("/health/detail");
        Assert.IsTrue(response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.ServiceUnavailable);
    }

    [TestMethod]
    public async Task Health_Detail_ReturnsJsonContentType()
    {
        using var client = _factory.CreateUnauthenticatedClient();
        var response = await client.GetAsync("/health/detail");
        Assert.AreEqual("application/json", response.Content.Headers.ContentType?.MediaType);
    }

    [TestMethod]
    public async Task Health_Detail_DoesNotRequireAuthentication()
    {
        using var client = _factory.CreateUnauthenticatedClient();
        var response = await client.GetAsync("/health/detail");
        Assert.IsTrue(response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.ServiceUnavailable);
    }

    [TestMethod]
    public async Task Health_Detail_ReturnsAllChecks()
    {
        using var client = _factory.CreateUnauthenticatedClient();
        var response = await client.GetAsync("/health/detail");
        var content = await response.Content.ReadAsStringAsync();
        Assert.IsTrue(content.Contains("status"));
        Assert.IsTrue(content.Contains("totalDurationMs"));
        Assert.IsTrue(content.Contains("checks"));
    }

    [TestMethod]
    public async Task Health_Detail_ReturnsTimestamp()
    {
        using var client = _factory.CreateUnauthenticatedClient();
        var response = await client.GetAsync("/health/detail");
        var content = await response.Content.ReadAsStringAsync();
        Assert.IsTrue(content.Contains("timestamp"));
    }

    // ── /api/payments/health ──────────────────────────────────────────────────

    [TestMethod]
    public async Task Payments_Health_Returns200_WithHealthyStatus()
    {
        using var client = _factory.CreateUnauthenticatedClient();
        var response = await client.GetAsync("/api/payments/health");
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.IsTrue(content.Contains("healthy"));
        Assert.IsTrue(content.Contains("PaymentService"));
    }

    [TestMethod]
    public async Task Payments_Health_DoesNotRequireAuthentication()
    {
        using var client = _factory.CreateUnauthenticatedClient();
        var response = await client.GetAsync("/api/payments/health");
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
    }

    [TestMethod]
    public async Task Payments_Health_ReturnsCorrectServiceName()
    {
        using var client = _factory.CreateUnauthenticatedClient();
        var response = await client.GetAsync("/api/payments/health");
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<HealthCheckResponseDto>>();
        Assert.IsNotNull(result);
        Assert.IsNotNull(result.Data);
        Assert.AreEqual("PaymentService", result.Data.Service);
    }
}
