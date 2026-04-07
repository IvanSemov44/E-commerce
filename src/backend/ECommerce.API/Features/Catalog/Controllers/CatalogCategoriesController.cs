using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MediatR;
using ECommerce.API.Shared.Extensions;
using ECommerce.API.Shared.Helpers;
using ECommerce.Application.DTOs.Common;
using ECommerce.SharedKernel.Results;
using ECommerce.Catalog.Application.DTOs.Common;
using ECommerce.Catalog.Application.Commands.CreateCategory;
using ECommerce.Catalog.Application.Commands.DeleteCategory;
using ECommerce.Catalog.Application.Commands.UpdateCategory;
using ECommerce.Catalog.Application.Queries.GetCategories;
using ECommerce.Catalog.Application.Queries.GetCategoryById;
using ECommerce.Catalog.Application.Queries.GetCategoryBySlug;
using CategoryDto = ECommerce.Catalog.Application.DTOs.Categories.CategoryDto;

namespace ECommerce.API.Features.Catalog.Controllers;

[ApiController]
[Route("api/categories")]
public class CatalogCategoriesController(IMediator mediator) : ControllerBase
{
    private readonly IMediator _mediator = mediator;

    private IActionResult Problem(DomainError error) => error.Code switch
    {
        "CATEGORY_NOT_FOUND"
            => NotFound(ApiResponse<object>.Failure(error.Message, error.Code)),
        "CATEGORY_HAS_PRODUCTS" or "DUPLICATE_CATEGORY_SLUG"
            or "CATEGORY_NAME_EMPTY" or "CATEGORY_NAME_TOO_LONG" or "CATEGORY_CIRCULAR"
            => UnprocessableEntity(ApiResponse<object>.Failure(error.Message, error.Code)),
        _ => BadRequest(ApiResponse<object>.Failure(error.Message, error.Code))
    };

    // -------------------------------------------------------------------------
    // Queries
    // -------------------------------------------------------------------------

    /// <summary>
    /// Returns paginated categories (root-level by default in catalog).
    /// </summary>
    /// <param name="pageNumber">Page number (default 1).</param>
    /// <param name="pageSize">Page size (default 20, max 100).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Paged categories.</returns>
    /// <response code="200">Categories retrieved successfully.</response>
    /// <response code="400">Invalid request.</response>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<ECommerce.Catalog.Application.DTOs.Common.PaginatedResult<CategoryDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetCategories(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        (pageNumber, pageSize) = PaginationRequestNormalizer.Normalize(pageNumber, pageSize);

        var result = await _mediator.Send(new GetCategoriesQuery(pageNumber, pageSize), ct);
        return result.ToActionResult(
            data => Ok(ApiResponse<ECommerce.Catalog.Application.DTOs.Common.PaginatedResult<CategoryDto>>.Ok(data, "Categories retrieved")),
            Problem);
    }

    /// <summary>
    /// Returns paginated top-level categories (ParentId == null).
    /// </summary>
    /// <param name="pageNumber">Page number (default 1).</param>
    /// <param name="pageSize">Page size (default 20, max 100).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Paged top-level categories.</returns>
    /// <response code="200">Top-level categories retrieved successfully.</response>
    [HttpGet("top-level")]
    [ProducesResponseType(typeof(ApiResponse<ECommerce.Catalog.Application.DTOs.Common.PaginatedResult<CategoryDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetTopLevelCategories(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        (pageNumber, pageSize) = PaginationRequestNormalizer.Normalize(pageNumber, pageSize);

        var result = await _mediator.Send(new ECommerce.Catalog.Application.Queries.GetTopLevelCategories.GetTopLevelCategoriesQuery(pageNumber, pageSize), ct);
        return result.ToActionResult(
            data => Ok(ApiResponse<ECommerce.Catalog.Application.DTOs.Common.PaginatedResult<CategoryDto>>.Ok(data, "Top-level categories retrieved")),
            Problem);
    }

    /// <summary>
    /// Returns a single category by ID.
    /// </summary>
    /// <param name="id">The category ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The category.</returns>
    /// <response code="200">Category retrieved successfully.</response>
    /// <response code="404">Category not found.</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<CategoryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCategoryById(Guid id, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetCategoryByIdQuery(id), ct);
        return result.ToActionResult(
            data => Ok(ApiResponse<CategoryDto>.Ok(data, "Category retrieved")),
            Problem);
    }

    /// <summary>
    /// Returns a single category by slug.
    /// </summary>
    /// <param name="slug">The category slug.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The category.</returns>
    /// <response code="200">Category retrieved successfully.</response>
    /// <response code="404">Category not found.</response>
    [HttpGet("slug/{slug}")]
    [ProducesResponseType(typeof(ApiResponse<CategoryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCategoryBySlug(string slug, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetCategoryBySlugQuery(slug), ct);
        return result.ToActionResult(
            data => Ok(ApiResponse<CategoryDto>.Ok(data, "Category retrieved")),
            Problem);
    }

    // -------------------------------------------------------------------------
    // Commands
    // -------------------------------------------------------------------------

    /// <summary>
    /// Creates a new category. Requires Admin or SuperAdmin role.
    /// </summary>
    /// <param name="command">Category creation command payload.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The newly created category.</returns>
    /// <response code="201">Category created successfully.</response>
    /// <response code="400">Invalid category data.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="403">User does not have permission to create categories.</response>
    /// <response code="422">Unprocessable category data (validation errors).</response>
    [HttpPost]
    [Authorize(Roles = "Admin,SuperAdmin")]
    [ProducesResponseType(typeof(ApiResponse<CategoryDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> CreateCategory(
        [FromBody] CreateCategoryCommand command,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(command, ct);
        return result.ToActionResult(
            dto => CreatedAtAction(nameof(GetCategoryById), new { id = dto.Id }, ApiResponse<CategoryDto>.Ok(dto, "Category created")),
            Problem);
    }

    /// <summary>
    /// Updates an existing category. Requires Admin or SuperAdmin role.
    /// </summary>
    /// <param name="id">The category ID.</param>
    /// <param name="command">Category update command payload.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The updated category.</returns>
    /// <response code="200">Category updated successfully.</response>
    /// <response code="400">Invalid category data.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="403">User does not have permission to update categories.</response>
    /// <response code="404">Category not found.</response>
    /// <response code="422">Unprocessable category data (validation errors).</response>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    [ProducesResponseType(typeof(ApiResponse<CategoryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> UpdateCategory(
        Guid id,
        [FromBody] UpdateCategoryCommand command,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(command with { Id = id }, ct);
        return result.ToActionResult(
            data => Ok(ApiResponse<CategoryDto>.Ok(data, "Category updated")),
            Problem);
    }

    /// <summary>
    /// Deletes a category. Returns 409 if it has products. Requires Admin or SuperAdmin role.
    /// </summary>
    /// <param name="id">The category ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Deletion result.</returns>
    /// <response code="200">Category deleted successfully.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="403">User does not have permission to delete categories.</response>
    /// <response code="404">Category not found.</response>
    /// <response code="409">Category has products and cannot be deleted.</response>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteCategory(Guid id, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new DeleteCategoryCommand(id), ct);
        return result.ToActionResult(
            () => Ok(ApiResponse<object>.Ok(new { }, "Category deleted")),
            Problem);
    }
}

