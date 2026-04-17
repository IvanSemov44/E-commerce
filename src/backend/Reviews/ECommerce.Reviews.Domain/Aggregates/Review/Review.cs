using ECommerce.Reviews.Domain.Enums;
using ECommerce.Reviews.Domain.Errors;
using ECommerce.Reviews.Domain.Events;
using ECommerce.Reviews.Domain.ValueObjects;
using ECommerce.SharedKernel.Domain;
using ECommerce.SharedKernel.Results;

namespace ECommerce.Reviews.Domain.Aggregates.Review;

public class Review : AggregateRoot
{
    public Guid ProductId { get; private set; }
    public Guid? UserId { get; private set; }
    public Rating Rating { get; private set; } = null!;
    public ReviewContent Content { get; private set; } = null!;
    public ReviewStatus Status { get; private set; }
    public bool IsVerifiedPurchase { get; private set; }
    public int HelpfulCount { get; private set; }
    public int FlagCount { get; private set; }
    public DateTime? DeletedAt { get; private set; }

    private Review()
    {
    }

    private Review(
        Guid productId,
        Guid userId,
        Rating rating,
        ReviewContent content)
    {
        ProductId = productId;
        UserId = userId;
        Rating = rating;
        Content = content;
        Status = ReviewStatus.Pending;
        IsVerifiedPurchase = false;
        HelpfulCount = 0;
        FlagCount = 0;
    }

    public static Review Create(
        Guid productId,
        Guid userId,
        Rating rating,
        ReviewContent content)
    {
        var review = new Review(productId, userId, rating, content);
        review.AddDomainEvent(new ReviewRatingProjectionChangedDomainEvent(review.ProductId));
        return review;
    }

    public Result Edit(int? rating, string? title, string? comment)
    {
        if (Status == ReviewStatus.Approved)
            return Result.Fail(ReviewsErrors.ReviewAlreadyApproved);

        if (rating.HasValue)
        {
            var ratingResult = Rating.Create(rating.Value);
            if (!ratingResult.IsSuccess)
                return Result.Fail(ratingResult.GetErrorOrThrow());
            Rating = ratingResult.GetDataOrThrow();
        }

        string resolvedTitle = string.IsNullOrWhiteSpace(title) ? Content.Title ?? string.Empty : title;
        string resolvedBody  = string.IsNullOrWhiteSpace(comment) ? Content.Body : comment;
        var contentResult = ReviewContent.Create(resolvedTitle, resolvedBody);
        if (!contentResult.IsSuccess)
            return Result.Fail(contentResult.GetErrorOrThrow());

        Content = contentResult.GetDataOrThrow();
        UpdatedAt = DateTime.UtcNow;
        AddDomainEvent(new ReviewRatingProjectionChangedDomainEvent(ProductId));
        return Result.Ok();
    }

    public Result Approve()
    {
        if (Status == ReviewStatus.Approved)
            return Result.Fail(ReviewsErrors.ReviewAlreadyApproved);

        Status = ReviewStatus.Approved;
        UpdatedAt = DateTime.UtcNow;
        AddDomainEvent(new ReviewRatingProjectionChangedDomainEvent(ProductId));
        return Result.Ok();
    }

    public void Reject()
    {
        // Only approved reviews affect the rating projection — rejecting a pending review is a no-op for ratings.
        bool wasApproved = Status == ReviewStatus.Approved;
        Status = ReviewStatus.Rejected;
        UpdatedAt = DateTime.UtcNow;
        if (wasApproved)
            AddDomainEvent(new ReviewRatingProjectionChangedDomainEvent(ProductId));
    }

    public void Delete()
    {
        DeletedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
        // Raise rating event so consumers recompute the average after this review is soft-deleted.
        AddDomainEvent(new ReviewRatingProjectionChangedDomainEvent(ProductId));
    }

    public void Flag()
    {
        Status = ReviewStatus.Flagged;
        FlagCount++;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkAsHelpful()
    {
        HelpfulCount++;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkAsVerifiedPurchase()
    {
        IsVerifiedPurchase = true;
        UpdatedAt = DateTime.UtcNow;
    }
}
