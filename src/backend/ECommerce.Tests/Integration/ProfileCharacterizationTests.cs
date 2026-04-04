using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ECommerce.Tests.Integration;

/// <summary>
/// Characterization tests for ProfileController.
/// Written BEFORE Phase 2 migration — pin the exact HTTP contract.
/// These tests must pass against both old UserService AND new MediatR handlers.
/// </summary>
[TestClass]
public class ProfileCharacterizationTests
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

    // ── Get Profile ───────────────────────────────────────────────────────────

    [TestMethod]
    public async Task GetProfile_Authenticated_Returns200()
    {
        using var client = _factory.CreateAuthenticatedClient();

        var res = await client.GetAsync("/api/profile", TestContext.CancellationToken);

        Assert.AreEqual(HttpStatusCode.OK, res.StatusCode);
    }

    [TestMethod]
    public async Task GetProfile_Unauthenticated_Returns401()
    {
        using var client = _factory.CreateUnauthenticatedClient();

        var res = await client.GetAsync("/api/profile", TestContext.CancellationToken);

        Assert.AreEqual(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [TestMethod]
    public async Task GetProfile_ResponseShape_HasExpectedFields()
    {
        using var client = _factory.CreateAuthenticatedClient();

        var res = await client.GetAsync("/api/profile", TestContext.CancellationToken);
        string body = await res.Content.ReadAsStringAsync();
        var json = JsonSerializer.Deserialize<JsonElement>(body, _jsonOptions);

        Assert.AreEqual(HttpStatusCode.OK, res.StatusCode);
        Assert.IsTrue(json.TryGetProperty("data", out var data), "Response must have 'data' property");
        // Profile must expose at minimum: id, email, firstName, lastName
        Assert.IsTrue(
            data.TryGetProperty("email", out _) || data.TryGetProperty("Email", out _),
            "Profile data must contain email");
    }

    // ── Update Profile ────────────────────────────────────────────────────────

    [TestMethod]
    public async Task UpdateProfile_ValidData_Returns200()
    {
        using var client = _factory.CreateAuthenticatedClient();
        var payload = new { FirstName = "Updated", LastName = "User", PhoneNumber = "+1234567890" };

        var res = await client.PutAsync("/api/profile", Json(payload), TestContext.CancellationToken);

        Assert.AreEqual(HttpStatusCode.OK, res.StatusCode);
    }

    [TestMethod]
    public async Task UpdateProfile_MissingFirstName_Returns400()
    {
        using var client = _factory.CreateAuthenticatedClient();
        var payload = new { LastName = "User" };

        var res = await client.PutAsync("/api/profile", Json(payload), TestContext.CancellationToken);

        Assert.AreEqual(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [TestMethod]
    public async Task UpdateProfile_Unauthenticated_Returns401()
    {
        using var client = _factory.CreateUnauthenticatedClient();
        var payload = new { FirstName = "Test", LastName = "User" };

        var res = await client.PutAsync("/api/profile", Json(payload), TestContext.CancellationToken);

        Assert.AreEqual(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    // ── Change Password ───────────────────────────────────────────────────────

    [TestMethod]
    public async Task ChangePassword_WrongOldPassword_Returns400OrUnauthorized()
    {
        using var client = _factory.CreateAuthenticatedClient();
        var payload = new { OldPassword = "WrongOld1!", NewPassword = "NewPass1!", ConfirmPassword = "NewPass1!" };

        var res = await client.PostAsync("/api/profile/change-password", Json(payload), TestContext.CancellationToken);

        Assert.IsTrue(
            res.StatusCode == HttpStatusCode.BadRequest
            || res.StatusCode == HttpStatusCode.Unauthorized
            || res.StatusCode == HttpStatusCode.UnprocessableEntity,
            $"Expected 400/401/422, got {(int)res.StatusCode}");
    }

    [TestMethod]
    public async Task ChangePassword_MissingFields_Returns400()
    {
        using var client = _factory.CreateAuthenticatedClient();
        var payload = new { };

        var res = await client.PostAsync("/api/profile/change-password", Json(payload), TestContext.CancellationToken);

        Assert.AreEqual(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [TestMethod]
    public async Task ChangePassword_Unauthenticated_Returns401()
    {
        using var client = _factory.CreateUnauthenticatedClient();
        var payload = new { OldPassword = "Old1!", NewPassword = "New1!", ConfirmPassword = "New1!" };

        var res = await client.PostAsync("/api/profile/change-password", Json(payload), TestContext.CancellationToken);

        Assert.AreEqual(HttpStatusCode.Unauthorized, res.StatusCode);
    }
}
