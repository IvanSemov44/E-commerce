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

        result.IsSuccess.Should().BeFalse();
        result.GetErrorOrThrow().Code.Should().Be(ReviewsErrors.RatingRange.Code);
    }

    [TestMethod]
    [DataRow(1)]
    [DataRow(3)]
    [DataRow(5)]
    public void Rating_Create_Valid_ReturnsSuccess(int value)
    {
        Result<Rating> result = Rating.Create(value);

        result.IsSuccess.Should().BeTrue();
        result.GetDataOrThrow().Value.Should().Be(value);
    }

    [TestMethod]
    public void ReviewContent_Create_EmptyText_ReturnsFailure()
    {
        Result<ReviewContent> result = ReviewContent.Create("Nice", "   ");

        result.IsSuccess.Should().BeFalse();
        result.GetErrorOrThrow().Code.Should().Be(ReviewsErrors.ReviewBodyEmpty.Code);
    }

    [TestMethod]
    public void ReviewContent_Create_TooShort_ReturnsFailure()
    {
        Result<ReviewContent> result = ReviewContent.Create("Nice", "too short");

        result.IsSuccess.Should().BeFalse();
        result.GetErrorOrThrow().Code.Should().Be(ReviewsErrors.ReviewBodyShort.Code);
    }

    [TestMethod]
    public void ReviewContent_Create_TooLong_ReturnsFailure()
    {
        string comment = new('a', 1001);

        Result<ReviewContent> result = ReviewContent.Create("Nice", comment);

        result.IsSuccess.Should().BeFalse();
        result.GetErrorOrThrow().Code.Should().Be(ReviewsErrors.ReviewBodyLong.Code);
    }

    [TestMethod]
    public void ReviewContent_Create_Valid_TrimsAndReturnsSuccess()
    {
        Result<ReviewContent> result = ReviewContent.Create("  Nice title  ", "  Great product overall!  ");

        result.IsSuccess.Should().BeTrue();
        ReviewContent content = result.GetDataOrThrow();
        content.Title.Should().Be("Nice title");
        content.Body.Should().Be("Great product overall!");
    }

    [TestMethod]
    public void Review_Create_InitialState_IsPendingWithZeroCounters()
    {
        Review review = CreateReview();

        review.Status.Should().Be(ReviewStatus.Pending);
        review.HelpfulCount.Should().Be(0);
        review.FlagCount.Should().Be(0);
        review.IsVerifiedPurchase.Should().BeFalse();
    }

    [TestMethod]
    public void Review_Edit_PendingReview_UpdatesValues()
    {
        Review review = CreateReview();
        Rating rating = Rating.Create(4).GetDataOrThrow();
        ReviewContent content = ReviewContent.Create("Updated", "Updated review text").GetDataOrThrow();
        DateTime updatedAt = DateTime.UtcNow.AddMinutes(-5);

        Result result = review.Edit(rating, content, updatedAt);

        result.IsSuccess.Should().BeTrue();
        review.Rating.Value.Should().Be(4);
        review.Content.Title.Should().Be("Updated");
        review.Content.Body.Should().Be("Updated review text");
        review.UpdatedAt.Should().Be(updatedAt);
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

        result.IsSuccess.Should().BeFalse();
        result.GetErrorOrThrow().Code.Should().Be(ReviewsErrors.ReviewAlreadyApproved.Code);
    }

    [TestMethod]
    public void Review_Approve_PendingReview_Succeeds()
    {
        Review review = CreateReview();
        DateTime updatedAt = DateTime.UtcNow.AddMinutes(-2);

        Result result = review.Approve(updatedAt);

        result.IsSuccess.Should().BeTrue();
        review.Status.Should().Be(ReviewStatus.Approved);
        review.UpdatedAt.Should().Be(updatedAt);
    }

    [TestMethod]
    public void Review_Approve_AlreadyApproved_ReturnsFailure()
    {
        Review review = CreateReview();
        review.Approve(DateTime.UtcNow);

        Result result = review.Approve(DateTime.UtcNow);

        result.IsSuccess.Should().BeFalse();
        result.GetErrorOrThrow().Code.Should().Be(ReviewsErrors.ReviewAlreadyApproved.Code);
    }

    [TestMethod]
    public void Review_Reject_SetsRejectedStatus()
    {
        Review review = CreateReview();
        DateTime updatedAt = DateTime.UtcNow.AddMinutes(-1);

        review.Reject(updatedAt);

        review.Status.Should().Be(ReviewStatus.Rejected);
        review.UpdatedAt.Should().Be(updatedAt);
    }

    [TestMethod]
    public void Review_Flag_IncrementsCounterAndMarksFlagged()
    {
        Review review = CreateReview();
        DateTime updatedAt = DateTime.UtcNow.AddMinutes(-1);

        review.Flag(updatedAt);

        review.Status.Should().Be(ReviewStatus.Flagged);
        review.FlagCount.Should().Be(1);
        review.UpdatedAt.Should().Be(updatedAt);
    }

    [TestMethod]
    public void Review_MarkAsHelpful_IncrementsCounter()
    {
        Review review = CreateReview();

        review.MarkAsHelpful(DateTime.UtcNow.AddMinutes(-1));
        review.MarkAsHelpful(DateTime.UtcNow);

        review.HelpfulCount.Should().Be(2);
    }

    [TestMethod]
    public void Review_MarkAsVerifiedPurchase_SetsFlag()
    {
        Review review = CreateReview();

        review.MarkAsVerifiedPurchase();

        review.IsVerifiedPurchase.Should().BeTrue();
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
