namespace ECommerce.Application.DTOs.Products;

public record ProductDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = null!;
    public string Slug { get; init; } = null!;
    public string? ShortDescription { get; init; }
    public decimal Price { get; init; }
    public decimal? CompareAtPrice { get; init; }
    public int StockQuantity { get; init; }
    public bool IsFeatured { get; init; }
    public List<ProductImageDto> Images { get; init; } = new();
    public ProductCategoryDto? Category { get; init; }
    public decimal AverageRating { get; init; }
    public int ReviewCount { get; init; }
}

public record ProductDetailDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = null!;
    public string Slug { get; init; } = null!;
    public string? ShortDescription { get; init; }
    public decimal Price { get; init; }
    public decimal? CompareAtPrice { get; init; }
    public int StockQuantity { get; init; }
    public bool IsFeatured { get; init; }
    public List<ProductImageDto> Images { get; init; } = new();
    public ProductCategoryDto? Category { get; init; }
    public decimal AverageRating { get; init; }
    public int ReviewCount { get; init; }
    public string? Description { get; init; }
    public string? Sku { get; init; }
    public int LowStockThreshold { get; init; }
    public bool IsActive { get; init; }
    public List<ProductReviewDto> Reviews { get; init; } = new();
}

public record ProductImageDto
{
    public Guid Id { get; init; }
    public string Url { get; init; } = null!;
    public string? AltText { get; init; }
    public bool IsPrimary { get; init; }
}

/// <summary>
/// Simplified category DTO for embedding in product responses.
/// For full category details, use DTOs.CategoryDto.
/// </summary>
public record ProductCategoryDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = null!;
    public string Slug { get; init; } = null!;
    public string? ImageUrl { get; init; }
}

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
