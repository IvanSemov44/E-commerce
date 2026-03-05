namespace ECommerce.Application.DTOs.Common;

public record CategoryDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = null!;
    public string Slug { get; init; } = null!;
    public string? Description { get; init; }
    public string? ImageUrl { get; init; }
    public Guid? ParentId { get; init; }
    public int ProductCount { get; init; }
    public bool IsActive { get; init; }
}
