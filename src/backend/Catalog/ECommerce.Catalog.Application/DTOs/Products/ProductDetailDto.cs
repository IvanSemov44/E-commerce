using System;
using System.Collections.Generic;
namespace ECommerce.Catalog.Application.DTOs.Products;

public class ProductDetailDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
    public decimal Price { get; init; }
    public string Currency { get; init; } = string.Empty;
    public decimal? CompareAtPrice { get; init; }
    public string? Sku { get; init; }
    public string? Description { get; init; }
    public string Status { get; init; } = string.Empty;
    public bool IsFeatured { get; init; }
    public Guid CategoryId { get; init; }
    public string CategoryName { get; init; } = string.Empty;
    public IReadOnlyList<ProductImageDto> Images { get; init; } = Array.Empty<ProductImageDto>();
}

public class ProductImageDto
{
    public Guid Id { get; init; }
    public string Url { get; init; } = string.Empty;
    public string? AltText { get; init; }
    public bool IsPrimary { get; init; }
    public int DisplayOrder { get; init; }
}
