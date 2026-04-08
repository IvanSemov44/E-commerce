using ECommerce.Reviews.Domain.Aggregates.Review;
using ECommerce.Reviews.Domain.Errors;
using ECommerce.Reviews.Domain.Enums;
using ECommerce.Reviews.Domain.ValueObjects;
using ECommerce.SharedKernel.Results;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ECommerce.Reviews.Tests.Domain.Reviews;

[TestClass]
public class ReviewsDomainTests
{
    [TestMethod]
    [DataRow(0)]
    [DataRow(-1)]
    [DataRow(6)]
    [DataRow(100)]
    public void Rating_Create_OutOfRange_ReturnsFailure(int value)
    {
        Result<Rating> result = Rating.Create(value);

        result.IsSuccess.ShouldBeFalse();
        result.GetErrorOrThrow().Code.ShouldBe(ReviewsErrors.RatingRange.Code);
    }

    [TestMethod]
    [DataRow(1)]
    [DataRow(3)]
    [DataRow(5)]
    public void Rating_Create_Valid_ReturnsSuccess(int value)
    {
        Result<Rating> result = Rating.Create(value);

        result.IsSuccess.ShouldBeTrue();
        result.GetDataOrThrow().Value.ShouldBe(value);
    }

    [TestMethod]
    public void ReviewContent_Create_EmptyText_ReturnsFailure()
    {
        Result<ReviewContent> result = ReviewContent.Create("Nice", "   ");

        result.IsSuccess.ShouldBeFalse();
        result.GetErrorOrThrow().Code.ShouldBe(ReviewsErrors.ReviewBodyEmpty.Code);
    }

    [TestMethod]
    public void ReviewContent_Create_TooShort_ReturnsFailure()
    {
        Result<ReviewContent> result = ReviewContent.Create("Nice", "too short");

        result.IsSuccess.ShouldBeFalse();
        result.GetErrorOrThrow().Code.ShouldBe(ReviewsErrors.ReviewBodyShort.Code);
    }

    [TestMethod]
    public void ReviewContent_Create_TooLong_ReturnsFailure()
    {
        string comment = new('a', 1001);

        Result<ReviewContent> result = ReviewContent.Create("Nice", comment);

        result.IsSuccess.ShouldBeFalse();
        result.GetErrorOrThrow().Code.ShouldBe(ReviewsErrors.ReviewBodyLong.Code);
    }

    [TestMethod]
    public void ReviewContent_Create_Valid_TrimsAndReturnsSuccess()
    {
        Result<ReviewContent> result = ReviewContent.Create("  Nice title  ", "  Great product overall!  ");

        result.IsSuccess.ShouldBeTrue();
        ReviewContent content = result.GetDataOrThrow();
        content.Title.ShouldBe("Nice title");
        content.Body.ShouldBe("Great product overall!");
    }

    [TestMethod]
    public void Review_Create_InitialState_IsPendingWithZeroCounters()
    {
        Review review = CreateReview();

        review.Status.ShouldBe(ReviewStatus.Pending);
        review.HelpfulCount.ShouldBe(0);
        review.FlagCount.ShouldBe(0);
        review.IsVerifiedPurchase.ShouldBeFalse();
    }

    [TestMethod]
    public void Review_Edit_PendingReview_UpdatesValues()
    {
        Review review = CreateReview();
        Rating rating = Rating.Create(4).GetDataOrThrow();
        ReviewContent content = ReviewContent.Create("Updated", "Updated review text").GetDataOrThrow();
        DateTime updatedAt = DateTime.UtcNow.AddMinutes(-5);

        Result result = review.Edit(rating, content, updatedAt);

        result.IsSuccess.ShouldBeTrue();
        review.Rating.Value.ShouldBe(4);
        review.Content.Title.ShouldBe("Updated");
        review.Content.Body.ShouldBe("Updated review text");
        review.UpdatedAt.ShouldBe(updatedAt);
    }

    [TestMethod]
    public void Review_Edit_ApprovedReview_ReturnsFailure()
    {
        Review review = CreateReview();
        review.Approve(DateTime.UtcNow);

        Result result = review.Edit(
            Rating.Create(4).GetDataOrThrow(),
            ReviewContent.Create("Updated", "Updated review text").GetDataOrThrow(),
            DateTime.UtcNow);

        result.IsSuccess.ShouldBeFalse();
        result.GetErrorOrThrow().Code.ShouldBe(ReviewsErrors.ReviewAlreadyApproved.Code);
    }

    [TestMethod]
    public void Review_Approve_PendingReview_Succeeds()
    {
        Review review = CreateReview();
        DateTime updatedAt = DateTime.UtcNow.AddMinutes(-2);

        Result result = review.Approve(updatedAt);

        result.IsSuccess.ShouldBeTrue();
        review.Status.ShouldBe(ReviewStatus.Approved);
        review.UpdatedAt.ShouldBe(updatedAt);
    }

    [TestMethod]
    public void Review_Approve_AlreadyApproved_ReturnsFailure()
    {
        Review review = CreateReview();
        review.Approve(DateTime.UtcNow);

        Result result = review.Approve(DateTime.UtcNow);

        result.IsSuccess.ShouldBeFalse();
        result.GetErrorOrThrow().Code.ShouldBe(ReviewsErrors.ReviewAlreadyApproved.Code);
    }

    [TestMethod]
    public void Review_Reject_SetsRejectedStatus()
    {
        Review review = CreateReview();
        DateTime updatedAt = DateTime.UtcNow.AddMinutes(-1);

        review.Reject(updatedAt);

        review.Status.ShouldBe(ReviewStatus.Rejected);
        review.UpdatedAt.ShouldBe(updatedAt);
    }

    [TestMethod]
    public void Review_Flag_IncrementsCounterAndMarksFlagged()
    {
        Review review = CreateReview();
        DateTime updatedAt = DateTime.UtcNow.AddMinutes(-1);

        review.Flag(updatedAt);

        review.Status.ShouldBe(ReviewStatus.Flagged);
        review.FlagCount.ShouldBe(1);
        review.UpdatedAt.ShouldBe(updatedAt);
    }

    [TestMethod]
    public void Review_MarkAsHelpful_IncrementsCounter()
    {
        Review review = CreateReview();

        review.MarkAsHelpful(DateTime.UtcNow.AddMinutes(-1));
        review.MarkAsHelpful(DateTime.UtcNow);

        review.HelpfulCount.ShouldBe(2);
    }

    [TestMethod]
    public void Review_MarkAsVerifiedPurchase_SetsFlag()
    {
        Review review = CreateReview();

        review.MarkAsVerifiedPurchase();

        review.IsVerifiedPurchase.ShouldBeTrue();
    }

    private static Review CreateReview()
    {
        Guid productId = Guid.NewGuid();
        Guid userId = Guid.NewGuid();
        Rating rating = Rating.Create(5).GetDataOrThrow();
        ReviewContent content = ReviewContent.Create("Great", "Great product overall!").GetDataOrThrow();

        return Review.Create(productId, userId, rating, content, null);
    }
}
