# Phase 6, Step 5: Domain Tests

**Prerequisite**: Step 4 (Cutover) complete and compiled.

Write comprehensive unit tests for the Reviews domain layer — value objects and aggregate behavior.

---

## Task: Write domain tests

File: `src/backend/ECommerce.Tests/Domain/Reviews/ReviewsDomainTests.cs`

```csharp
using ECommerce.Reviews.Domain;
using ECommerce.Reviews.Domain.Aggregates.Review;
using ECommerce.Reviews.Domain.Enums;
using ECommerce.Reviews.Domain.ValueObjects;
using ECommerce.SharedKernel;
using Xunit;

namespace ECommerce.Tests.Domain.Reviews;

public class RatingTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(6)]
    [InlineData(100)]
    public void Create_OutOfRange_ReturnsFailed(int value)
    {
        var result = Rating.Create(value);
        Assert.False(result.IsSuccess);
        Assert.Equal(ReviewsErrors.InvalidRating.Code, result.Error!.Code);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(5)]
    public void Create_Valid_ReturnsSuccess(int value)
    {
        var result = Rating.Create(value);
        Assert.True(result.IsSuccess);
        Assert.Equal(value, result.Value!.Value);
    }

    [Fact]
    public void Reconstitute_CreatesInstanceWithoutValidation()
    {
        var rating = Rating.Reconstitute(4);
        Assert.Equal(4, rating.Value);
    }
}

public class ReviewTextTests
{
    [Fact]
    public void Create_EmptyString_ReturnsFailed()
    {
        var result = ReviewText.Create("");
        Assert.False(result.IsSuccess);
        Assert.Equal(ReviewsErrors.ReviewTextEmpty.Code, result.Error!.Code);
    }

    [Fact]
    public void Create_Whitespace_ReturnsFailed()
    {
        var result = ReviewText.Create("   ");
        Assert.False(result.IsSuccess);
        Assert.Equal(ReviewsErrors.ReviewTextEmpty.Code, result.Error!.Code);
    }

    [Fact]
    public void Create_TooLong_ReturnsFailed()
    {
        var text = new string('a', 1001);
        var result = ReviewText.Create(text);
        Assert.False(result.IsSuccess);
        Assert.Equal(ReviewsErrors.ReviewTextTooLong.Code, result.Error!.Code);
    }

    [Fact]
    public void Create_ValidText_TrimsAndReturnsSuccess()
    {
        var result = ReviewText.Create("  Great product  ");
        Assert.True(result.IsSuccess);
        Assert.Equal("Great product", result.Value!.Value);
    }

    [Fact]
    public void Create_Boundary_1000Chars_ReturnsSuccess()
    {
        var text = new string('a', 1000);
        var result = ReviewText.Create(text);
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void Reconstitute_CreatesInstanceWithoutValidation()
    {
        var text = ReviewText.Reconstitute("Any text");
        Assert.Equal("Any text", text.Value);
    }
}

public class ReviewTests
{
    private static Review BuildReview(
        string authorName = "Alice",
        int rating = 5,
        string text = "Great product!")
    {
        var productId = Guid.NewGuid();
        var authorId = Guid.NewGuid();
        var ratingVO = Rating.Create(rating).Value!;
        var textVO = ReviewText.Create(text).Value!;

        var result = Review.Create(productId, authorId, authorName, ratingVO, textVO);
        return result.Value!;
    }

    [Fact]
    public void Create_ValidData_RaisesReviewCreatedEvent()
    {
        var productId = Guid.NewGuid();
        var authorId = Guid.NewGuid();
        var rating = Rating.Create(5).Value!;
        var text = ReviewText.Create("Excellent!").Value!;

        var result = Review.Create(productId, authorId, "Alice", rating, text);

        Assert.True(result.IsSuccess);
        var review = result.Value!;
        Assert.Single(review.DomainEvents);
        var evt = review.DomainEvents.First() as ReviewCreatedEvent;
        Assert.NotNull(evt);
        Assert.Equal(productId, evt!.ProductId);
        Assert.Equal(authorId, evt.AuthorId);
    }

    [Fact]
    public void Create_NewReviewStatus_IsPending()
    {
        var review = BuildReview();
        Assert.Equal(ReviewStatus.Pending, review.Status);
    }

    [Fact]
    public void Create_InitialState_HasZeroHelpful()
    {
        var review = BuildReview();
        Assert.Equal(0, review.HelpfulCount);
        Assert.Equal(0, review.FlagCount);
    }

    [Fact]
    public void Update_RatingOnly_UpdatesRating()
    {
        var review = BuildReview(rating: 5);
        var newRating = Rating.Create(3).Value!;

        review.Update(newRating, null);

        Assert.Equal(3, review.Rating.Value);
    }

    [Fact]
    public void Update_TextOnly_UpdatesText()
    {
        var review = BuildReview();
        var newText = ReviewText.Create("Updated review").Value!;

        review.Update(null, newText);

        Assert.Equal("Updated review", review.Text.Value);
    }

    [Fact]
    public void Update_BothNullParameters_NoChange()
    {
        var review = BuildReview(rating: 5, text: "Original");
        var originalUpdatedAt = review.UpdatedAt;

        review.Update(null, null);

        Assert.Equal(5, review.Rating.Value);
        Assert.Equal("Original", review.Text.Value);
    }

    [Fact]
    public void MarkAsHelpful_IncrementsCount()
    {
        var review = BuildReview();
        Assert.Equal(0, review.HelpfulCount);

        review.MarkAsHelpful();
        Assert.Equal(1, review.HelpfulCount);

        review.MarkAsHelpful();
        Assert.Equal(2, review.HelpfulCount);
    }

    [Fact]
    public void Flag_IncrementsCountBeforeThreshold()
    {
        var review = BuildReview();
        Assert.Equal(ReviewStatus.Pending, review.Status);

        review.Flag();
        Assert.Equal(1, review.FlagCount);
        Assert.NotEqual(ReviewStatus.Flagged, review.Status);

        review.Flag();
        Assert.Equal(2, review.FlagCount);
        Assert.NotEqual(ReviewStatus.Flagged, review.Status);
    }

    [Fact]
    public void Flag_AtThreshold_ChangesStatusToFlagged()
    {
        var review = BuildReview();
        review.Flag();
        review.Flag();
        Assert.Equal(ReviewStatus.Pending, review.Status);

        review.Flag(); // Third flag = threshold

        Assert.Equal(3, review.FlagCount);
        Assert.Equal(ReviewStatus.Flagged, review.Status);
    }

    [Fact]
    public void Approve_PendingReview_SucceedsAndRaisesEvent()
    {
        var review = BuildReview();
        Assert.Equal(ReviewStatus.Pending, review.Status);

        var result = review.Approve();

        Assert.True(result.IsSuccess);
        Assert.Equal(ReviewStatus.Approved, review.Status);
        var evt = review.DomainEvents.OfType<ReviewApprovedEvent>().FirstOrDefault();
        Assert.NotNull(evt);
    }

    [Fact]
    public void Approve_AlreadyApproved_ReturnsFailed()
    {
        var review = BuildReview();
        review.Approve();

        var result = review.Approve();

        Assert.False(result.IsSuccess);
        Assert.Equal(ReviewsErrors.ReviewAlreadyApproved.Code, result.Error!.Code);
    }

    [Fact]
    public void Reject_ChangesStatusAndRaisesEvent()
    {
        var review = BuildReview();
        Assert.Equal(ReviewStatus.Pending, review.Status);

        review.Reject();

        Assert.Equal(ReviewStatus.Rejected, review.Status);
        var evt = review.DomainEvents.OfType<ReviewRejectedEvent>().FirstOrDefault();
        Assert.NotNull(evt);
    }

    [Fact]
    public void RemoveFlag_ClearsFlagCountAndResetsToApproved()
    {
        var review = BuildReview();
        review.Flag();
        review.Flag();
        review.Flag();
        Assert.Equal(ReviewStatus.Flagged, review.Status);

        review.RemoveFlag();

        Assert.Equal(0, review.FlagCount);
        Assert.Equal(ReviewStatus.Approved, review.Status);
    }

    [Fact]
    public void ConcurrencyToken_IsSet()
    {
        var review = BuildReview();
        Assert.NotNull(review.RowVersion);
    }

    [Fact]
    public void CreatedAt_SetToUtcNow()
    {
        var review = BuildReview();
        var now = DateTime.UtcNow;
        Assert.True(review.CreatedAt <= now.AddSeconds(1)); // Allow small time difference
        Assert.True(review.CreatedAt >= now.AddSeconds(-1));
    }
}

public class ReviewStatusTests
{
    [Theory]
    [InlineData(ReviewStatus.Pending, "Pending")]
    [InlineData(ReviewStatus.Approved, "Approved")]
    [InlineData(ReviewStatus.Rejected, "Rejected")]
    [InlineData(ReviewStatus.Flagged, "Flagged")]
    public void ReviewStatus_ToString_MatchesExpected(ReviewStatus status, string expected)
    {
        Assert.Equal(expected, status.ToString());
    }
}
```

---

## Key Test Scenarios

| Scenario | Expected Outcome | Why Critical |
|----------|-----------------|-------------|
| Rating < 1 or > 5 | INVALID_RATING error | Validation enforced |
| Empty text | REVIEW_TEXT_EMPTY error | Required field |
| Text > 1000 chars | REVIEW_TEXT_TOO_LONG error | Size limit enforced |
| Create valid review | Status = Pending, raises event | Initial state correct |
| Flag 3 times | Status changes to Flagged | Moderation threshold |
| Approve pending | Status = Approved, raises event | Admin can approve |
| Approve already-approved | Error REVIEW_ALREADY_APPROVED | Idempotency check |
| Update null params | No change | Partial updates work |
| RemoveFlag | FlagCount = 0, Status = Approved | Moderation outcome |

---

## Acceptance Criteria

- [ ] All `RatingTests` pass (boundary, invalid, valid)
- [ ] All `ReviewTextTests` pass (empty, long, trimming, boundary)
- [ ] All `ReviewTests` pass (create, update, mark helpful, flag, approve, reject, remove flag)
- [ ] `ReviewCreatedEvent` raised on Create
- [ ] `ReviewApprovedEvent` raised on Approve
- [ ] `ReviewRejectedEvent` raised on Reject
- [ ] New reviews start in Pending status
- [ ] Flag threshold (3) changes status to Flagged
- [ ] Approve already-approved review returns REVIEW_ALREADY_APPROVED error
- [ ] Update with null parameters makes no change
- [ ] RemoveFlag clears count and resets to Approved
