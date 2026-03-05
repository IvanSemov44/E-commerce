using ECommerce.Application.DTOs.Common;
using ECommerce.Core.Results;

namespace ECommerce.Application.Interfaces;

/// <summary>
/// Service interface for category management operations.
/// </summary>
public interface ICategoryService
{
    Task<Result<PaginatedResult<CategoryDto>>> GetAllCategoriesAsync(
        int pageNumber = 1,
        int pageSize = 100,
        CancellationToken cancellationToken = default);
    
    Task<Result<PaginatedResult<CategoryDto>>> GetTopLevelCategoriesAsync(
        int pageNumber = 1,
        int pageSize = 100,
        CancellationToken cancellationToken = default);
    
    Task<Result<CategoryDetailDto>> GetCategoryByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Result<CategoryDetailDto>> GetCategoryBySlugAsync(string slug, CancellationToken cancellationToken = default);
    Task<Result<CategoryDetailDto>> CreateCategoryAsync(CreateCategoryDto dto, CancellationToken cancellationToken = default);
    Task<Result<CategoryDetailDto>> UpdateCategoryAsync(Guid id, UpdateCategoryDto dto, CancellationToken cancellationToken = default);
    Task<Result<Unit>> DeleteCategoryAsync(Guid id, CancellationToken cancellationToken = default);
}
