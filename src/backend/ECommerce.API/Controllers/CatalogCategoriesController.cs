using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using ECommerce.SharedKernel.Results;
using ECommerce.Application.DTOs.Common;
using CatalogCategory = ECommerce.Catalog.Application.DTOs.Categories.CategoryDto;
using CatalogCommon = ECommerce.Catalog.Application.DTOs.Common;
using ECommerce.Catalog.Application.Queries.GetCategories;
using ECommerce.Catalog.Application.Queries.GetCategoryById;
using ECommerce.Catalog.Application.Queries.GetCategoryBySlug;
using ECommerce.Catalog.Application.Commands.CreateCategory;
using ECommerce.Catalog.Application.Commands.UpdateCategory;
using ECommerce.API.ActionFilters;

namespace ECommerce.API.Controllers;

[ApiController]
[Route("api/catalog/categories")]
public class CatalogCategoriesController(IMediator mediator) : ControllerBase
{
    private readonly IMediator _mediator = mediator;

    private IActionResult Problem(DomainError error) => error.Code switch
    {
        "CATEGORY_NOT_FOUND"
            => NotFound(ApiResponse<object>.Failure(error.Message, error.Code)),
        "CATEGORY_HAS_PRODUCTS"
            => Conflict(ApiResponse<object>.Failure(error.Message, error.Code)),
        "CATEGORY_NAME_EMPTY" or "CATEGORY_NAME_TOO_LONG" or "CATEGORY_CIRCULAR"
            => UnprocessableEntity(ApiResponse<object>.Failure(error.Message, error.Code)),
        _ => BadRequest(ApiResponse<object>.Failure(error.Message, error.Code))
    };

    /// <summary>
    /// Retrieves all categories available in the catalog.
    /// Returns categories as a flat collection; clients may reconstruct hierarchy from the parent references.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A collection of category DTOs.</returns>
    /// <response code="200">Categories retrieved successfully.</response>
    /// <response code="400">Invalid request parameters.</response>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<CatalogCategory>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetCategories(CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetCategoriesQuery(), ct);
        if (result.IsSuccess)
            return Ok(ApiResponse<IEnumerable<CatalogCategory>>.Ok(result.GetDataOrThrow(), "Categories retrieved"));
        return Problem(result.GetErrorOrThrow());
    }

    /// <summary>
    /// Retrieves a single category by its identifier, including basic relationships.
    /// </summary>
    /// <param name="id">Category unique identifier (GUID).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Category DTO with details and parent/child references.</returns>
    /// <response code="200">Category retrieved successfully.</response>
    /// <response code="404">Category not found.</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<CatalogCategory>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCategoryById(Guid id, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetCategoryByIdQuery(id), ct);
        if (result.IsSuccess)
            return Ok(ApiResponse<CatalogCategory>.Ok(result.GetDataOrThrow(), "Category retrieved"));
        return Problem(result.GetErrorOrThrow());
    }

    /// <summary>
    /// Retrieves a category by its URL-friendly slug.
    /// </summary>
    /// <param name="slug">Category slug (URL-friendly identifier).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Category DTO matching the provided slug.</returns>
    /// <response code="200">Category retrieved successfully.</response>
    /// <response code="404">Category not found.</response>
    [HttpGet("slug/{slug}")]
    [ProducesResponseType(typeof(ApiResponse<CatalogCategory>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCategoryBySlug(string slug, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetCategoryBySlugQuery(slug), ct);
        if (result.IsSuccess)
            return Ok(ApiResponse<CatalogCategory>.Ok(result.GetDataOrThrow(), "Category retrieved"));
        return Problem(result.GetErrorOrThrow());
    }

    /// <summary>
    /// Creates a new category. Requires `Admin` or `SuperAdmin` role.
    /// </summary>
    /// <param name="command">Command with new category details (name, slug, parentId).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The created category details.</returns>
    /// <response code="201">Category created successfully.</response>
    /// <response code="400">Invalid input data.</response>
    /// <response code="422">Business validation failed (e.g., duplicate slug).</response>
    [HttpPost]
    [Authorize(Roles = "Admin,SuperAdmin")]
    [ValidationFilter]
    [ProducesResponseType(typeof(ApiResponse<CatalogCategory>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> CreateCategory([FromBody] CreateCategoryCommand command, CancellationToken ct = default)
    {
        var result = await _mediator.Send(command, ct);
        if (result.IsSuccess)
        {
            var dto = result.GetDataOrThrow();
            return CreatedAtAction(nameof(GetCategoryById), new { id = dto.Id }, ApiResponse<CatalogCategory>.Ok(dto, "Category created"));
        }
        return Problem(result.GetErrorOrThrow());
    }

    /// <summary>
    /// Updates an existing category. Requires `Admin` or `SuperAdmin` role.
    /// </summary>
    /// <param name="id">Category identifier (GUID) to update.</param>
    /// <param name="command">Update command containing new category values.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The updated category details.</returns>
    /// <response code="200">Category updated successfully.</response>
    /// <response code="404">Category not found.</response>
    /// <response code="400">Invalid request data.</response>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    [ValidationFilter]
    [ProducesResponseType(typeof(ApiResponse<CatalogCategory>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateCategory(Guid id, [FromBody] UpdateCategoryCommand command, CancellationToken ct = default)
    {
        var result = await _mediator.Send(command with { Id = id }, ct);
        if (result.IsSuccess)
            return Ok(ApiResponse<CatalogCategory>.Ok(result.GetDataOrThrow(), "Category updated"));
        return Problem(result.GetErrorOrThrow());
    }

    /// <summary>
    /// Deletes the specified category. Requires `Admin` or `SuperAdmin` role.
    /// Categories with dependent products or child categories cannot be deleted.
    /// </summary>
    /// <param name="id">Category identifier (GUID) to delete.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Deletion result or failure reason.</returns>
    /// <response code="200">Category deleted successfully.</response>
    /// <response code="400">Category cannot be deleted due to business rules.</response>
    /// <response code="404">Category not found.</response>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteCategory(Guid id, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new ECommerce.Catalog.Application.Commands.DeleteCategory.DeleteCategoryCommand(id), ct);
        if (result.IsSuccess)
            return Ok(ApiResponse<object>.Ok(new object(), "Category deleted"));
        return Problem(result.GetErrorOrThrow());
    }
}
