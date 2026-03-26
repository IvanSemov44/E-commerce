using System;
namespace ECommerce.Catalog.Application.DTOs.Products;

public class ProductDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
    public decimal Price { get; init; }
    public string Currency { get; init; } = string.Empty;
    public string? PrimaryImageUrl { get; init; }
    public string CategoryName { get; init; } = string.Empty;
    public bool IsActive { get; init; }
}
