using System.Net;
using System.Text;
using System.Text.Json;
using ECommerce.Application.DTOs.Common;
using ECommerce.Application.DTOs.Reviews;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ECommerce.Tests.Integration;

[TestClass]
public class ReviewsCharacterizationTests
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };
    private static readonly Guid SeededProductId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    private TestWebApplicationFactory _factory = null!;

    [TestInitialize]
    public void Setup()
    {
        _factory = new TestWebApplicationFactory();
    }

    [TestCleanup]
    public void Cleanup()
    {
        ConditionalTestAuthHandler.IsAuthenticationEnabled = true;
        ConditionalTestAuthHandler.CurrentUserId = ConditionalTestAuthHandler.TestUserId;
        ConditionalTestAuthHandler.CurrentUserRole = "Customer";
        _factory?.Dispose();
    }

    private static async Task<ResponseEnvelope<T>> ReadResponseAsync<T>(HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<ResponseEnvelope<T>>(content, JsonOptions)!;
    }

    private static async Task<ResponseEnvelope<object>> ReadErrorResponseAsync(HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<ResponseEnvelope<object>>(content, JsonOptions)!;
    }

    private static StringContent BuildJsonBody(object body)
        => new(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

    private static async Task<ReviewDetailDto> CreateReviewAsync(HttpClient client, int rating = 5, string title = "Great Product", string comment = "Excellent quality and fast delivery")
    {
        var createReviewDto = new CreateReviewDto
        {
            ProductId = SeededProductId,
            Rating = rating,
            Title = title,
            Comment = comment
        };

        var response = await client.PostAsync("/api/reviews", BuildJsonBody(createReviewDto));
        Assert.AreEqual(HttpStatusCode.Created, response.StatusCode, await response.Content.ReadAsStringAsync());

        var envelope = await ReadResponseAsync<ReviewDetailDto>(response);
        Assert.IsNotNull(envelope.Data);
        return envelope.Data!;
    }

    // ────────────────────────────────────────────────
    // GET /api/reviews/product/{productId}
    // ────────────────────────────────────────────────

    [TestMethod]
    public async Task GetProductReviews_ApprovedReview_Returns200AndIncludesApprovedReview()
    {
        using var customerClient = _factory.CreateAuthenticatedClient();
        using var adminClient = _factory.CreateAdminClient();

        var created = await CreateReviewAsync(customerClient, rating: 5, title: "Baseline Review", comment: "Excellent quality and fast delivery");

        var approveResponse = await adminClient.PostAsync($"/api/reviews/{created.Id}/approve", BuildJsonBody(new { }));
        Assert.AreEqual(HttpStatusCode.OK, approveResponse.StatusCode, await approveResponse.Content.ReadAsStringAsync());

        var response = await customerClient.GetAsync($"/api/reviews/product/{SeededProductId}?page=1&pageSize=10");
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, await response.Content.ReadAsStringAsync());

        var envelope = await ReadResponseAsync<PaginatedResult<ReviewDetailDto>>(response);
        Assert.IsNotNull(envelope.Data);
        Assert.IsTrue(envelope.Data!.Items.Any(r => r.Id == created.Id));
    }

    [TestMethod]
    public async Task GetProductReviews_UnknownProduct_Returns404ProductNotFound()
    {
        using var client = _factory.CreateUnauthenticatedClient();

        var response = await client.GetAsync($"/api/reviews/product/{Guid.NewGuid()}?page=1&pageSize=10");

        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
        var envelope = await ReadErrorResponseAsync(response);
        Assert.AreEqual("PRODUCT_NOT_FOUND", envelope.ErrorDetails?.Code);
    }

    // ────────────────────────────────────────────────
    // POST /api/reviews
    // ────────────────────────────────────────────────

    [TestMethod]
    public async Task CreateReview_ValidRequest_Returns201WithLocationAndPendingReview()
    {
        using var client = _factory.CreateAuthenticatedClient();

        var response = await client.PostAsync("/api/reviews", BuildJsonBody(new
        {
            ProductId = SeededProductId,
            Rating = 5,
            Title = "Great Product",
            Comment = "Excellent quality and fast delivery"
        }));

        Assert.AreEqual(HttpStatusCode.Created, response.StatusCode, await response.Content.ReadAsStringAsync());
        Assert.IsNotNull(response.Headers.Location);

        var envelope = await ReadResponseAsync<ReviewDetailDto>(response);
        Assert.IsNotNull(envelope.Data);
        Assert.AreEqual(5, envelope.Data!.Rating);
        Assert.IsFalse(envelope.Data.IsApproved);
    }

    [TestMethod]
    public async Task CreateReview_DuplicateUserProduct_Returns409Conflict()
    {
        using var client = _factory.CreateAuthenticatedClient();

        await CreateReviewAsync(client, rating: 5, title: "First Review", comment: "This is a unique review body");
        var response = await client.PostAsync("/api/reviews", BuildJsonBody(new
        {
            ProductId = SeededProductId,
            Rating = 3,
            Title = "Second Review",
            Comment = "Second attempt for the same product"
        }));

        Assert.AreEqual(HttpStatusCode.Conflict, response.StatusCode);
        var envelope = await ReadErrorResponseAsync(response);
        Assert.AreEqual("DUPLICATE_REVIEW", envelope.ErrorDetails?.Code);
    }

    [TestMethod]
    public async Task CreateReview_InvalidRating_Returns400BadRequest()
    {
        using var client = _factory.CreateAuthenticatedClient();

        var response = await client.PostAsync("/api/reviews", BuildJsonBody(new
        {
            ProductId = SeededProductId,
            Rating = 6,
            Title = "Bad Rating",
            Comment = "Invalid rating value"
        }));

        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
        var envelope = await ReadErrorResponseAsync(response);
        Assert.AreEqual("VALIDATION_FAILED", envelope.ErrorDetails?.Code);
        Assert.IsTrue(envelope.ErrorDetails?.Errors?.ContainsKey("Rating") == true);
    }

    [TestMethod]
    public async Task CreateReview_EmptyComment_Returns400BadRequest()
    {
        using var client = _factory.CreateAuthenticatedClient();

        var response = await client.PostAsync("/api/reviews", BuildJsonBody(new
        {
            ProductId = SeededProductId,
            Rating = 4,
            Title = "Empty Comment",
            Comment = "   "
        }));

        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
        var envelope = await ReadErrorResponseAsync(response);
        Assert.AreEqual("VALIDATION_FAILED", envelope.ErrorDetails?.Code);
        Assert.IsTrue(envelope.ErrorDetails?.Errors?.ContainsKey("Comment") == true);
    }

    // ────────────────────────────────────────────────
    // PUT /api/reviews/{reviewId}
    // ────────────────────────────────────────────────

    [TestMethod]
    public async Task UpdateReview_ValidRequest_Returns200AndUpdatesReview()
    {
        using var client = _factory.CreateAuthenticatedClient();

        var created = await CreateReviewAsync(client, rating: 3, title: "Original Title", comment: "Original comment body");
        var response = await client.PutAsync($"/api/reviews/{created.Id}", BuildJsonBody(new
        {
            Rating = 4,
            Title = "Updated Title",
            Comment = "Updated comment body"
        }));

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, await response.Content.ReadAsStringAsync());
        var envelope = await ReadResponseAsync<ReviewDetailDto>(response);
        Assert.AreEqual(4, envelope.Data!.Rating);
        Assert.AreEqual("Updated Title", envelope.Data.Title);
        Assert.AreEqual("Updated comment body", envelope.Data.Comment);
    }

    [TestMethod]
    public async Task UpdateReview_UnknownId_Returns404ReviewNotFound()
    {
        using var client = _factory.CreateAuthenticatedClient();

        var response = await client.PutAsync($"/api/reviews/{Guid.NewGuid()}", BuildJsonBody(new
        {
            Rating = 5,
            Title = "New Title",
            Comment = "New comment body"
        }));

        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
        var envelope = await ReadErrorResponseAsync(response);
        Assert.AreEqual("REVIEW_NOT_FOUND", envelope.ErrorDetails?.Code);
    }

    // ────────────────────────────────────────────────
    // DELETE /api/reviews/{reviewId}
    // ────────────────────────────────────────────────

    [TestMethod]
    public async Task DeleteReview_ValidId_Returns200()
    {
        using var client = _factory.CreateAuthenticatedClient();

        var created = await CreateReviewAsync(client, rating: 5, title: "Delete Me", comment: "Delete this review after creation");
        var response = await client.DeleteAsync($"/api/reviews/{created.Id}");

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, await response.Content.ReadAsStringAsync());
    }

    [TestMethod]
    public async Task DeleteReview_UnknownId_Returns404ReviewNotFound()
    {
        using var client = _factory.CreateAuthenticatedClient();

        var response = await client.DeleteAsync($"/api/reviews/{Guid.NewGuid()}");

        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
        var envelope = await ReadErrorResponseAsync(response);
        Assert.AreEqual("REVIEW_NOT_FOUND", envelope.ErrorDetails?.Code);
    }

    // ────────────────────────────────────────────────
    // Admin-only routes
    // ────────────────────────────────────────────────

    [TestMethod]
    public async Task ApproveReview_NonAdmin_Returns403Forbidden()
    {
        using var client = _factory.CreateAuthenticatedClient();

        var response = await client.PostAsync($"/api/reviews/{Guid.NewGuid()}/approve", BuildJsonBody(new { }));

        Assert.AreEqual(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [TestMethod]
    public async Task GetPendingReviews_NonAdmin_Returns403Forbidden()
    {
        using var client = _factory.CreateAuthenticatedClient();

        var response = await client.GetAsync("/api/reviews/admin/pending");

        Assert.AreEqual(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [TestMethod]
    public async Task GetPendingReviews_Admin_Returns200AndIncludesPendingReview()
    {
        using var customerClient = _factory.CreateAuthenticatedClient();
        using var adminClient = _factory.CreateAdminClient();

        var created = await CreateReviewAsync(customerClient, rating: 4, title: "Pending Baseline", comment: "Pending review body that should appear for admins");

        var response = await adminClient.GetAsync("/api/reviews/admin/pending?page=1&pageSize=10");

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, await response.Content.ReadAsStringAsync());
        var envelope = await ReadResponseAsync<PaginatedResult<ReviewDetailDto>>(response);
        Assert.IsNotNull(envelope.Data);
        Assert.IsTrue(envelope.Data!.Items.Any(r => r.Id == created.Id));
    }

    private sealed record ResponseEnvelope<T>
    {
        public bool Success { get; init; }
        public T? Data { get; init; }
        public ResponseError? ErrorDetails { get; init; }
        public string? TraceId { get; init; }
    }

    private sealed record ResponseError
    {
        public string? Message { get; init; }
        public string? Code { get; init; }
        public Dictionary<string, string[]>? Errors { get; init; }
        public string? TraceId { get; init; }
    }
}
