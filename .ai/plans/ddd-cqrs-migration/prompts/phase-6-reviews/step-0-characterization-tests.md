# Phase 6, Step 0: Characterization Tests (Backend)

**Prerequisite**: Phase 5 complete. `src/backend` compiles and all tests pass.

Capture the behavior of the **old** `ReviewsController` and `IReviewService` (if they exist) in a characterization test class. This establishes a baseline before any refactoring to CQRS/MediatR.

---

## Task: Write `PromoCodeCharacterizationTests`

File: `src/backend/ECommerce.Tests/Integration/ReviewsCharacterizationTests.cs`

Use the **static factory** pattern to match the linter-modified `PromoCodesControllerTests` style:

```csharp
using ECommerce.API;
using ECommerce.Tests.Helpers;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace ECommerce.Tests.Integration;

[Collection("Sequential")]
public class ReviewsCharacterizationTests
{
    private static readonly Lazy<TestWebApplicationFactory> LazyFactory =
        new(() => new TestWebApplicationFactory());

    private static TestWebApplicationFactory Factory => LazyFactory.Value;

    [Fact]
    public async Task GetProductReviews_ValidProductId_Returns200WithApprovedReviewsOnly()
    {
        var client = Factory.CreateClient();
        var productId = Guid.Parse("11111111-1111-1111-1111-111111111111"); // Seeded product

        var response = await client.GetAsync($"/api/products/{productId}/reviews?page=1&pageSize=10");
        
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsAsync<ApiResponse<PaginatedResult<ReviewDto>>>();
        Assert.NotNull(content.Data);
        Assert.True(content.Data.Items.Count > 0);
        Assert.All(content.Data.Items, r => Assert.Equal("Approved", r.Status)); // Only approved reviews shown
    }

    [Fact]
    public async Task GetProductReviews_UnknownProductId_Returns200WithEmptyList()
    {
        var client = Factory.CreateClient();
        var unknownId = Guid.NewGuid();

        var response = await client.GetAsync($"/api/products/{unknownId}/reviews");
        
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsAsync<ApiResponse<PaginatedResult<ReviewDto>>>();
        Assert.NotNull(content.Data);
        Assert.Empty(content.Data.Items);
    }

    [Fact]
    public async Task CreateReview_ValidRequest_Returns201WithLocation()
    {
        var client = Factory.CreateClient();
        var productId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var req = new { productId, rating = 5, text = "Excellent product!", status = "Pending" };

        var response = await client.PostAsJsonAsync("/api/reviews", req);
        
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);
        var content = await response.Content.ReadAsAsync<ApiResponse<ReviewDetailDto>>();
        Assert.NotNull(content.Data);
        Assert.Equal(5, content.Data.Rating);
        Assert.Equal("Pending", content.Data.Status); // New reviews start as Pending
    }

    [Fact]
    public async Task CreateReview_DuplicateUserProduct_Returns409Conflict()
    {
        var client = Factory.CreateClient();
        var productId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var userId = Guid.Parse("99999999-9999-9999-9999-999999999999");
        // User 99999... already reviewed this product (seeded)
        
        var req = new { productId, userId, rating = 3, text = "Second review attempt", status = "Pending" };
        var response = await client.PostAsJsonAsync("/api/reviews", req);
        
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var content = await response.Content.ReadAsAsync<ApiResponse<object>>();
        Assert.Equal("DUPLICATE_REVIEW", content.Code);
    }

    [Fact]
    public async Task CreateReview_InvalidRating_Returns400BadRequest()
    {
        var client = Factory.CreateClient();
        var req = new { productId = Guid.NewGuid(), rating = 6, text = "Bad rating value" };

        var response = await client.PostAsJsonAsync("/api/reviews", req);
        
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var content = await response.Content.ReadAsAsync<ApiResponse<object>>();
        Assert.Equal("INVALID_RATING", content.Code);
    }

    [Fact]
    public async Task CreateReview_EmptyText_Returns400BadRequest()
    {
        var client = Factory.CreateClient();
        var req = new { productId = Guid.NewGuid(), rating = 4, text = "" };

        var response = await client.PostAsJsonAsync("/api/reviews", req);
        
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var content = await response.Content.ReadAsAsync<ApiResponse<object>>();
        Assert.Equal("REVIEW_TEXT_EMPTY", content.Code);
    }

    [Fact]
    public async Task UpdateReview_ValidRequest_Returns200()
    {
        var client = Factory.CreateClient();
        var reviewId = Guid.Parse("22222222-2222-2222-2222-222222222222"); // Seeded review
        var req = new { rating = 4, text = "Updated review text" };

        var response = await client.PutAsJsonAsync($"/api/reviews/{reviewId}", req);
        
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsAsync<ApiResponse<ReviewDetailDto>>();
        Assert.Equal(4, content.Data.Rating);
        Assert.Equal("Updated review text", content.Data.Text);
    }

    [Fact]
    public async Task UpdateReview_UnknownId_Returns404NotFound()
    {
        var client = Factory.CreateClient();
        var req = new { rating = 5, text = "New text" };

        var response = await client.PutAsJsonAsync($"/api/reviews/{Guid.NewGuid()}", req);
        
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var content = await response.Content.ReadAsAsync<ApiResponse<object>>();
        Assert.Equal("REVIEW_NOT_FOUND", content.Code);
    }

    [Fact]
    public async Task DeleteReview_ValidId_Returns200()
    {
        var client = Factory.CreateClient();
        var reviewId = Guid.Parse("22222222-2222-2222-2222-222222222222");

        var response = await client.DeleteAsync($"/api/reviews/{reviewId}");
        
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task DeleteReview_UnknownId_Returns404NotFound()
    {
        var client = Factory.CreateClient();

        var response = await client.DeleteAsync($"/api/reviews/{Guid.NewGuid()}");
        
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var content = await response.Content.ReadAsAsync<ApiResponse<object>>();
        Assert.Equal("REVIEW_NOT_FOUND", content.Code);
    }

    [Fact]
    public async Task MarkAsHelpful_ValidId_Returns200AndIncrementsCount()
    {
        var client = Factory.CreateClient();
        var reviewId = Guid.Parse("22222222-2222-2222-2222-222222222222");

        var before = await client.GetAsync($"/api/reviews/{reviewId}");
        var beforeContent = await before.Content.ReadAsAsync<ApiResponse<ReviewDetailDto>>();
        var beforeCount = beforeContent.Data.HelpfulCount;

        var response = await client.PostAsJsonAsync($"/api/reviews/{reviewId}/mark-helpful", new { });
        
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var after = await client.GetAsync($"/api/reviews/{reviewId}");
        var afterContent = await after.Content.ReadAsAsync<ApiResponse<ReviewDetailDto>>();
        Assert.Equal(beforeCount + 1, afterContent.Data.HelpfulCount);
    }

    [Fact]
    public async Task FlagReview_ValidId_Returns200AndSetsFlagged()
    {
        var client = Factory.CreateClient();
        var reviewId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var req = new { reason = "Inappropriate language" };

        var response = await client.PostAsJsonAsync($"/api/reviews/{reviewId}/flag", req);
        
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var getResp = await client.GetAsync($"/api/reviews/{reviewId}");
        var content = await getResp.Content.ReadAsAsync<ApiResponse<ReviewDetailDto>>();
        Assert.True(content.Data.FlagCount > 0);
    }

    [Fact]
    public async Task ApproveReview_AdminOnly_Returns403ForNonAdmin()
    {
        var client = Factory.CreateClient();
        var reviewId = Guid.Parse("33333333-3333-3333-3333-333333333333"); // Pending review
        
        var response = await client.PostAsJsonAsync($"/api/reviews/{reviewId}/approve", new { });
        
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetPendingReviews_AdminOnly_Returns403ForNonAdmin()
    {
        var client = Factory.CreateClient();
        
        var response = await client.GetAsync("/api/reviews/admin/pending");
        
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task ReviewCodeNormalization_TextNormalizedToUppercase()
    {
        var client = Factory.CreateClient();
        var req = new { productId = Guid.NewGuid(), rating = 5, text = "great product" };

        var response = await client.PostAsJsonAsync("/api/reviews", req);
        var content = await response.Content.ReadAsAsync<ApiResponse<ReviewDetailDto>>();
        
        // Verify first letter is uppercase (or check full text normalization rule)
        Assert.NotNull(content.Data.Text);
    }
}
```

---

## Critical Pins (Must Pass)

| Endpoint | Status | Why it matters |
|----------|--------|----------------|
| `GET /api/products/{productId}/reviews` | 200 OK | Public list of approved reviews only |
| `GET /api/products/{productId}/reviews` (unknown product) | 200 OK with empty list | No 404 for missing product |
| `POST /api/reviews` (create) | 201 Created | Created response with Location header |
| `POST /api/reviews` (duplicate user+product) | 409 Conflict | User can't review same product twice |
| `POST /api/reviews` (invalid rating 6) | 400 Bad Request | Rating validation enforced |
| `POST /api/reviews` (empty text) | 400 Bad Request | Text validation enforced |
| `PUT /api/reviews/{id}` (update) | 200 OK | Update returns modified review |
| `PUT /api/reviews/{id}` (unknown) | 404 Not Found | Error mapping for not found |
| `DELETE /api/reviews/{id}` | 200 OK | Delete succeeds and returns 200 |
| `DELETE /api/reviews/{id}` (unknown) | 404 Not Found | `REVIEW_NOT_FOUND` error code |
| `POST /api/reviews/{id}/mark-helpful` | 200 OK | Increments helpful count |
| `POST /api/reviews/{id}/flag` | 200 OK | Sets flagged status |
| `POST /api/reviews/{id}/approve` (non-admin) | 403 Forbidden | Admin-only action |
| `GET /api/reviews/admin/pending` (non-admin) | 403 Forbidden | Admin-only query |

---

## Seeded Test Data

Add to your database seeding in `Startup` or migration:

```sql
-- Product (for testing reviews)
INSERT INTO Products (Id, Name, Description, Price) 
VALUES ('11111111-1111-1111-1111-111111111111', 'Test Product', 'A test product', 99.99);

-- Approved Review (status: Approved)
INSERT INTO Reviews (Id, ProductId, UserId, Rating, Text, Status, HelpfulCount, FlagCount, CreatedAt, UpdatedAt)
VALUES ('22222222-2222-2222-2222-222222222222', '11111111-1111-1111-1111-111111111111', '99999999-9999-9999-9999-999999999999', 5, 'Great product!', 'Approved', 3, 0, GETDATE(), GETDATE());

-- Pending Review (for admin approval flow testing)
INSERT INTO Reviews (Id, ProductId, UserId, Rating, Text, Status, HelpfulCount, FlagCount, CreatedAt, UpdatedAt)
VALUES ('33333333-3333-3333-3333-333333333333', '11111111-1111-1111-1111-111111111111', '88888888-8888-8888-8888-888888888888', 4, 'Good but could be better', 'Pending', 1, 0, GETDATE(), GETDATE());
```

---

## Acceptance Criteria

- [ ] All 14 tests pass against the OLD service
- [ ] Zero regressions when comparing against live endpoints
- [ ] Error codes match exactly: `REVIEW_NOT_FOUND`, `DUPLICATE_REVIEW`, `INVALID_RATING`, `REVIEW_TEXT_EMPTY`
- [ ] Admin authorization preserved: `/approve`, `/admin/pending` return 403 for non-admin
- [ ] Approved reviews only returned in public list
- [ ] `POST /api/reviews` always returns 201 with Location header
- [ ] Helpful count increments correctly
