namespace ECommerce.Reviews.Application.DTOs;

public static class ReviewMappingExtensions
{
    public static ReviewDto ToDto(this Review review) => new()
    {
        Id = review.Id,
        Title = review.Content.Title,
        Comment = review.Content.Body,
        Rating = review.Rating.Value,
        UserName = null,
        CreatedAt = review.CreatedAt
    };

    public static ReviewDetailDto ToDetailDto(this Review review) => new()
    {
        Id = review.Id,
        UserId = review.UserId,
        ProductId = review.ProductId,
        Title = review.Content.Title,
        Comment = review.Content.Body,
        Rating = review.Rating.Value,
        UserName = null,
        IsVerified = review.IsVerifiedPurchase,
        IsApproved = review.Status == ReviewStatus.Approved,
        CreatedAt = review.CreatedAt,
        UpdatedAt = review.UpdatedAt
    };
}
