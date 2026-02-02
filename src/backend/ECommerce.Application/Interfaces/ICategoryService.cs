using ECommerce.Application.DTOs.Common;

namespace ECommerce.Application.Interfaces;

/// <summary>
/// Service interface for category management operations.
/// </summary>
public interface ICategoryService
{
    Task<IEnumerable<CategoryDto>> GetAllCategoriesAsync();
    Task<IEnumerable<CategoryDto>> GetTopLevelCategoriesAsync();
    Task<CategoryDetailDto> GetCategoryByIdAsync(Guid id);
    Task<CategoryDetailDto> GetCategoryBySlugAsync(string slug);
    Task<CategoryDetailDto> CreateCategoryAsync(CreateCategoryDto dto);
    Task<CategoryDetailDto> UpdateCategoryAsync(Guid id, UpdateCategoryDto dto);
    Task DeleteCategoryAsync(Guid id);
}
