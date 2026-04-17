using ECommerce.Reviews.Domain.Aggregates.Review;
using ECommerce.Reviews.Domain.Events;
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
    public void Review_Create_CollectsRatingProjectionDomainEvent()
    {
        Review review = CreateReview();

        review.DomainEvents.OfType<ReviewRatingProjectionChangedDomainEvent>().ShouldHaveSingleItem();
    }

    [TestMethod]
    public void Review_Edit_PendingReview_UpdatesValues()
    {
        Review review = CreateReview();

        Result result = review.Edit(4, "Updated", "Updated review text");

        result.IsSuccess.ShouldBeTrue();
        review.Rating.Value.ShouldBe(4);
        review.Content.Title.ShouldBe("Updated");
        review.Content.Body.ShouldBe("Updated review text");
    }

    [TestMethod]
    public void Review_Edit_ApprovedReview_ReturnsFailure()
    {
        Review review = CreateReview();
        review.Approve();

        Result result = review.Edit(4, "Updated", "Updated review text");

        result.IsSuccess.ShouldBeFalse();
        result.GetErrorOrThrow().Code.ShouldBe(ReviewsErrors.ReviewAlreadyApproved.Code);
    }

    [TestMethod]
    public void Review_Approve_PendingReview_Succeeds()
    {
        Review review = CreateReview();

        Result result = review.Approve();

        result.IsSuccess.ShouldBeTrue();
        review.Status.ShouldBe(ReviewStatus.Approved);
    }

    [TestMethod]
    public void Review_Approve_AlreadyApproved_ReturnsFailure()
    {
        Review review = CreateReview();
        review.Approve();

        Result result = review.Approve();

        result.IsSuccess.ShouldBeFalse();
        result.GetErrorOrThrow().Code.ShouldBe(ReviewsErrors.ReviewAlreadyApproved.Code);
    }

    [TestMethod]
    public void Review_Reject_SetsRejectedStatus()
    {
        Review review = CreateReview();

        review.Reject();

        review.Status.ShouldBe(ReviewStatus.Rejected);
    }

    [TestMethod]
    public void Review_Approve_CollectsNewRatingProjectionDomainEvent()
    {
        Review review = CreateReview();
        review.ClearDomainEvents();

        Result result = review.Approve();

        result.IsSuccess.ShouldBeTrue();
        review.DomainEvents.OfType<ReviewRatingProjectionChangedDomainEvent>().ShouldHaveSingleItem();
    }

    [TestMethod]
    public void Review_Flag_IncrementsCounterAndMarksFlagged()
    {
        Review review = CreateReview();

        review.Flag();

        review.Status.ShouldBe(ReviewStatus.Flagged);
        review.FlagCount.ShouldBe(1);
    }

    [TestMethod]
    public void Review_MarkAsHelpful_IncrementsCounter()
    {
        Review review = CreateReview();

        review.MarkAsHelpful();
        review.MarkAsHelpful();

        review.HelpfulCount.ShouldBe(2);
    }

    [TestMethod]
    public void Review_MarkAsVerifiedPurchase_SetsFlag()
    {
        Review review = CreateReview();

        review.MarkAsVerifiedPurchase();

        review.IsVerifiedPurchase.ShouldBeTrue();
    }

    [TestMethod]
    public void Review_Delete_SetsSoftDeleteAndRaisesRatingEvent()
    {
        Review review = CreateReview();
        review.ClearDomainEvents();

        review.Delete();

        review.DeletedAt.ShouldNotBeNull();
        review.DomainEvents.OfType<ReviewRatingProjectionChangedDomainEvent>().ShouldHaveSingleItem();
    }

    [TestMethod]
    public void Review_Edit_NullRating_KeepsExistingRating()
    {
        Review review = CreateReview();
        int originalRating = review.Rating.Value;

        Result result = review.Edit(null, "Updated", "Updated review text here");

        result.IsSuccess.ShouldBeTrue();
        review.Rating.Value.ShouldBe(originalRating);
    }

    [TestMethod]
    public void Review_Edit_NullComment_KeepsExistingComment()
    {
        Review review = CreateReview();
        string originalBody = review.Content.Body;

        Result result = review.Edit(4, "Updated title", null);

        result.IsSuccess.ShouldBeTrue();
        review.Content.Body.ShouldBe(originalBody);
    }

    [TestMethod]
    public void Review_Edit_RaisesRatingProjectionDomainEvent()
    {
        Review review = CreateReview();
        review.ClearDomainEvents();

        review.Edit(4, "Updated", "Updated review text here");

        review.DomainEvents.OfType<ReviewRatingProjectionChangedDomainEvent>().ShouldHaveSingleItem();
    }

    [TestMethod]
    public void Review_Edit_InvalidRating_ReturnsFailure()
    {
        Review review = CreateReview();

        Result result = review.Edit(0, "Updated", "Updated review text here");

        result.IsSuccess.ShouldBeFalse();
        result.GetErrorOrThrow().Code.ShouldBe(ReviewsErrors.RatingRange.Code);
    }

    [TestMethod]
    public void Review_Reject_WasApproved_RaisesRatingProjectionDomainEvent()
    {
        Review review = CreateReview();
        review.Approve();
        review.ClearDomainEvents();

        review.Reject();

        review.DomainEvents.OfType<ReviewRatingProjectionChangedDomainEvent>().ShouldHaveSingleItem();
    }

    [TestMethod]
    public void Review_Reject_WasPending_DoesNotRaiseRatingProjectionDomainEvent()
    {
        Review review = CreateReview();
        review.ClearDomainEvents();

        review.Reject();

        review.DomainEvents.OfType<ReviewRatingProjectionChangedDomainEvent>().ShouldBeEmpty();
    }

    [TestMethod]
    public void ReviewContent_Create_NullTitle_Succeeds()
    {
        Result<ReviewContent> result = ReviewContent.Create(null, "Great product overall!");

        result.IsSuccess.ShouldBeTrue();
        result.GetDataOrThrow().Title.ShouldBeNull();
    }

    private static Review CreateReview()
    {
        Guid productId = Guid.NewGuid();
        Guid userId = Guid.NewGuid();
        Rating rating = Rating.Create(5).GetDataOrThrow();
        ReviewContent content = ReviewContent.Create("Great", "Great product overall!").GetDataOrThrow();

        return Review.Create(productId, userId, rating, content);
    }
}
