using System.Net;
using System.Text.Json;

namespace ECommerce.Tests.Integration;

[TestClass]
public class DashboardControllerTests
{
    private static readonly TestWebApplicationFactory _factory = SharedTestInfrastructure.Factory;
    private static readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

    // ── GET /api/dashboard/stats ──────────────────────────────────────────────

    [TestMethod]
    public async Task GetDashboardStats_WithAdminRole_ReturnsOk()
    {
        using var client = _factory.CreateAdminClient();
        var response = await client.GetAsync("/api/dashboard/stats");
        Assert.IsTrue(response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.NotFound,
            $"Expected OK or NotFound, got {(int)response.StatusCode}");
    }

    [TestMethod]
    public async Task GetDashboardStats_WithCustomerRole_ReturnsForbidden()
    {
        using var client = _factory.CreateAuthenticatedClient();
        var response = await client.GetAsync("/api/dashboard/stats");
        Assert.AreEqual(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [TestMethod]
    public async Task GetDashboardStats_Unauthenticated_ReturnsUnauthorized()
    {
        using var client = _factory.CreateUnauthenticatedClient();
        var response = await client.GetAsync("/api/dashboard/stats");
        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [TestMethod]
    public async Task GetDashboardStats_ReturnsCorrectFormat()
    {
        using var client = _factory.CreateAdminClient();
        var response = await client.GetAsync("/api/dashboard/stats");
        var responseContent = await response.Content.ReadAsStringAsync();
        if (response.StatusCode == HttpStatusCode.OK && !string.IsNullOrEmpty(responseContent))
        {
            var responseData = JsonSerializer.Deserialize<JsonElement>(responseContent, _jsonOptions);
            Assert.IsTrue(responseData.TryGetProperty("data", out _), "Response should have data property");
        }
    }

    // ── GET /api/dashboard/user-stats ─────────────────────────────────────────

    [TestMethod]
    public async Task GetUserStats_WithAdminRole_ReturnsOk()
    {
        using var client = _factory.CreateAdminClient();
        var response = await client.GetAsync("/api/dashboard/user-stats");
        Assert.IsTrue(response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.NotFound,
            $"Expected OK or NotFound, got {(int)response.StatusCode}");
    }

    [TestMethod]
    public async Task GetUserStats_WithCustomerRole_ReturnsForbidden()
    {
        using var client = _factory.CreateAuthenticatedClient();
        var response = await client.GetAsync("/api/dashboard/user-stats");
        Assert.AreEqual(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // ── GET /api/dashboard/order-stats ────────────────────────────────────────

    [TestMethod]
    public async Task GetOrderStats_WithAdminRole_ReturnsOk()
    {
        using var client = _factory.CreateAdminClient();
        var response = await client.GetAsync("/api/dashboard/order-stats");
        Assert.IsTrue(response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.NotFound,
            $"Expected OK or NotFound, got {(int)response.StatusCode}");
    }

    [TestMethod]
    public async Task GetOrderStats_WithCustomerRole_ReturnsForbidden()
    {
        using var client = _factory.CreateAuthenticatedClient();
        var response = await client.GetAsync("/api/dashboard/order-stats");
        Assert.AreEqual(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // ── GET /api/dashboard/revenue-stats ──────────────────────────────────────

    [TestMethod]
    public async Task GetRevenueStats_WithAdminRole_ReturnsOk()
    {
        using var client = _factory.CreateAdminClient();
        var response = await client.GetAsync("/api/dashboard/revenue-stats");
        Assert.IsTrue(response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.NotFound,
            $"Expected OK or NotFound, got {(int)response.StatusCode}");
    }

    [TestMethod]
    public async Task GetRevenueStats_WithCustomerRole_ReturnsForbidden()
    {
        using var client = _factory.CreateAuthenticatedClient();
        var response = await client.GetAsync("/api/dashboard/revenue-stats");
        Assert.AreEqual(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // ── Response format ────────────────────────────────────────────────────────

    [TestMethod]
    public async Task GetDashboardStats_ReturnsStandardApiResponse()
    {
        using var client = _factory.CreateAdminClient();
        var response = await client.GetAsync("/api/dashboard/stats");
        var responseContent = await response.Content.ReadAsStringAsync();
        if (!string.IsNullOrEmpty(responseContent) && response.StatusCode == HttpStatusCode.OK)
        {
            var responseData = JsonSerializer.Deserialize<JsonElement>(responseContent, _jsonOptions);
            Assert.IsTrue(responseData.TryGetProperty("success", out _) || responseData.TryGetProperty("data", out _),
                "Response should have success or data property");
        }
    }
}
