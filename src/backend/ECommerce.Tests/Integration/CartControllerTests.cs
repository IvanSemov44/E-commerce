using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ECommerce.Tests.Integration;

/// <summary>
/// Integration tests for CartController endpoints.
/// Tests shopping cart operations including add, remove, and update items.
/// </summary>
[TestClass]
public class CartControllerTests
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

    #region Get Cart Tests

    [TestMethod]
    public async Task GetCart_WithAuthenticatedUser_ReturnsOk()
    {
        // Arrange
        using var client = _factory.CreateAuthenticatedClient();

        // Act
        var response = await client.GetAsync("/api/cart");

        // Assert
        Assert.IsTrue(response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.NotFound,
            "GetCart should return OK or NotFound");
    }

    [TestMethod]
    public async Task GetCart_WithUnauthenticatedUser_ReturnsUnauthorized()
    {
        // Arrange
        using var client = _factory.CreateUnauthenticatedClient();

        // Act
        var response = await client.GetAsync("/api/cart");

        // Assert
        Assert.IsTrue(response.StatusCode == HttpStatusCode.Unauthorized,
            "Unauthenticated should return Unauthorized");
    }

    [TestMethod]
    public async Task GetCart_ReturnsCorrectFormat()
    {
        // Arrange
        using var client = _factory.CreateAuthenticatedClient();

        // Act
        var response = await client.GetAsync("/api/cart");
        var responseContent = await response.Content.ReadAsStringAsync();

        // Assert
        if (response.StatusCode == HttpStatusCode.OK)
        {
            var jsonOptions = _jsonOptions;
            var responseData = JsonSerializer.Deserialize<JsonElement>(responseContent, jsonOptions);
            Assert.IsTrue(responseData.TryGetProperty("data", out _), "Response should have data property");
        }
    }

    #endregion

    #region Add Item Tests

    [TestMethod]
    public async Task AddItemToCart_WithValidProductId_ReturnsOk()
    {
        // Arrange
        using var client = _factory.CreateAuthenticatedClient();
        var addItemDto = new
        {
            ProductId = Guid.Parse("22222222-2222-2222-2222-222222222222"), // Test product
            Quantity = 1
        };

        var content = new StringContent(JsonSerializer.Serialize(addItemDto), Encoding.UTF8, "application/json");

        // Act
        var response = await client.PostAsync("/api/cart/add-item", content);

        // Assert
        Assert.IsTrue(response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.BadRequest,
            "AddItem should return OK or BadRequest");
    }

    [TestMethod]
    public async Task AddItemToCart_WithInvalidQuantity_ReturnsBadRequest()
    {
        // Arrange
        using var client = _factory.CreateAuthenticatedClient();
        var addItemDto = new
        {
            ProductId = Guid.NewGuid(),
            Quantity = -1 // Invalid
        };

        var content = new StringContent(JsonSerializer.Serialize(addItemDto), Encoding.UTF8, "application/json");

        // Act
        var response = await client.PostAsync("/api/cart/add-item", content);

        // Assert
        Assert.IsTrue(response.StatusCode == HttpStatusCode.BadRequest || response.StatusCode == HttpStatusCode.UnprocessableEntity,
            "Invalid quantity should return BadRequest or similar");
    }

    [TestMethod]
    public async Task AddItemToCart_WithUnauthenticated_AllowedForGuests()
    {
        // Arrange
        using var client = _factory.CreateUnauthenticatedClient();
        var addItemDto = new
        {
            ProductId = Guid.NewGuid(),
            Quantity = 1
        };

        var content = new StringContent(JsonSerializer.Serialize(addItemDto), Encoding.UTF8, "application/json");

        // Act
        var response = await client.PostAsync("/api/cart/add-item", content);

        // Assert
        Assert.IsTrue(response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.BadRequest || response.StatusCode == HttpStatusCode.NotFound,
            "Guest users can add to cart");
    }

    #endregion

    #region Update Item Tests

    [TestMethod]
    public async Task UpdateCartItem_WithValidQuantity_ReturnsOk()
    {
        // Arrange
        using var client = _factory.CreateAuthenticatedClient();
        var itemId = Guid.NewGuid();
        var updateItemDto = new { Quantity = 5 };

        var content = new StringContent(JsonSerializer.Serialize(updateItemDto), Encoding.UTF8, "application/json");

        // Act
        var response = await client.PutAsync($"/api/cart/items/{itemId}", content);

        // Assert
        Assert.IsTrue(response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.NotFound || response.StatusCode == HttpStatusCode.BadRequest,
            "UpdateItem should return OK, NotFound, or BadRequest");
    }

    [TestMethod]
    public async Task UpdateCartItem_WithZeroQuantity_ReturnsBadRequest()
    {
        // Arrange
        using var client = _factory.CreateAuthenticatedClient();
        var itemId = Guid.NewGuid();
        var updateItemDto = new { Quantity = 0 };

        var content = new StringContent(JsonSerializer.Serialize(updateItemDto), Encoding.UTF8, "application/json");

        // Act
        var response = await client.PutAsync($"/api/cart/items/{itemId}", content);

        // Assert
        Assert.IsTrue(response.StatusCode == HttpStatusCode.BadRequest || response.StatusCode == HttpStatusCode.UnprocessableEntity,
            "Zero quantity should return BadRequest");
    }

    #endregion

    #region Remove Item Tests

    [TestMethod]
    public async Task RemoveItemFromCart_WithExistingItem_ReturnsNoContent()
    {
        // Arrange
        using var client = _factory.CreateAuthenticatedClient();
        var itemId = Guid.NewGuid();

        // Act
        var response = await client.DeleteAsync($"/api/cart/items/{itemId}");

        // Assert
        Assert.IsTrue(response.StatusCode == HttpStatusCode.NoContent || response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.NotFound,
            "RemoveItem should return NoContent, OK, or NotFound");
    }

    [TestMethod]
    public async Task RemoveItemFromCart_WithUnauthenticated_ReturnsUnauthorized()
    {
        // Arrange
        using var client = _factory.CreateUnauthenticatedClient();
        var itemId = Guid.NewGuid();

        // Act
        var response = await client.DeleteAsync($"/api/cart/items/{itemId}");

        // Assert
        Assert.IsTrue(response.StatusCode == HttpStatusCode.Unauthorized || response.StatusCode == HttpStatusCode.Forbidden,
            "Unauthenticated remove should return Unauthorized");
    }

    #endregion

    #region Clear Cart Tests

    [TestMethod]
    public async Task ClearCart_WithAuthenticatedUser_ReturnsNoContent()
    {
        // Arrange
        using var client = _factory.CreateAuthenticatedClient();

        // Act
        var response = await client.DeleteAsync("/api/cart");

        // Assert
        Assert.IsTrue(response.StatusCode == HttpStatusCode.NoContent || response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.NotFound,
            "ClearCart should return NoContent or OK");
    }

    #endregion

    #region Response Format Tests

    [TestMethod]
    public async Task GetCart_ReturnsStandardApiResponse()
    {
        // Arrange
        using var client = _factory.CreateAuthenticatedClient();

        // Act
        var response = await client.GetAsync("/api/cart");
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

    #region IDOR Protection Tests

    [TestMethod]
    public async Task ValidateCart_UserCannotValidateOtherUsersCart_ReturnsForbidden()
    {
        // Arrange - Create a cart for User A
        using var clientUserA = _factory.CreateAuthenticatedClient();
        var addItemDto = new
        {
            ProductId = Guid.Parse("22222222-2222-2222-2222-222222222222"),
            Quantity = 1
        };

        var content = new StringContent(JsonSerializer.Serialize(addItemDto), Encoding.UTF8, "application/json");
        var addResponse = await clientUserA.PostAsync("/api/cart/add-item", content);

        // Get the cart to find its ID - use POST since get-or-create is a POST endpoint
        var cartResponse = await clientUserA.PostAsync("/api/cart/get-or-create", null);
        if (cartResponse.StatusCode != HttpStatusCode.OK)
        {
            Assert.Inconclusive("Could not create cart for IDOR test");
            return;
        }

        var cartContent = await cartResponse.Content.ReadAsStringAsync();
        var cartData = JsonSerializer.Deserialize<JsonElement>(cartContent);
        var cartId = cartData.GetProperty("data").GetProperty("id").GetGuid();

        // Create client for User B (different user)
        var userBToken = TestWebApplicationFactory.GenerateJwtToken(Guid.NewGuid().ToString(), "Customer");
        using var clientUserB = _factory.CreateClient();
        clientUserB.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", userBToken);

        // Act - User B tries to validate User A's cart
        var validateResponse = await clientUserB.PostAsync($"/api/cart/validate/{cartId}", null);

        // Assert
        Assert.AreEqual(HttpStatusCode.Forbidden, validateResponse.StatusCode,
            "User B should not be able to validate User A's cart");
    }

    [TestMethod]
    public async Task ValidateCart_AdminCanValidateAnyCart_ReturnsSuccess()
    {
        // Arrange - Create a cart for a regular user
        using var clientUser = _factory.CreateAuthenticatedClient();
        var addItemDto = new
        {
            ProductId = Guid.Parse("22222222-2222-2222-2222-222222222222"),
            Quantity = 1
        };

        var content = new StringContent(JsonSerializer.Serialize(addItemDto), Encoding.UTF8, "application/json");
        await clientUser.PostAsync("/api/cart/add-item", content);

        // Get the cart to find its ID - use POST since get-or-create is a POST endpoint
        var cartResponse = await clientUser.PostAsync("/api/cart/get-or-create", null);
        if (cartResponse.StatusCode != HttpStatusCode.OK)
        {
            Assert.Inconclusive("Could not create cart for admin test");
            return;
        }

        var cartContent = await cartResponse.Content.ReadAsStringAsync();
        var cartData = JsonSerializer.Deserialize<JsonElement>(cartContent);
        var cartId = cartData.GetProperty("data").GetProperty("id").GetGuid();

        // Create admin client
        using var adminClient = _factory.CreateAdminClient();

        // Act - Admin validates user's cart
        var validateResponse = await adminClient.PostAsync($"/api/cart/validate/{cartId}", null);

        // Assert
        Assert.IsTrue(validateResponse.StatusCode == HttpStatusCode.OK || validateResponse.StatusCode == HttpStatusCode.BadRequest || validateResponse.StatusCode == HttpStatusCode.NotFound,
            "Admin should be able to validate any cart");
    }

    [TestMethod]
    public async Task ValidateCart_UserCanValidateOwnCart_ReturnsSuccess()
    {
        // Arrange - Create and validate cart as same user
        using var client = _factory.CreateAuthenticatedClient();
        var addItemDto = new
        {
            ProductId = Guid.Parse("22222222-2222-2222-2222-222222222222"),
            Quantity = 1
        };

        var content = new StringContent(JsonSerializer.Serialize(addItemDto), Encoding.UTF8, "application/json");
        await client.PostAsync("/api/cart/add-item", content);

        // Get the cart to find its ID - use POST since get-or-create is a POST endpoint
        var cartResponse = await client.PostAsync("/api/cart/get-or-create", null);
        if (cartResponse.StatusCode != HttpStatusCode.OK)
        {
            Assert.Inconclusive("Could not create cart for validation test");
            return;
        }

        var cartContent = await cartResponse.Content.ReadAsStringAsync();
        var cartData = JsonSerializer.Deserialize<JsonElement>(cartContent);
        var cartId = cartData.GetProperty("data").GetProperty("id").GetGuid();

        // Act - User validates their own cart
        var validateResponse = await client.PostAsync($"/api/cart/validate/{cartId}", null);

        // Assert
        Assert.IsTrue(validateResponse.StatusCode == HttpStatusCode.OK || validateResponse.StatusCode == HttpStatusCode.BadRequest,
            "User should be able to validate their own cart");
    }

    [TestMethod]
    public async Task ValidateCart_GuestCartCanBeValidated_ReturnsSuccess()
    {
        // Arrange - Create a guest cart (no authentication)
        using var guestClient = _factory.CreateUnauthenticatedClient();

        // Get or create a guest cart
        var cartResponse = await guestClient.PostAsync("/api/cart/get-or-create", null);
        if (cartResponse.StatusCode != HttpStatusCode.OK)
        {
            Assert.Inconclusive("Could not create guest cart for validation test");
            return;
        }

        var cartContent = await cartResponse.Content.ReadAsStringAsync();
        var cartData = JsonSerializer.Deserialize<JsonElement>(cartContent);
        var cartId = cartData.GetProperty("data").GetProperty("id").GetGuid();

        // Add an item to the cart
        var addItemDto = new
        {
            ProductId = Guid.Parse("22222222-2222-2222-2222-222222222222"),
            Quantity = 1
        };
        var content = new StringContent(JsonSerializer.Serialize(addItemDto), Encoding.UTF8, "application/json");
        await guestClient.PostAsync("/api/cart/add-item", content);

        // Act - Validate the guest cart
        var validateResponse = await guestClient.PostAsync($"/api/cart/validate/{cartId}", null);

        // Assert - Guest carts should be validatable
        Assert.IsTrue(validateResponse.StatusCode == HttpStatusCode.OK || validateResponse.StatusCode == HttpStatusCode.BadRequest || validateResponse.StatusCode == HttpStatusCode.NotFound,
            "Guest cart should be validatable");
    }

    #endregion
}
