using System;
namespace ECommerce.Catalog.Application.DTOs.Categories;

public class CategoryDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
    public Guid? ParentId { get; init; }
    public bool IsActive { get; init; }
}
