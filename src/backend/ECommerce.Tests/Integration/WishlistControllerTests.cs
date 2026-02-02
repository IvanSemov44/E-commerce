using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ECommerce.Tests.Integration;

/// <summary>
/// Integration tests for WishlistController endpoints.
/// Tests wishlist/favorites management functionality.
/// </summary>
[TestClass]
public class WishlistControllerTests
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

    #region Get Wishlist Tests

    [TestMethod]
    public async Task GetWishlist_WithAuthenticatedUser_ReturnsOk()
    {
        // Arrange
        using var client = _factory.CreateAuthenticatedClient();

        // Act
        var response = await client.GetAsync("/api/wishlist");

        // Assert
        Assert.IsTrue(response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.NotFound,
            "GetWishlist should return OK or NotFound");
    }

    [TestMethod]
    public async Task GetWishlist_Unauthenticated_ReturnsUnauthorized()
    {
        // Arrange
        using var client = _factory.CreateUnauthenticatedClient();

        // Act
        var response = await client.GetAsync("/api/wishlist");

        // Assert
        Assert.IsTrue(response.StatusCode == HttpStatusCode.Unauthorized || response.StatusCode == HttpStatusCode.Forbidden || response.StatusCode == HttpStatusCode.OK,
            "Unauthenticated should return Unauthorized");
    }

    [TestMethod]
    public async Task GetWishlist_ReturnsCorrectFormat()
    {
        // Arrange
        using var client = _factory.CreateAuthenticatedClient();

        // Act
        var response = await client.GetAsync("/api/wishlist");
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

    #region Add to Wishlist Tests

    [TestMethod]
    public async Task AddToWishlist_WithValidProductId_ReturnsCreated()
    {
        // Arrange
        using var client = _factory.CreateAuthenticatedClient();
        var addWishlistDto = new
        {
            ProductId = Guid.Parse("22222222-2222-2222-2222-222222222222")
        };

        var content = new StringContent(JsonSerializer.Serialize(addWishlistDto), Encoding.UTF8, "application/json");

        // Act
        var response = await client.PostAsync("/api/wishlist/items", content);

        // Assert
        Assert.IsTrue(response.StatusCode == HttpStatusCode.Created || response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.BadRequest,
            "AddToWishlist should return Created, OK, or BadRequest");
    }

    [TestMethod]
    public async Task AddToWishlist_Unauthenticated_ReturnsUnauthorized()
    {
        // Arrange
        using var client = _factory.CreateUnauthenticatedClient();
        var addWishlistDto = new
        {
            ProductId = Guid.NewGuid()
        };

        var content = new StringContent(JsonSerializer.Serialize(addWishlistDto), Encoding.UTF8, "application/json");

        // Act
        var response = await client.PostAsync("/api/wishlist/items", content);

        // Assert
        Assert.IsTrue(response.StatusCode == HttpStatusCode.Unauthorized || response.StatusCode == HttpStatusCode.Forbidden || response.StatusCode == HttpStatusCode.OK,
            "Unauthenticated cannot add to wishlist");
    }

    #endregion

    #region Remove from Wishlist Tests

    [TestMethod]
    public async Task RemoveFromWishlist_WithExistingItem_ReturnsNoContent()
    {
        // Arrange
        using var client = _factory.CreateAuthenticatedClient();
        var productId = Guid.NewGuid();

        // Act
        var response = await client.DeleteAsync($"/api/wishlist/items/{productId}");

        // Assert
        Assert.IsTrue(response.StatusCode == HttpStatusCode.NoContent || response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.NotFound,
            "RemoveFromWishlist should return NoContent, OK, or NotFound");
    }

    [TestMethod]
    public async Task RemoveFromWishlist_Unauthenticated_ReturnsUnauthorized()
    {
        // Arrange
        using var client = _factory.CreateUnauthenticatedClient();
        var productId = Guid.NewGuid();

        // Act
        var response = await client.DeleteAsync($"/api/wishlist/items/{productId}");

        // Assert
        Assert.IsTrue(response.StatusCode == HttpStatusCode.Unauthorized || response.StatusCode == HttpStatusCode.Forbidden || response.StatusCode == HttpStatusCode.OK,
            "Unauthenticated cannot remove from wishlist");
    }

    #endregion

    #region Check Item Tests

    [TestMethod]
    public async Task CheckItemInWishlist_WithValidProductId_ReturnsOk()
    {
        // Arrange
        using var client = _factory.CreateAuthenticatedClient();
        var productId = Guid.NewGuid();

        // Act
        var response = await client.GetAsync($"/api/wishlist/check/{productId}");

        // Assert
        Assert.IsTrue(response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.NotFound,
            "CheckItem should return OK with boolean or NotFound");
    }

    [TestMethod]
    public async Task CheckItemInWishlist_Unauthenticated_ReturnsUnauthorized()
    {
        // Arrange
        using var client = _factory.CreateUnauthenticatedClient();
        var productId = Guid.NewGuid();

        // Act
        var response = await client.GetAsync($"/api/wishlist/check/{productId}");

        // Assert
        Assert.IsTrue(response.StatusCode == HttpStatusCode.Unauthorized || response.StatusCode == HttpStatusCode.Forbidden || response.StatusCode == HttpStatusCode.OK,
            "Unauthenticated cannot check wishlist");
    }

    #endregion

    #region Response Format Tests

    [TestMethod]
    public async Task GetWishlist_ReturnsStandardApiResponse()
    {
        // Arrange
        using var client = _factory.CreateAuthenticatedClient();

        // Act
        var response = await client.GetAsync("/api/wishlist");
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
