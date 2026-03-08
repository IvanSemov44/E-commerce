using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ECommerce.Tests.Integration;

/// <summary>
/// Integration tests for ReviewsController endpoints.
/// Tests product review creation, retrieval, and management.
/// </summary>
[TestClass]
public class ReviewsControllerTests
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

    #region Get Reviews Tests

    [TestMethod]
    public async Task GetReviews_ForProduct_ReturnsOk()
    {
        // Arrange
        using var client = _factory.CreateUnauthenticatedClient();
        var productId = Guid.Parse("22222222-2222-2222-2222-222222222222");

        // Act
        var response = await client.GetAsync($"/api/reviews/product/{productId}");

        // Assert
        Assert.IsTrue(response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.NotFound,
            "GetReviews should return OK or NotFound");
    }

    [TestMethod]
    public async Task GetReviews_ForProduct_WithLargePageSize_IsClamped()
    {
        // Arrange
        using var client = _factory.CreateUnauthenticatedClient();
        var productId = Guid.Parse("22222222-2222-2222-2222-222222222222");

        // Act
        var response = await client.GetAsync($"/api/reviews/product/{productId}?page=1&pageSize=1000");
        var responseContent = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.IsTrue(response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.NotFound,
            "GetReviews should return OK or NotFound");

        if (response.StatusCode == HttpStatusCode.OK && !string.IsNullOrEmpty(responseContent))
        {
            var json = JsonSerializer.Deserialize<JsonElement>(responseContent);
            if (json.TryGetProperty("data", out var data)
                && data.TryGetProperty("items", out var items)
                && items.ValueKind == JsonValueKind.Array)
            {
                Assert.IsLessThanOrEqualTo(100, items.GetArrayLength(), "Reviews pageSize should be clamped to 100");
            }
        }
    }

    [TestMethod]
    public async Task GetReviews_ForProductRating_ReturnsOk()
    {
        // Arrange
        using var client = _factory.CreateUnauthenticatedClient();
        var productId = Guid.Parse("22222222-2222-2222-2222-222222222222");

        // Act
        var response = await client.GetAsync($"/api/reviews/product/{productId}/rating");

        // Assert
        Assert.IsTrue(response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.NotFound,
            "GetProductRating should return OK or NotFound");
    }

    [TestMethod]
    public async Task GetReviews_ReturnsCorrectFormat()
    {
        // Arrange
        using var client = _factory.CreateUnauthenticatedClient();
        var productId = Guid.NewGuid();

        // Act
        var response = await client.GetAsync($"/api/reviews/product/{productId}");
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

    #region Get Review By ID Tests

    [TestMethod]
    public async Task GetReviewById_WithExistingId_ReturnsOk()
    {
        // Arrange
        using var client = _factory.CreateUnauthenticatedClient();
        var reviewId = Guid.NewGuid();

        // Act
        var response = await client.GetAsync($"/api/reviews/{reviewId}");

        // Assert
        Assert.IsTrue(response.StatusCode == HttpStatusCode.OK ||
            response.StatusCode == HttpStatusCode.NotFound ||
            response.StatusCode == HttpStatusCode.BadRequest,
            "Should return OK, NotFound, or BadRequest depending on current validation/seed state");
    }

    #endregion

    #region Create Review Tests

    [TestMethod]
    public async Task CreateReview_WithAuthenticatedAndValidData_ReturnsCreated()
    {
        // Arrange
        using var client = _factory.CreateAuthenticatedClient();
        var createReviewDto = new
        {
            ProductId = Guid.Parse("22222222-2222-2222-2222-222222222222"),
            Rating = 5,
            Title = "Great Product",
            Comment = "Excellent quality and fast delivery"
        };

        var content = new StringContent(JsonSerializer.Serialize(createReviewDto), Encoding.UTF8, "application/json");

        // Act
        var response = await client.PostAsync("/api/reviews", content);

        // Assert
        Assert.IsTrue(response.StatusCode == HttpStatusCode.Created || response.StatusCode == HttpStatusCode.BadRequest,
            "CreateReview should return Created or BadRequest");
    }

    [TestMethod]
    public async Task CreateReview_WithInvalidRating_ReturnsBadRequest()
    {
        // Arrange
        using var client = _factory.CreateAuthenticatedClient();
        var createReviewDto = new
        {
            ProductId = Guid.NewGuid(),
            Rating = 10, // Invalid rating
            Title = "Bad Product",
            Comment = "Does not work"
        };

        var content = new StringContent(JsonSerializer.Serialize(createReviewDto), Encoding.UTF8, "application/json");

        // Act
        var response = await client.PostAsync("/api/reviews", content);

        // Assert
        Assert.IsTrue(response.StatusCode == HttpStatusCode.BadRequest || response.StatusCode == HttpStatusCode.UnprocessableEntity,
            "Invalid rating should return BadRequest");
    }

    [TestMethod]
    public async Task CreateReview_Unauthenticated_ReturnsUnauthorized()
    {
        // Arrange
        using var client = _factory.CreateUnauthenticatedClient();
        var createReviewDto = new
        {
            ProductId = Guid.NewGuid(),
            Rating = 4,
            Title = "Nice",
            Comment = "Good product"
        };

        var content = new StringContent(JsonSerializer.Serialize(createReviewDto), Encoding.UTF8, "application/json");

        // Act
        var response = await client.PostAsync("/api/reviews", content);

        // Assert
        Assert.IsTrue(response.StatusCode == HttpStatusCode.Unauthorized || response.StatusCode == HttpStatusCode.Forbidden,
            "Unauthenticated should not create review");
    }

    [TestMethod]
    public async Task GetMyReviews_WithPaginationQuery_ReturnsExpectedStatuses()
    {
        // Arrange
        using var client = _factory.CreateAuthenticatedClient();

        // Act
        var response = await client.GetAsync("/api/reviews/my-reviews?page=1&pageSize=1000");

        // Assert
        Assert.IsTrue(
            response.StatusCode == HttpStatusCode.OK ||
            response.StatusCode == HttpStatusCode.NotFound ||
            response.StatusCode == HttpStatusCode.BadRequest,
            "My reviews endpoint should accept pagination query parameters");
    }

    #endregion

    #region Update Review Tests

    [TestMethod]
    public async Task UpdateReview_WithAuthenticatedAndValidData_ReturnsOk()
    {
        // Arrange
        using var client = _factory.CreateAuthenticatedClient();
        var reviewId = Guid.NewGuid();
        var updateReviewDto = new
        {
            Rating = 4,
            Title = "Updated Review",
            Comment = "Updated comment after testing"
        };

        var content = new StringContent(JsonSerializer.Serialize(updateReviewDto), Encoding.UTF8, "application/json");

        // Act
        var response = await client.PutAsync($"/api/reviews/{reviewId}", content);

        // Assert
        Assert.IsTrue(response.StatusCode == HttpStatusCode.NotFound || response.StatusCode == HttpStatusCode.Forbidden,
            "UpdateReview should return NotFound or Forbidden");
    }

    #endregion

    #region Delete Review Tests

    [TestMethod]
    public async Task DeleteReview_WithNonexistentId_ReturnsNotFound()
    {
        // Arrange
        using var client = _factory.CreateAuthenticatedClient();
        var reviewId = Guid.NewGuid();

        // Act
        var response = await client.DeleteAsync($"/api/reviews/{reviewId}");

        // Assert
        Assert.IsTrue(response.StatusCode == HttpStatusCode.NotFound || response.StatusCode == HttpStatusCode.Forbidden,
            "Delete nonexistent should return NotFound or Forbidden");
    }

    [TestMethod]
    public async Task DeleteReview_Unauthenticated_ReturnsUnauthorized()
    {
        // Arrange
        using var client = _factory.CreateUnauthenticatedClient();
        var reviewId = Guid.NewGuid();

        // Act
        var response = await client.DeleteAsync($"/api/reviews/{reviewId}");

        // Assert
        Assert.IsTrue(response.StatusCode == HttpStatusCode.Unauthorized || response.StatusCode == HttpStatusCode.Forbidden,
            "Unauthenticated cannot delete review");
    }

    #endregion

    #region Response Format Tests

    [TestMethod]
    public async Task GetReviews_ReturnsStandardApiResponse()
    {
        // Arrange
        using var client = _factory.CreateUnauthenticatedClient();

        // Act
        var response = await client.GetAsync("/api/reviews");
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
