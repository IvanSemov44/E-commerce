using ECommerce.API.ActionFilters;
using ECommerce.Application.DTOs.Common;
using ECommerce.Application.Interfaces;
using ECommerce.Core.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ECommerce.Core.Results;

namespace ECommerce.API.Controllers;

/// <summary>
/// Controller for category management and retrieval operations.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class CategoriesController : ControllerBase
{
    private readonly ICategoryService _categoryService;
    private readonly ILogger<CategoriesController> _logger;

    public CategoriesController(ICategoryService categoryService, ILogger<CategoriesController> logger)
    {
        _categoryService = categoryService;
        _logger = logger;
    }

    /// <summary>
    /// Retrieves all categories in a hierarchical structure with pagination.
    /// </summary>
    /// <param name="pageNumber">The page number (default: 1).</param>
    /// <param name="pageSize">The page size (default: 100).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A paginated list of categories including parent and child relationships.</returns>
    /// <response code="200">Categories retrieved successfully.</response>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResult<CategoryDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllCategories(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 100,
        CancellationToken cancellationToken = default)
    {
        // Enforce bounds to prevent malicious requests
        if (pageNumber < PaginationConstants.MinPageNumber) pageNumber = PaginationConstants.MinPageNumber;
        if (pageSize < PaginationConstants.MinPageSize || pageSize > PaginationConstants.MaxPageSize) pageSize = PaginationConstants.MaxPageSize;
        
        var categories = await _categoryService.GetAllCategoriesAsync(pageNumber, pageSize, cancellationToken: cancellationToken);
        return Ok(ApiResponse<PaginatedResult<CategoryDto>>.Ok(categories, "Categories retrieved successfully"));
    }

    /// <summary>
    /// Retrieves all top-level categories (categories without a parent) with pagination.
    /// </summary>
    /// <param name="pageNumber">The page number (default: 1).</param>
    /// <param name="pageSize">The page size (default: 100).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A paginated list of root-level categories.</returns>
    /// <response code="200">Top-level categories retrieved successfully.</response>
    [HttpGet("top-level")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResult<CategoryDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTopLevelCategories(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 100,
        CancellationToken cancellationToken = default)
    {
        // Enforce bounds to prevent malicious requests
        if (pageNumber < PaginationConstants.MinPageNumber) pageNumber = PaginationConstants.MinPageNumber;
        if (pageSize < PaginationConstants.MinPageSize || pageSize > PaginationConstants.MaxPageSize) pageSize = PaginationConstants.MaxPageSize;
        
        var categories = await _categoryService.GetTopLevelCategoriesAsync(pageNumber, pageSize, cancellationToken: cancellationToken);
        return Ok(ApiResponse<PaginatedResult<CategoryDto>>.Ok(categories, "Top-level categories retrieved successfully"));
    }

    /// <summary>
    /// Retrieves a category by its ID with detailed information including subcategories.
    /// </summary>
    /// <param name="id">The category ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The category details.</returns>
    /// <response code="200">Category retrieved successfully.</response>
    /// <response code="404">Category not found.</response>
    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<CategoryDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCategoryById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _categoryService.GetCategoryByIdAsync(id, cancellationToken: cancellationToken);
        if (result is Result<CategoryDetailDto>.Success success)
            return Ok(ApiResponse<CategoryDetailDto>.Ok(success.Data, "Category retrieved successfully"));
        if (result is Result<CategoryDetailDto>.Failure)
            return NotFound(ApiResponse<object>.Error("Category not found"));
        return StatusCode(500, ApiResponse<object>.Error("Unknown error occurred"));
    }

    /// <summary>
    /// Retrieves a category by its URL-friendly slug.
    /// </summary>
    /// <param name="slug">The category slug.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The category details.</returns>
    /// <response code="200">Category retrieved successfully.</response>
    /// <response code="404">Category not found.</response>
    [HttpGet("slug/{slug}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<CategoryDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCategoryBySlug(string slug, CancellationToken cancellationToken)
    {
        var result = await _categoryService.GetCategoryBySlugAsync(slug, cancellationToken: cancellationToken);
        if (result is Result<CategoryDetailDto>.Success success)
            return Ok(ApiResponse<CategoryDetailDto>.Ok(success.Data, "Category retrieved successfully"));
        if (result is Result<CategoryDetailDto>.Failure)
            return NotFound(ApiResponse<object>.Error("Category not found"));
        return StatusCode(500, ApiResponse<object>.Error("Unknown error occurred"));
    }

    /// <summary>
    /// Creates a new category (admin only).
    /// </summary>
    /// <param name="dto">The category creation details.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The newly created category.</returns>
    /// <response code="201">Category created successfully.</response>
    /// <response code="400">Invalid category data.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="403">User does not have permission to create categories.</response>
    /// <response code="409">Category with the same slug already exists.</response>
    [HttpPost]
    [Authorize(Roles = "Admin,SuperAdmin")]
    [ValidationFilter]
    [ProducesResponseType(typeof(ApiResponse<CategoryDetailDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateCategory([FromBody] CreateCategoryDto dto, CancellationToken cancellationToken)
    {
        var result = await _categoryService.CreateCategoryAsync(dto, cancellationToken: cancellationToken);
        if (result is Result<CategoryDetailDto>.Success success)
        {
            _logger.LogInformation("Category created: {CategoryId}", success.Data.Id);
            return CreatedAtAction(nameof(GetCategoryById), new { id = success.Data.Id },
                ApiResponse<CategoryDetailDto>.Ok(success.Data, "Category created successfully"));
        }
        if (result is Result<CategoryDetailDto>.Failure failure)
            return Conflict(ApiResponse<object>.Error(failure.Message));
        return StatusCode(500, ApiResponse<object>.Error("Unknown error occurred"));
    }

    /// <summary>
    /// Updates an existing category (admin only).
    /// </summary>
    /// <param name="id">The category ID.</param>
    /// <param name="dto">The updated category details.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated category.</returns>
    /// <response code="200">Category updated successfully.</response>
    /// <response code="400">Invalid category data.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="403">User does not have permission to update categories.</response>
    /// <response code="404">Category not found.</response>
    /// <response code="409">Category with the same slug already exists.</response>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    [ValidationFilter]
    [ProducesResponseType(typeof(ApiResponse<CategoryDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateCategory(Guid id, [FromBody] UpdateCategoryDto dto, CancellationToken cancellationToken)
    {
        var result = await _categoryService.UpdateCategoryAsync(id, dto, cancellationToken: cancellationToken);
        if (result is Result<CategoryDetailDto>.Success success)
        {
            _logger.LogInformation("Category updated: {CategoryId}", id);
            return Ok(ApiResponse<CategoryDetailDto>.Ok(success.Data, "Category updated successfully"));
        }
        if (result is Result<CategoryDetailDto>.Failure failure)
        {
            var statusCode = failure.Code switch
            {
                "DUPLICATE_CATEGORY_SLUG" => StatusCodes.Status409Conflict,
                "CATEGORY_NOT_FOUND" => StatusCodes.Status404NotFound,
                _ => StatusCodes.Status400BadRequest
            };
            return StatusCode(statusCode, ApiResponse<object>.Error(failure.Message));
        }
        return StatusCode(500, ApiResponse<object>.Error("Unknown error occurred"));
    }

    /// <summary>
    /// Deletes a category (admin only). Categories with products or subcategories cannot be deleted.
    /// </summary>
    /// <param name="id">The category ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Deletion result.</returns>
    /// <response code="200">Category deleted successfully.</response>
    /// <response code="400">Category cannot be deleted because it has products or subcategories.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="403">User does not have permission to delete categories.</response>
    /// <response code="404">Category not found.</response>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DeleteCategory(Guid id, CancellationToken cancellationToken)
    {
        var result = await _categoryService.DeleteCategoryAsync(id, cancellationToken: cancellationToken);
        if (result is Result<ECommerce.Core.Results.Unit>.Success)
        {
            _logger.LogInformation("Category deleted: {CategoryId}", id);
            return Ok(ApiResponse<object>.Ok(new object(), "Category deleted successfully"));
        }
        if (result is Result<ECommerce.Core.Results.Unit>.Failure failure)
        {
            var statusCode = failure.Code switch
            {
                "CATEGORY_HAS_PRODUCTS" => StatusCodes.Status400BadRequest,
                "CATEGORY_NOT_FOUND" => StatusCodes.Status404NotFound,
                _ => StatusCodes.Status400BadRequest
            };
            return StatusCode(statusCode, ApiResponse<object>.Error(failure.Message));
        }
        return StatusCode(500, ApiResponse<object>.Error("Unknown error occurred"));
        _logger.LogInformation("Category deleted: {CategoryId}", id);
        return Ok(ApiResponse<object>.Ok(new object(), "Category deleted successfully"));
    }
}

