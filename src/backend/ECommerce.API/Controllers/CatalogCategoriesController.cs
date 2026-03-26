using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MediatR;
using ECommerce.API.ActionFilters;
using ECommerce.Application.DTOs.Common;
using ECommerce.SharedKernel.Results;
using ECommerce.Catalog.Application.Commands.CreateCategory;
using ECommerce.Catalog.Application.Commands.DeleteCategory;
using ECommerce.Catalog.Application.Commands.UpdateCategory;
using ECommerce.Catalog.Application.Queries.GetCategories;
using ECommerce.Catalog.Application.Queries.GetCategoryById;
using ECommerce.Catalog.Application.Queries.GetCategoryBySlug;
using CategoryDto = ECommerce.Catalog.Application.DTOs.Categories.CategoryDto;

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

    // -------------------------------------------------------------------------
    // Queries
    // -------------------------------------------------------------------------

    /// <summary>Returns all categories as a flat list.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<CategoryDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetCategories(CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetCategoriesQuery(), ct);
        if (result.IsSuccess)
            return Ok(ApiResponse<IEnumerable<CategoryDto>>.Ok(result.GetDataOrThrow(), "Categories retrieved"));
        return Problem(result.GetErrorOrThrow());
    }

    /// <summary>Returns a single category by ID. Returns 404 if not found.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<CategoryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCategoryById(Guid id, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetCategoryByIdQuery(id), ct);
        if (result.IsSuccess)
            return Ok(ApiResponse<CategoryDto>.Ok(result.GetDataOrThrow(), "Category retrieved"));
        return Problem(result.GetErrorOrThrow());
    }

    /// <summary>Returns a single category by slug. Returns 404 if not found.</summary>
    [HttpGet("slug/{slug}")]
    [ProducesResponseType(typeof(ApiResponse<CategoryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCategoryBySlug(string slug, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetCategoryBySlugQuery(slug), ct);
        if (result.IsSuccess)
            return Ok(ApiResponse<CategoryDto>.Ok(result.GetDataOrThrow(), "Category retrieved"));
        return Problem(result.GetErrorOrThrow());
    }

    // -------------------------------------------------------------------------
    // Commands
    // -------------------------------------------------------------------------

    /// <summary>Creates a new category. Requires Admin or SuperAdmin role.</summary>
    [HttpPost]
    [Authorize(Roles = "Admin,SuperAdmin")]
    [ValidationFilter]
    [ProducesResponseType(typeof(ApiResponse<CategoryDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> CreateCategory(
        [FromBody] CreateCategoryCommand command,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(command, ct);
        if (result.IsSuccess)
        {
            var dto = result.GetDataOrThrow();
            return CreatedAtAction(nameof(GetCategoryById), new { id = dto.Id }, ApiResponse<CategoryDto>.Ok(dto, "Category created"));
        }
        return Problem(result.GetErrorOrThrow());
    }

    /// <summary>Updates an existing category. Requires Admin or SuperAdmin role.</summary>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    [ValidationFilter]
    [ProducesResponseType(typeof(ApiResponse<CategoryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> UpdateCategory(
        Guid id,
        [FromBody] UpdateCategoryCommand command,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(command with { Id = id }, ct);
        if (result.IsSuccess)
            return Ok(ApiResponse<CategoryDto>.Ok(result.GetDataOrThrow(), "Category updated"));
        return Problem(result.GetErrorOrThrow());
    }

    /// <summary>Deletes a category. Returns 409 if it has products. Requires Admin or SuperAdmin role.</summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteCategory(Guid id, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new DeleteCategoryCommand(id), ct);
        if (result.IsSuccess)
            return Ok(ApiResponse<object>.Ok(new { }, "Category deleted"));
        return Problem(result.GetErrorOrThrow());
    }
}
