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

public record CategoryDetailDto : CategoryDto
{
    public CategoryDto? Parent { get; init; }
    public List<CategoryDto> Children { get; init; } = new();
}

public class CreateCategoryDto
{
    public string Name { get; set; } = null!;
    public string Slug { get; set; } = null!;
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public Guid? ParentId { get; set; }
}

public class UpdateCategoryDto
{
    public string? Name { get; set; }
    public string? Slug { get; set; }
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public Guid? ParentId { get; set; }
    public bool? IsActive { get; set; }
}
