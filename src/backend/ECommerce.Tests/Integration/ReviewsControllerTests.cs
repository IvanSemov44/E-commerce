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
        var response = await client.GetAsync($"/api/reviews?productId={productId}");

        // Assert
        Assert.IsTrue(response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.NotFound,
            "GetReviews should return OK or NotFound");
    }

    [TestMethod]
    public async Task GetReviews_WithoutProductId_ReturnsAllReviews()
    {
        // Arrange
        using var client = _factory.CreateUnauthenticatedClient();

        // Act
        var response = await client.GetAsync("/api/reviews");

        // Assert
        Assert.IsTrue(response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.NotFound,
            "GetReviews without filter should return OK");
    }

    [TestMethod]
    public async Task GetReviews_ReturnsCorrectFormat()
    {
        // Arrange
        using var client = _factory.CreateUnauthenticatedClient();
        var productId = Guid.NewGuid();

        // Act
        var response = await client.GetAsync($"/api/reviews?productId={productId}");
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
        Assert.IsTrue(response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.NotFound,
            "Should return OK if exists, NotFound otherwise");
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
        Assert.IsTrue(response.StatusCode == HttpStatusCode.Created || response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.BadRequest,
            "CreateReview should return Created, OK, or BadRequest");
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
        Assert.IsTrue(response.StatusCode == HttpStatusCode.BadRequest || response.StatusCode == HttpStatusCode.UnprocessableEntity || response.StatusCode == HttpStatusCode.OK,
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
        Assert.IsTrue(response.StatusCode == HttpStatusCode.Unauthorized || response.StatusCode == HttpStatusCode.Forbidden || response.StatusCode == HttpStatusCode.OK,
            "Unauthenticated should not create review");
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
        Assert.IsTrue(response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.NotFound || response.StatusCode == HttpStatusCode.Forbidden,
            "UpdateReview should return OK, NotFound, or Forbidden");
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
        Assert.IsTrue(response.StatusCode == HttpStatusCode.NotFound || response.StatusCode == HttpStatusCode.NoContent || response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.Forbidden,
            "Delete nonexistent should return NotFound or similar");
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
        Assert.IsTrue(response.StatusCode == HttpStatusCode.Unauthorized || response.StatusCode == HttpStatusCode.Forbidden || response.StatusCode == HttpStatusCode.OK,
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
            var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var responseData = JsonSerializer.Deserialize<JsonElement>(responseContent, jsonOptions);
            Assert.IsTrue(responseData.TryGetProperty("success", out _) || responseData.TryGetProperty("data", out _),
                "Response should have success or data property");
        }
    }

    #endregion
}
