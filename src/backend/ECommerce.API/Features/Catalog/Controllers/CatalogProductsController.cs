using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MediatR;
using ECommerce.API.Common.Extensions;
using ECommerce.Contracts.DTOs.Common;
using ECommerce.Catalog.Application.Commands;
using ECommerce.Catalog.Application.Queries;
using ECommerce.Catalog.Application.DTOs.Products;
using ECommerce.Inventory.Application.DTOs;
using ECommerce.SharedKernel.Pagination;

namespace ECommerce.API.Features.Catalog.Controllers;

[ApiController]
[Route("api/products")]
public class CatalogProductsController(IMediator mediator) : ControllerBase
{
    private readonly IMediator _mediator = mediator;

    // -------------------------------------------------------------------------
    // Queries
    // -------------------------------------------------------------------------

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResult<ProductDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetProducts([FromQuery] GetProductsQuery query, CancellationToken ct = default)
    {
        var result = await _mediator.Send(query, ct);
        return result.ToActionResult(data => Ok(ApiResponse<PaginatedResult<ProductDto>>.Ok(data)));
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<ProductDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProductById(Guid id, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetProductByIdQuery(id), ct);
        return result.ToActionResult(data => Ok(ApiResponse<ProductDetailDto>.Ok(data)));
    }

    [HttpGet("slug/{slug}")]
    [ProducesResponseType(typeof(ApiResponse<ProductDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProductBySlug(string slug, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetProductBySlugQuery(slug), ct);
        return result.ToActionResult(data => Ok(ApiResponse<ProductDetailDto>.Ok(data)));
    }

    [HttpGet("featured")]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResult<ProductDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetFeaturedProducts([FromQuery] GetFeaturedProductsQuery query, CancellationToken ct = default)
    {
        var result = await _mediator.Send(query, ct);
        return result.ToActionResult(data => Ok(ApiResponse<PaginatedResult<ProductDto>>.Ok(data)));
    }

    [HttpGet("low-stock")]
    [ProducesResponseType(typeof(ApiResponse<List<ProductDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetLowStockProducts([FromQuery] int threshold = 10, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetLowStockProductsQuery(threshold), ct);
        return result.ToActionResult(data => Ok(ApiResponse<List<ProductDto>>.Ok(data)));
    }

    [HttpGet("search")]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResult<ProductDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> SearchProducts([FromQuery] SearchProductsQuery query, CancellationToken ct = default)
    {
        var result = await _mediator.Send(query, ct);
        return result.ToActionResult(data => Ok(ApiResponse<PaginatedResult<ProductDto>>.Ok(data)));
    }

    [HttpGet("by-category/{categoryId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResult<ProductDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProductsByCategory(Guid categoryId, [FromQuery] GetProductsByCategoryQuery query, CancellationToken ct = default)
    {
        var result = await _mediator.Send(query with { CategoryId = categoryId }, ct);
        return result.ToActionResult(data => Ok(ApiResponse<PaginatedResult<ProductDto>>.Ok(data)));
    }

    [HttpGet("by-price")]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResult<ProductDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetProductsByPriceRange([FromQuery] GetProductsByPriceRangeQuery query, CancellationToken ct = default)
    {
        var result = await _mediator.Send(query, ct);
        return result.ToActionResult(data => Ok(ApiResponse<PaginatedResult<ProductDto>>.Ok(data)));
    }

    // -------------------------------------------------------------------------
    // Commands
    // -------------------------------------------------------------------------

    [HttpPost]
    [Authorize(Roles = "Admin,SuperAdmin")]
    [ProducesResponseType(typeof(ApiResponse<Guid>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> CreateProduct([FromBody] CreateProductCommand command, CancellationToken ct = default)
    {
        var result = await _mediator.Send(command, ct);
        return result.ToActionResult(
            id => CreatedAtAction(nameof(GetProductById), new { id }, ApiResponse<Guid>.Ok(id)));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    [ProducesResponseType(typeof(ApiResponse<Guid>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> UpdateProduct(Guid id, [FromBody] UpdateProductCommand command, CancellationToken ct = default)
    {
        var result = await _mediator.Send(command with { Id = id }, ct);
        return result.ToActionResult(productId => Ok(ApiResponse<Guid>.Ok(productId)));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteProduct(Guid id, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new DeleteProductCommand(id), ct);
        return result.ToActionResult(() => NoContent());
    }

    [HttpPut("{id:guid}/price")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    [ProducesResponseType(typeof(ApiResponse<Guid>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> UpdateProductPrice(Guid id, [FromBody] UpdateProductPriceCommand command, CancellationToken ct = default)
    {
        var result = await _mediator.Send(command with { Id = id }, ct);
        return result.ToActionResult(productId => Ok(ApiResponse<Guid>.Ok(productId)));
    }

    [HttpPut("{id:guid}/stock")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> UpdateProductStock(Guid id, [FromBody] AdjustStockRequest request, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new UpdateProductStockCommand(id, request.Quantity, request.Reason ?? string.Empty), ct);
        return result.ToActionResult(() => NoContent());
    }

    [HttpPost("{id:guid}/activate")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> ActivateProduct(Guid id, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new ActivateProductCommand(id), ct);
        return result.ToActionResult(() => NoContent());
    }

    [HttpPost("{id:guid}/deactivate")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> DeactivateProduct(Guid id, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new DeactivateProductCommand(id), ct);
        return result.ToActionResult(() => NoContent());
    }

    [HttpPost("{id:guid}/images")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    [ProducesResponseType(typeof(ApiResponse<Guid>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> AddProductImage(Guid id, [FromBody] AddProductImageCommand command, CancellationToken ct = default)
    {
        var result = await _mediator.Send(command with { ProductId = id }, ct);
        return result.ToActionResult(
            imageId => CreatedAtAction(nameof(GetProductById), new { id }, ApiResponse<Guid>.Ok(imageId)));
    }

    [HttpPost("{id:guid}/images/{imageId:guid}/primary")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetPrimaryImage(Guid id, Guid imageId, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new SetPrimaryImageCommand(id, imageId), ct);
        return result.ToActionResult(_ => NoContent());
    }
}
