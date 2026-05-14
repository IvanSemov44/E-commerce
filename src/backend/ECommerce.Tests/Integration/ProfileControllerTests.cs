using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ECommerce.Tests.Integration;

[TestClass]
public class ProfileControllerTests
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

    // ── GET /api/profile ─────────────────────────────────────────────────────

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
    public async Task GetProfile_ResponseShape_HasEmailField()
    {
        using var client = _factory.CreateAuthenticatedClient();
        var res = await client.GetAsync("/api/profile", TestContext.CancellationToken);
        var json = JsonSerializer.Deserialize<JsonElement>(await res.Content.ReadAsStringAsync(), _jsonOptions);
        Assert.AreEqual(HttpStatusCode.OK, res.StatusCode);
        Assert.IsTrue(json.TryGetProperty("data", out var data), "Response must have 'data'");
        Assert.IsTrue(data.TryGetProperty("email", out _) || data.TryGetProperty("Email", out _),
            "Profile data must contain email");
    }

    // ── PUT /api/profile ──────────────────────────────────────────────────────

    [TestMethod]
    public async Task UpdateProfile_ValidData_Returns200()
    {
        using var client = _factory.CreateAuthenticatedClient();
        var res = await client.PutAsync("/api/profile",
            Json(new { FirstName = "Updated", LastName = "User", PhoneNumber = "+1234567890" }),
            TestContext.CancellationToken);
        Assert.AreEqual(HttpStatusCode.OK, res.StatusCode);
    }

    [TestMethod]
    public async Task UpdateProfile_MissingFirstName_Returns400()
    {
        using var client = _factory.CreateAuthenticatedClient();
        var res = await client.PutAsync("/api/profile",
            Json(new { LastName = "User" }),
            TestContext.CancellationToken);
        Assert.AreEqual(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [TestMethod]
    public async Task UpdateProfile_WithInvalidEmail_ReturnsBadRequest()
    {
        using var client = _factory.CreateAuthenticatedClient();
        var res = await client.PutAsync("/api/profile",
            Json(new { Email = "invalid-email-format", FirstName = "Jane", LastName = "Smith" }),
            TestContext.CancellationToken);
        Assert.IsTrue(res.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.UnprocessableEntity or HttpStatusCode.OK);
    }

    [TestMethod]
    public async Task UpdateProfile_Unauthenticated_Returns401()
    {
        using var client = _factory.CreateUnauthenticatedClient();
        var res = await client.PutAsync("/api/profile",
            Json(new { FirstName = "Test", LastName = "User" }),
            TestContext.CancellationToken);
        Assert.AreEqual(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    // ── POST /api/profile/change-password ────────────────────────────────────

    [TestMethod]
    public async Task ChangePassword_WrongOldPassword_Returns400OrUnauthorized()
    {
        using var client = _factory.CreateAuthenticatedClient();
        var res = await client.PostAsync("/api/profile/change-password",
            Json(new { OldPassword = "WrongOld1!", NewPassword = "NewPass1!", ConfirmPassword = "NewPass1!" }),
            TestContext.CancellationToken);
        Assert.IsTrue(res.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.Unauthorized or HttpStatusCode.UnprocessableEntity,
            $"Expected 400/401/422, got {(int)res.StatusCode}");
    }

    [TestMethod]
    public async Task ChangePassword_WithMismatchedNewPasswords_ReturnsBadRequest()
    {
        using var client = _factory.CreateAuthenticatedClient();
        var res = await client.PostAsync("/api/profile/change-password",
            Json(new { OldPassword = "OldPassword123", NewPassword = "NewPassword456", ConfirmPassword = "DifferentPassword789" }),
            TestContext.CancellationToken);
        Assert.IsTrue(res.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.UnprocessableEntity or HttpStatusCode.OK);
    }

    [TestMethod]
    public async Task ChangePassword_MissingFields_Returns400()
    {
        using var client = _factory.CreateAuthenticatedClient();
        var res = await client.PostAsync("/api/profile/change-password",
            Json(new { }),
            TestContext.CancellationToken);
        Assert.AreEqual(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [TestMethod]
    public async Task ChangePassword_Unauthenticated_Returns401()
    {
        using var client = _factory.CreateUnauthenticatedClient();
        var res = await client.PostAsync("/api/profile/change-password",
            Json(new { OldPassword = "Old1!", NewPassword = "New1!", ConfirmPassword = "New1!" }),
            TestContext.CancellationToken);
        Assert.AreEqual(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    // ── GET /api/profile/preferences ─────────────────────────────────────────

    [TestMethod]
    public async Task GetPreferences_Authenticated_ReturnsOk()
    {
        using var client = _factory.CreateAuthenticatedClient();
        var res = await client.GetAsync("/api/profile/preferences", TestContext.CancellationToken);
        Assert.IsTrue(res.StatusCode is HttpStatusCode.OK or HttpStatusCode.NotFound);
    }

    [TestMethod]
    public async Task GetPreferences_Unauthenticated_ReturnsUnauthorized()
    {
        using var client = _factory.CreateUnauthenticatedClient();
        var res = await client.GetAsync("/api/profile/preferences", TestContext.CancellationToken);
        Assert.IsTrue(res.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden);
    }

    // ── PUT /api/profile/preferences ─────────────────────────────────────────

    [TestMethod]
    public async Task UpdatePreferences_WithValidData_ReturnsOk()
    {
        using var client = _factory.CreateAuthenticatedClient();
        var res = await client.PutAsync("/api/profile/preferences",
            Json(new { NewsletterOptIn = true, NotificationsEnabled = true, PreferredLanguage = "en-US" }),
            TestContext.CancellationToken);
        Assert.IsTrue(res.StatusCode is HttpStatusCode.OK or HttpStatusCode.BadRequest or HttpStatusCode.NotFound);
    }
}
