namespace ECommerce.Contracts.DTOs.Products;

/// <summary>
/// Simplified review DTO for embedding in product detail responses.
/// For full review operations, use DTOs.Reviews.ReviewDetailDto.
/// </summary>
public record ProductReviewDto
{
    public Guid Id { get; init; }
    public string? Title { get; init; }
    public string? Comment { get; init; }
    public int Rating { get; init; }
    public string? UserName { get; init; }
    public DateTime CreatedAt { get; init; }
}

