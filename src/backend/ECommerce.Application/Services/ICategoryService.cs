using ECommerce.Application.DTOs;

namespace ECommerce.Application.Services;

public interface ICategoryService
{
    Task<IEnumerable<CategoryDto>> GetAllCategoriesAsync();
    Task<IEnumerable<CategoryDto>> GetTopLevelCategoriesAsync();
    Task<CategoryDetailDto?> GetCategoryByIdAsync(Guid id);
    Task<CategoryDetailDto?> GetCategoryBySlugAsync(string slug);
    Task<CategoryDetailDto> CreateCategoryAsync(CreateCategoryDto dto);
    Task<CategoryDetailDto> UpdateCategoryAsync(Guid id, UpdateCategoryDto dto);
    Task<bool> DeleteCategoryAsync(Guid id);
}
