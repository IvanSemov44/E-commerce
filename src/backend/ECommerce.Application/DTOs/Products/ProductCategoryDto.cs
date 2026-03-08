namespace ECommerce.Application.DTOs.Products;

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
