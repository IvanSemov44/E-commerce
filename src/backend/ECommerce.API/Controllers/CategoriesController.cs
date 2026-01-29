using ECommerce.Application.DTOs;
using ECommerce.Application.DTOs.Common;
using ECommerce.Application.Services;
using ECommerce.Application.Interfaces;
using ECommerce.Core.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.API.Controllers;

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

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetAllCategories()
    {
        try
        {
            var categories = await _categoryService.GetAllCategoriesAsync();
            return Ok(ApiResponse<IEnumerable<CategoryDto>>.Ok(categories, "Categories retrieved successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving categories");
            return StatusCode(500, ApiResponse<IEnumerable<CategoryDto>>.Error("An error occurred while retrieving categories"));
        }
    }

    [HttpGet("top-level")]
    [AllowAnonymous]
    public async Task<IActionResult> GetTopLevelCategories()
    {
        try
        {
            var categories = await _categoryService.GetTopLevelCategoriesAsync();
            return Ok(ApiResponse<IEnumerable<CategoryDto>>.Ok(categories, "Top-level categories retrieved successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving top-level categories");
            return StatusCode(500, ApiResponse<IEnumerable<CategoryDto>>.Error("An error occurred"));
        }
    }

    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetCategoryById(Guid id)
    {
        try
        {
            var category = await _categoryService.GetCategoryByIdAsync(id);
            if (category == null)
            {
                return NotFound(ApiResponse<CategoryDetailDto>.Error("Category not found"));
            }
            return Ok(ApiResponse<CategoryDetailDto>.Ok(category, "Category retrieved successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving category {Id}", id);
            return StatusCode(500, ApiResponse<CategoryDetailDto>.Error("An error occurred"));
        }
    }

    [HttpGet("slug/{slug}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetCategoryBySlug(string slug)
    {
        try
        {
            var category = await _categoryService.GetCategoryBySlugAsync(slug);
            if (category == null)
            {
                return NotFound(ApiResponse<CategoryDetailDto>.Error("Category not found"));
            }
            return Ok(ApiResponse<CategoryDetailDto>.Ok(category, "Category retrieved successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving category with slug {Slug}", slug);
            return StatusCode(500, ApiResponse<CategoryDetailDto>.Error("An error occurred"));
        }
    }

    [HttpPost]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> CreateCategory([FromBody] CreateCategoryDto dto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                return BadRequest(ApiResponse<CategoryDetailDto>.Error("Validation failed", errors));
            }

            var category = await _categoryService.CreateCategoryAsync(dto);
            return CreatedAtAction(nameof(GetCategoryById), new { id = category.Id },
                ApiResponse<CategoryDetailDto>.Ok(category, "Category created successfully"));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResponse<CategoryDetailDto>.Error(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating category");
            return StatusCode(500, ApiResponse<CategoryDetailDto>.Error("An error occurred"));
        }
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> UpdateCategory(Guid id, [FromBody] UpdateCategoryDto dto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                return BadRequest(ApiResponse<CategoryDetailDto>.Error("Validation failed", errors));
            }

            var category = await _categoryService.UpdateCategoryAsync(id, dto);
            return Ok(ApiResponse<CategoryDetailDto>.Ok(category, "Category updated successfully"));
        }
        catch (ArgumentException ex)
        {
            return NotFound(ApiResponse<CategoryDetailDto>.Error(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating category {Id}", id);
            return StatusCode(500, ApiResponse<CategoryDetailDto>.Error("An error occurred"));
        }
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> DeleteCategory(Guid id)
    {
        try
        {
            var result = await _categoryService.DeleteCategoryAsync(id);
            if (!result)
            {
                return NotFound(ApiResponse<bool>.Error("Category not found"));
            }
            return Ok(ApiResponse<bool>.Ok(true, "Category deleted successfully"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<bool>.Error(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting category {Id}", id);
            return StatusCode(500, ApiResponse<bool>.Error("An error occurred"));
        }
    }
}
