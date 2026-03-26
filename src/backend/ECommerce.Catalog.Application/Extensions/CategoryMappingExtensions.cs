using ECommerce.Catalog.Application.DTOs.Categories;
using ECommerce.Catalog.Domain.Aggregates.Category;

namespace ECommerce.Catalog.Application.Extensions;

public static class CategoryMappingExtensions
{
    public static CategoryDto ToDto(this Category category) => new()
    {
        Id = category.Id,
        Name = category.Name.Value,
        Slug = category.Slug.Value,
        ParentId = category.ParentId,
        IsActive = category.IsActive,
    };
}
