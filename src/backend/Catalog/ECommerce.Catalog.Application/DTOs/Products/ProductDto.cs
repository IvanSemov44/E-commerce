using System;
using System.Collections.Generic;
namespace ECommerce.Catalog.Application.DTOs.Products;

public class ProductDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
    public decimal Price { get; init; }
    public string Currency { get; init; } = string.Empty;
    public decimal? CompareAtPrice { get; init; }
    public string? Description { get; init; }
    public IReadOnlyList<ProductImageDto> Images { get; init; } = Array.Empty<ProductImageDto>();
    public int StockQuantity { get; init; }
    public bool IsFeatured { get; init; }
    public Guid CategoryId { get; init; }
    public string CategoryName { get; init; } = string.Empty;
    public decimal AverageRating { get; init; }
    public int ReviewCount { get; init; }
    public bool IsActive { get; init; }
}
