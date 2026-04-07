namespace ECommerce.Contracts.DTOs.Products;

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

