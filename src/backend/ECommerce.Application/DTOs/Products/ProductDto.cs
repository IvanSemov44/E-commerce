namespace ECommerce.Application.DTOs.Products;

public class ProductDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string Slug { get; set; } = null!;
    public string? ShortDescription { get; set; }
    public decimal Price { get; set; }
    public decimal? CompareAtPrice { get; set; }
    public int StockQuantity { get; set; }
    public bool IsFeatured { get; set; }
    public List<ProductImageDto> Images { get; set; } = new();
    public ProductCategoryDto? Category { get; set; }
    public decimal AverageRating { get; set; }
    public int ReviewCount { get; set; }
}

public class ProductDetailDto : ProductDto
{
    public string? Description { get; set; }
    public string? Sku { get; set; }
    public int LowStockThreshold { get; set; }
    public bool IsActive { get; set; }
    public List<ProductReviewDto> Reviews { get; set; } = new();
}

public class ProductImageDto
{
    public Guid Id { get; set; }
    public string Url { get; set; } = null!;
    public string? AltText { get; set; }
    public bool IsPrimary { get; set; }
}

/// <summary>
/// Simplified category DTO for embedding in product responses.
/// For full category details, use DTOs.CategoryDto.
/// </summary>
public class ProductCategoryDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string Slug { get; set; } = null!;
    public string? ImageUrl { get; set; }
}

/// <summary>
/// Simplified review DTO for embedding in product detail responses.
/// For full review operations, use DTOs.Reviews.ReviewDetailDto.
/// </summary>
public class ProductReviewDto
{
    public Guid Id { get; set; }
    public string? Title { get; set; }
    public string? Comment { get; set; }
    public int Rating { get; set; }
    public string? UserName { get; set; }
    public DateTime CreatedAt { get; set; }
}
