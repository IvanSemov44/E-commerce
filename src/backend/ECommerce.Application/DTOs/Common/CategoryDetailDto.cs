namespace ECommerce.Application.DTOs.Common;

public record CategoryDetailDto : CategoryDto
{
    public CategoryDto? Parent { get; init; }
    public List<CategoryDto> Children { get; init; } = new();
}
