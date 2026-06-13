using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MediatR;
using ECommerce.API.Shared.Extensions;
using ECommerce.API.Common.Helpers;
using ECommerce.Contracts.DTOs.Common;
using ECommerce.Catalog.Application.Commands;
using ECommerce.Catalog.Application.Queries;
using ECommerce.SharedKernel.Pagination;
using CategoryDto = ECommerce.Catalog.Application.DTOs.Categories.CategoryDto;

namespace ECommerce.API.Features.Catalog.Controllers;

[ApiController]
[Route("api/categories")]
public class CatalogCategoriesController(IMediator mediator) : ControllerBase
{
    private readonly IMediator _mediator = mediator;

    // -------------------------------------------------------------------------
    // Queries
    // -------------------------------------------------------------------------

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResult<CategoryDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCategories(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        (pageNumber, pageSize) = PaginationRequestNormalizer.Normalize(pageNumber, pageSize);
        var result = await _mediator.Send(new GetCategoriesQuery(pageNumber, pageSize), ct);
        return result.ToActionResult(data => Ok(ApiResponse<PaginatedResult<CategoryDto>>.Ok(data)));
    }

    [HttpGet("top-level")]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResult<CategoryDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTopLevelCategories(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        (pageNumber, pageSize) = PaginationRequestNormalizer.Normalize(pageNumber, pageSize);
        var result = await _mediator.Send(new GetTopLevelCategoriesQuery(pageNumber, pageSize), ct);
        return result.ToActionResult(data => Ok(ApiResponse<PaginatedResult<CategoryDto>>.Ok(data)));
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<CategoryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCategoryById(Guid id, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetCategoryByIdQuery(id), ct);
        return result.ToActionResult(data => Ok(ApiResponse<CategoryDto>.Ok(data)));
    }

    [HttpGet("slug/{slug}")]
    [ProducesResponseType(typeof(ApiResponse<CategoryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCategoryBySlug(string slug, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetCategoryBySlugQuery(slug), ct);
        return result.ToActionResult(data => Ok(ApiResponse<CategoryDto>.Ok(data)));
    }

    // -------------------------------------------------------------------------
    // Commands
    // -------------------------------------------------------------------------

    [HttpPost]
    [Authorize(Roles = "Admin,SuperAdmin")]
    [ProducesResponseType(typeof(ApiResponse<Guid>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> CreateCategory(
        [FromBody] CreateCategoryCommand command,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(command, ct);
        return result.ToActionResult(
            id => CreatedAtAction(nameof(GetCategoryById), new { id }, ApiResponse<Guid>.Ok(id)));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    [ProducesResponseType(typeof(ApiResponse<Guid>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> UpdateCategory(
        Guid id,
        [FromBody] UpdateCategoryCommand command,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(command with { Id = id }, ct);
        return result.ToActionResult(categoryId => Ok(ApiResponse<Guid>.Ok(categoryId)));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> DeleteCategory(Guid id, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new DeleteCategoryCommand(id), ct);
        return result.ToActionResult(() => NoContent());
    }
}
