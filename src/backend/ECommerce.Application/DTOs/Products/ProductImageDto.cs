namespace ECommerce.Application.DTOs.Products;

public record ProductImageDto
{
    public Guid Id { get; init; }
    public string Url { get; init; } = null!;
    public string? AltText { get; init; }
    public bool IsPrimary { get; init; }
}
