using ECommerce.API.ActionFilters;
using ECommerce.Application.DTOs.Common;
using ECommerce.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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
    /// Retrieves all categories in a hierarchical structure.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of all categories including parent and child relationships.</returns>
    /// <response code="200">Categories retrieved successfully.</response>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<CategoryDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllCategories(CancellationToken cancellationToken)
    {
        var categories = await _categoryService.GetAllCategoriesAsync(cancellationToken: cancellationToken);
        return Ok(ApiResponse<IEnumerable<CategoryDto>>.Ok(categories, "Categories retrieved successfully"));
    }

    /// <summary>
    /// Retrieves all top-level categories (categories without a parent).
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of root-level categories.</returns>
    /// <response code="200">Top-level categories retrieved successfully.</response>
    [HttpGet("top-level")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<CategoryDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTopLevelCategories(CancellationToken cancellationToken)
    {
        var categories = await _categoryService.GetTopLevelCategoriesAsync(cancellationToken: cancellationToken);
        return Ok(ApiResponse<IEnumerable<CategoryDto>>.Ok(categories, "Top-level categories retrieved successfully"));
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
    [ProducesResponseType(typeof(ErrorDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCategoryById(Guid id, CancellationToken cancellationToken)
    {
        var category = await _categoryService.GetCategoryByIdAsync(id, cancellationToken: cancellationToken);
        return Ok(ApiResponse<CategoryDetailDto>.Ok(category, "Category retrieved successfully"));
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
    [ProducesResponseType(typeof(ErrorDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCategoryBySlug(string slug, CancellationToken cancellationToken)
    {
        var category = await _categoryService.GetCategoryBySlugAsync(slug, cancellationToken: cancellationToken);
        return Ok(ApiResponse<CategoryDetailDto>.Ok(category, "Category retrieved successfully"));
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
    [ProducesResponseType(typeof(ErrorDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateCategory([FromBody] CreateCategoryDto dto, CancellationToken cancellationToken)
    {
        var category = await _categoryService.CreateCategoryAsync(dto, cancellationToken: cancellationToken);
        _logger.LogInformation("Category created: {CategoryId}", category.Id);
        return CreatedAtAction(nameof(GetCategoryById), new { id = category.Id },
            ApiResponse<CategoryDetailDto>.Ok(category, "Category created successfully"));
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
    [ProducesResponseType(typeof(ErrorDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateCategory(Guid id, [FromBody] UpdateCategoryDto dto, CancellationToken cancellationToken)
    {
        var category = await _categoryService.UpdateCategoryAsync(id, dto, cancellationToken: cancellationToken);
        _logger.LogInformation("Category updated: {CategoryId}", id);
        return Ok(ApiResponse<CategoryDetailDto>.Ok(category, "Category updated successfully"));
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
    [ProducesResponseType(typeof(ErrorDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DeleteCategory(Guid id, CancellationToken cancellationToken)
    {
        await _categoryService.DeleteCategoryAsync(id, cancellationToken: cancellationToken);
        _logger.LogInformation("Category deleted: {CategoryId}", id);
        return Ok(ApiResponse<object>.Ok(new object(), "Category deleted successfully"));
    }
}
