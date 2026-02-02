using ECommerce.Application.DTOs.Common;

namespace ECommerce.Application.Interfaces;

/// <summary>
/// Service interface for category management operations.
/// </summary>
public interface ICategoryService
{
    Task<IEnumerable<CategoryDto>> GetAllCategoriesAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<CategoryDto>> GetTopLevelCategoriesAsync(CancellationToken cancellationToken = default);
    Task<CategoryDetailDto> GetCategoryByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<CategoryDetailDto> GetCategoryBySlugAsync(string slug, CancellationToken cancellationToken = default);
    Task<CategoryDetailDto> CreateCategoryAsync(CreateCategoryDto dto, CancellationToken cancellationToken = default);
    Task<CategoryDetailDto> UpdateCategoryAsync(Guid id, UpdateCategoryDto dto, CancellationToken cancellationToken = default);
    Task DeleteCategoryAsync(Guid id, CancellationToken cancellationToken = default);
}
