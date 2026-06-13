using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace ECommerce.Tests.Integration;

[TestClass]
public class ProfileControllerTests
{
    private static readonly TestWebApplicationFactory _factory = SharedTestInfrastructure.Factory;
    private static readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

    public TestContext TestContext { get; set; } = null!;

    private static StringContent Json(object dto) =>
        new(JsonSerializer.Serialize(dto), Encoding.UTF8, "application/json");

    private static async Task<JsonElement> ReadData(HttpResponseMessage response)
    {
        var body = JsonSerializer.Deserialize<JsonElement>(
            await response.Content.ReadAsStringAsync(), _jsonOptions);
        return body.GetProperty("data");
    }

    // ── GET /api/profile ─────────────────────────────────────────────────────

    [TestMethod]
    public async Task GetProfile_Authenticated_ReturnsSeededUserProfile()
    {
        using var client = _factory.CreateAuthenticatedClient();

        var res = await client.GetAsync("/api/profile", TestContext.CancellationToken);

        res.StatusCode.ShouldBe(HttpStatusCode.OK);
        var data = await ReadData(res);
        data.GetProperty("email").GetString().ShouldBe("integration@test.com");
        data.GetProperty("firstName").GetString().ShouldBe("Integration");
        data.GetProperty("lastName").GetString().ShouldBe("User");
    }

    [TestMethod]
    public async Task GetProfile_Unauthenticated_Returns401()
    {
        using var client = _factory.CreateUnauthenticatedClient();

        var res = await client.GetAsync("/api/profile", TestContext.CancellationToken);

        res.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    // ── PUT /api/profile ──────────────────────────────────────────────────────

    [TestMethod]
    public async Task UpdateProfile_ValidData_ReturnsUpdatedProfile()
    {
        using var client = _factory.CreateAuthenticatedClient();

        var res = await client.PutAsync("/api/profile",
            Json(new { FirstName = "Updated", LastName = "Name" }),
            TestContext.CancellationToken);

        res.StatusCode.ShouldBe(HttpStatusCode.OK);
        var data = await ReadData(res);
        data.GetProperty("firstName").GetString().ShouldBe("Updated");
        data.GetProperty("lastName").GetString().ShouldBe("Name");
    }

    [TestMethod]
    public async Task UpdateProfile_ThenGetProfile_ReflectsChange()
    {
        using var client = _factory.CreateAuthenticatedClient();

        await client.PutAsync("/api/profile",
            Json(new { FirstName = "Changed", LastName = "Person" }),
            TestContext.CancellationToken);

        var res = await client.GetAsync("/api/profile", TestContext.CancellationToken);

        res.StatusCode.ShouldBe(HttpStatusCode.OK);
        var data = await ReadData(res);
        data.GetProperty("firstName").GetString().ShouldBe("Changed");
        data.GetProperty("lastName").GetString().ShouldBe("Person");
    }

    [TestMethod]
    public async Task UpdateProfile_MissingFirstName_Returns400()
    {
        using var client = _factory.CreateAuthenticatedClient();

        var res = await client.PutAsync("/api/profile",
            Json(new { LastName = "User" }),
            TestContext.CancellationToken);

        res.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [TestMethod]
    public async Task UpdateProfile_Unauthenticated_Returns401()
    {
        using var client = _factory.CreateUnauthenticatedClient();

        var res = await client.PutAsync("/api/profile",
            Json(new { FirstName = "Test", LastName = "User" }),
            TestContext.CancellationToken);

        res.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    // ── POST /api/profile/change-password ────────────────────────────────────

    [TestMethod]
    public async Task ChangePassword_ValidRequest_Returns200()
    {
        using var client = _factory.CreateAuthenticatedClient();

        var res = await client.PostAsync("/api/profile/change-password",
            Json(new { OldPassword = "TestPassword123!", NewPassword = "NewPassword1!", ConfirmPassword = "NewPassword1!" }),
            TestContext.CancellationToken);

        res.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [TestMethod]
    public async Task ChangePassword_WrongOldPassword_Returns401()
    {
        using var client = _factory.CreateAuthenticatedClient();

        var res = await client.PostAsync("/api/profile/change-password",
            Json(new { OldPassword = "WrongPassword1!", NewPassword = "NewPass1!", ConfirmPassword = "NewPass1!" }),
            TestContext.CancellationToken);

        res.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [TestMethod]
    public async Task ChangePassword_MismatchedConfirmPassword_Returns422()
    {
        using var client = _factory.CreateAuthenticatedClient();

        var res = await client.PostAsync("/api/profile/change-password",
            Json(new { OldPassword = "TestPassword123!", NewPassword = "NewPassword1!", ConfirmPassword = "DifferentPassword1!" }),
            TestContext.CancellationToken);

        res.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [TestMethod]
    public async Task ChangePassword_MissingFields_Returns400()
    {
        using var client = _factory.CreateAuthenticatedClient();

        var res = await client.PostAsync("/api/profile/change-password",
            Json(new { }),
            TestContext.CancellationToken);

        res.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [TestMethod]
    public async Task ChangePassword_Unauthenticated_Returns401()
    {
        using var client = _factory.CreateUnauthenticatedClient();

        var res = await client.PostAsync("/api/profile/change-password",
            Json(new { OldPassword = "Old1!", NewPassword = "New1!", ConfirmPassword = "New1!" }),
            TestContext.CancellationToken);

        res.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    // ── GET /api/profile/preferences ─────────────────────────────────────────

    [TestMethod]
    public async Task GetPreferences_Authenticated_ReturnsDefaultPreferences()
    {
        using var client = _factory.CreateAuthenticatedClient();

        var res = await client.GetAsync("/api/profile/preferences", TestContext.CancellationToken);

        res.StatusCode.ShouldBe(HttpStatusCode.OK);
        var data = await ReadData(res);
        data.GetProperty("language").GetString().ShouldBe("en");
        data.GetProperty("currency").GetString().ShouldBe("USD");
    }

    [TestMethod]
    public async Task GetPreferences_Unauthenticated_Returns401()
    {
        using var client = _factory.CreateUnauthenticatedClient();

        var res = await client.GetAsync("/api/profile/preferences", TestContext.CancellationToken);

        res.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    // ── PUT /api/profile/preferences ─────────────────────────────────────────

    [TestMethod]
    public async Task UpdatePreferences_ValidData_Returns200()
    {
        using var client = _factory.CreateAuthenticatedClient();

        var res = await client.PutAsync("/api/profile/preferences",
            Json(new { EmailNotifications = true, SmsNotifications = false, PushNotifications = true, Language = "en-US", Currency = "USD", NewsletterSubscribed = true }),
            TestContext.CancellationToken);

        res.StatusCode.ShouldBe(HttpStatusCode.OK);
    }
}
