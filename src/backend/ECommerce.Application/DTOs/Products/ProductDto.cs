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
    public CategoryDto? Category { get; set; }
    public decimal AverageRating { get; set; }
    public int ReviewCount { get; set; }
}

public class ProductDetailDto : ProductDto
{
    public string? Description { get; set; }
    public string? Sku { get; set; }
    public int LowStockThreshold { get; set; }
    public bool IsActive { get; set; }
    public List<ReviewDto> Reviews { get; set; } = new();
}

public class ProductImageDto
{
    public Guid Id { get; set; }
    public string Url { get; set; } = null!;
    public string? AltText { get; set; }
    public bool IsPrimary { get; set; }
}

public class CategoryDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string Slug { get; set; } = null!;
    public string? ImageUrl { get; set; }
}

public class ReviewDto
{
    public Guid Id { get; set; }
    public string? Title { get; set; }
    public string? Comment { get; set; }
    public int Rating { get; set; }
    public string? UserName { get; set; }
    public DateTime CreatedAt { get; set; }
}
