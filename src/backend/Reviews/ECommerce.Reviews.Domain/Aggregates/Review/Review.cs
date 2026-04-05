using ECommerce.Reviews.Domain.Enums;
using ECommerce.Reviews.Domain.Errors;
using ECommerce.Reviews.Domain.ValueObjects;
using ECommerce.SharedKernel.Domain;
using ECommerce.SharedKernel.Results;

namespace ECommerce.Reviews.Domain.Aggregates.Review;

public class Review : AggregateRoot
{
    public Guid ProductId { get; private set; }
    public Guid? UserId { get; private set; }
    public Guid? OrderId { get; private set; }
    public Rating Rating { get; private set; } = null!;
    public ReviewContent Content { get; private set; } = null!;
    public ReviewStatus Status { get; private set; }
    public bool IsVerifiedPurchase { get; private set; }
    public int HelpfulCount { get; private set; }
    public int FlagCount { get; private set; }

    private Review()
    {
    }

    private Review(
        Guid productId,
        Guid userId,
        Rating rating,
        ReviewContent content,
        Guid? orderId)
    {
        ProductId = productId;
        UserId = userId;
        OrderId = orderId;
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
        ReviewContent content,
        Guid? orderId = null)
        => new(productId, userId, rating, content, orderId);

    public Result Edit(Rating rating, ReviewContent content, DateTime updatedAt)
    {
        if (Status == ReviewStatus.Approved)
            return Result.Fail(ReviewsErrors.ReviewAlreadyApproved);

        Rating = rating;
        Content = content;
        UpdatedAt = updatedAt;
        return Result.Ok();
    }

    public Result Approve(DateTime updatedAt)
    {
        if (Status == ReviewStatus.Approved)
            return Result.Fail(ReviewsErrors.ReviewAlreadyApproved);

        Status = ReviewStatus.Approved;
        UpdatedAt = updatedAt;
        return Result.Ok();
    }

    public void Reject(DateTime updatedAt)
    {
        Status = ReviewStatus.Rejected;
        UpdatedAt = updatedAt;
    }

    public void Flag(DateTime updatedAt)
    {
        Status = ReviewStatus.Flagged;
        FlagCount++;
        UpdatedAt = updatedAt;
    }

    public void MarkAsHelpful(DateTime updatedAt)
    {
        HelpfulCount++;
        UpdatedAt = updatedAt;
    }

    public void MarkAsVerifiedPurchase()
    {
        IsVerifiedPurchase = true;
        UpdatedAt = DateTime.UtcNow;
    }
}
