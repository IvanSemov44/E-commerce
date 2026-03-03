using ECommerce.Application.DTOs.Common;

namespace ECommerce.Application.Interfaces;

/// <summary>
/// Service interface for category management operations.
/// </summary>
public interface ICategoryService
{
    Task<PaginatedResult<CategoryDto>> GetAllCategoriesAsync(
        int pageNumber = 1,
        int pageSize = 100,
        CancellationToken cancellationToken = default);
    
    Task<PaginatedResult<CategoryDto>> GetTopLevelCategoriesAsync(
        int pageNumber = 1,
        int pageSize = 100,
        CancellationToken cancellationToken = default);
    
    Task<CategoryDetailDto> GetCategoryByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<CategoryDetailDto> GetCategoryBySlugAsync(string slug, CancellationToken cancellationToken = default);
    Task<CategoryDetailDto> CreateCategoryAsync(CreateCategoryDto dto, CancellationToken cancellationToken = default);
    Task<CategoryDetailDto> UpdateCategoryAsync(Guid id, UpdateCategoryDto dto, CancellationToken cancellationToken = default);
    Task DeleteCategoryAsync(Guid id, CancellationToken cancellationToken = default);
}
