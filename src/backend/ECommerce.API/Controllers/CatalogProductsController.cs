using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MediatR;
using ECommerce.API.ActionFilters;
using ECommerce.Application.DTOs.Common;
using ECommerce.SharedKernel.Results;
using ECommerce.Catalog.Application.DTOs.Products;
using ECommerce.Catalog.Application.Commands.ActivateProduct;
using ECommerce.Catalog.Application.Commands.AddProductImage;
using ECommerce.Catalog.Application.Commands.CreateProduct;
using ECommerce.Catalog.Application.Commands.DeactivateProduct;
using ECommerce.Catalog.Application.Commands.DeleteProduct;
using ECommerce.Catalog.Application.Commands.SetPrimaryImage;
using ECommerce.Catalog.Application.Commands.UpdateProduct;
using ECommerce.Catalog.Application.Commands.UpdateProductPrice;
using ECommerce.Catalog.Application.Queries.GetFeaturedProducts;
using ECommerce.Catalog.Application.Queries.GetProductById;
using ECommerce.Catalog.Application.Queries.GetProductBySlug;
using ECommerce.Catalog.Application.Queries.GetProducts;
using ECommerce.Catalog.Application.Queries.GetProductsByCategory;
using ECommerce.Catalog.Application.Queries.GetProductsByPriceRange;
using ECommerce.Catalog.Application.Queries.SearchProducts;
using CatalogCommon = ECommerce.Catalog.Application.DTOs.Common;

namespace ECommerce.API.Controllers;

[ApiController]
[Route("api/catalog/products")]
public class CatalogProductsController(IMediator mediator) : ControllerBase
{
    private readonly IMediator _mediator = mediator;

    private IActionResult Problem(DomainError error) => error.Code switch
    {
        "PRODUCT_NOT_FOUND" or "CATEGORY_NOT_FOUND" or "IMAGE_NOT_FOUND"
            => NotFound(ApiResponse<object>.Failure(error.Message, error.Code)),
        "SKU_ALREADY_EXISTS"
            => Conflict(ApiResponse<object>.Failure(error.Message, error.Code)),
        "PRODUCT_NAME_EMPTY" or "PRODUCT_NAME_TOO_LONG"
        or "SKU_EMPTY" or "SKU_TOO_LONG"
        or "MONEY_NEGATIVE" or "MONEY_INVALID_CURRENCY" or "MONEY_CURRENCY_MISMATCH"
        or "WEIGHT_NEGATIVE" or "STOCK_QUANTITY_NEGATIVE"
        or "PRODUCT_DISCONTINUED" or "PRODUCT_MAX_IMAGES"
            => UnprocessableEntity(ApiResponse<object>.Failure(error.Message, error.Code)),
        _ => BadRequest(ApiResponse<object>.Failure(error.Message, error.Code))
    };

    // -------------------------------------------------------------------------
    // Queries
    // -------------------------------------------------------------------------

    /// <summary>Returns a paged list of products.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<CatalogCommon.PaginatedResult<ProductDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetProducts(
        [FromQuery, Range(1, int.MaxValue)] int page = 1,
        [FromQuery, Range(1, 100)] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetProductsQuery(page, pageSize), ct);
        if (result.IsSuccess)
            return Ok(ApiResponse<CatalogCommon.PaginatedResult<ProductDto>>.Ok(result.GetDataOrThrow(), "Products retrieved"));
        return Problem(result.GetErrorOrThrow());
    }

    /// <summary>Returns a single product by ID. Returns 404 if not found.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<ProductDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProductById(Guid id, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetProductByIdQuery(id), ct);
        if (result.IsSuccess)
            return Ok(ApiResponse<ProductDetailDto>.Ok(result.GetDataOrThrow(), "Product retrieved"));
        return Problem(result.GetErrorOrThrow());
    }

    /// <summary>Returns a single product by slug. Returns 404 if not found.</summary>
    [HttpGet("slug/{slug}")]
    [ProducesResponseType(typeof(ApiResponse<ProductDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProductBySlug(string slug, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetProductBySlugQuery(slug), ct);
        if (result.IsSuccess)
            return Ok(ApiResponse<ProductDetailDto>.Ok(result.GetDataOrThrow(), "Product retrieved"));
        return Problem(result.GetErrorOrThrow());
    }

    /// <summary>Returns featured products up to the given limit.</summary>
    [HttpGet("featured")]
    [ProducesResponseType(typeof(ApiResponse<List<ProductDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetFeaturedProducts(
        [FromQuery] int limit = 10,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetFeaturedProductsQuery(limit), ct);
        if (result.IsSuccess)
            return Ok(ApiResponse<List<ProductDto>>.Ok(result.GetDataOrThrow(), "Featured products retrieved"));
        return Problem(result.GetErrorOrThrow());
    }

    /// <summary>Searches products by text query. Returns paged results.</summary>
    [HttpGet("search")]
    [ProducesResponseType(typeof(ApiResponse<CatalogCommon.PaginatedResult<ProductDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SearchProducts(
        [FromQuery] string? q,
        [FromQuery, Range(1, int.MaxValue)] int page = 1,
        [FromQuery, Range(1, 100)] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(new SearchProductsQuery(q ?? string.Empty, page, pageSize), ct);
        if (result.IsSuccess)
            return Ok(ApiResponse<CatalogCommon.PaginatedResult<ProductDto>>.Ok(result.GetDataOrThrow(), "Search results"));
        return Problem(result.GetErrorOrThrow());
    }

    /// <summary>Returns paged products belonging to a category.</summary>
    [HttpGet("by-category/{categoryId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<CatalogCommon.PaginatedResult<ProductDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetProductsByCategory(
        Guid categoryId,
        [FromQuery, Range(1, int.MaxValue)] int page = 1,
        [FromQuery, Range(1, 100)] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetProductsByCategoryQuery(categoryId, page, pageSize), ct);
        if (result.IsSuccess)
            return Ok(ApiResponse<CatalogCommon.PaginatedResult<ProductDto>>.Ok(result.GetDataOrThrow(), "Products by category"));
        return Problem(result.GetErrorOrThrow());
    }

    /// <summary>Returns paged products within a price range.</summary>
    [HttpGet("by-price")]
    [ProducesResponseType(typeof(ApiResponse<CatalogCommon.PaginatedResult<ProductDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetProductsByPriceRange(
        [FromQuery] decimal min,
        [FromQuery] decimal max,
        [FromQuery, Range(1, int.MaxValue)] int page = 1,
        [FromQuery, Range(1, 100)] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetProductsByPriceRangeQuery(min, max, page, pageSize), ct);
        if (result.IsSuccess)
            return Ok(ApiResponse<CatalogCommon.PaginatedResult<ProductDto>>.Ok(result.GetDataOrThrow(), "Products by price range"));
        return Problem(result.GetErrorOrThrow());
    }

    // -------------------------------------------------------------------------
    // Commands
    // -------------------------------------------------------------------------

    /// <summary>Creates a new product. Requires Admin or SuperAdmin role.</summary>
    [HttpPost]
    [Authorize(Roles = "Admin,SuperAdmin")]
    [ValidationFilter]
    [ProducesResponseType(typeof(ApiResponse<ProductDetailDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> CreateProduct(
        [FromBody] CreateProductCommand command,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(command, ct);
        if (result.IsSuccess)
        {
            var dto = result.GetDataOrThrow();
            return CreatedAtAction(nameof(GetProductById), new { id = dto.Id }, ApiResponse<ProductDetailDto>.Ok(dto, "Product created"));
        }
        return Problem(result.GetErrorOrThrow());
    }

    /// <summary>Updates an existing product. Requires Admin or SuperAdmin role.</summary>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    [ValidationFilter]
    [ProducesResponseType(typeof(ApiResponse<ProductDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> UpdateProduct(
        Guid id,
        [FromBody] UpdateProductCommand command,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(command with { Id = id }, ct);
        if (result.IsSuccess)
            return Ok(ApiResponse<ProductDetailDto>.Ok(result.GetDataOrThrow(), "Product updated"));
        return Problem(result.GetErrorOrThrow());
    }

    /// <summary>Deletes a product permanently. Requires Admin or SuperAdmin role.</summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteProduct(Guid id, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new DeleteProductCommand(id), ct);
        if (result.IsSuccess)
            return Ok(ApiResponse<object>.Ok(new { },"Product deleted"));
        return Problem(result.GetErrorOrThrow());
    }

    /// <summary>Updates the price of a product. Requires Admin or SuperAdmin role.</summary>
    [HttpPut("{id:guid}/price")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    [ValidationFilter]
    [ProducesResponseType(typeof(ApiResponse<ProductDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> UpdateProductPrice(
        Guid id,
        [FromBody] UpdateProductPriceCommand command,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(command with { Id = id }, ct);
        if (result.IsSuccess)
            return Ok(ApiResponse<ProductDetailDto>.Ok(result.GetDataOrThrow(), "Price updated"));
        return Problem(result.GetErrorOrThrow());
    }

    /// <summary>Activates a product. Requires Admin or SuperAdmin role.</summary>
    [HttpPost("{id:guid}/activate")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> ActivateProduct(Guid id, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new ActivateProductCommand(id), ct);
        if (result.IsSuccess)
            return Ok(ApiResponse<object>.Ok(new { },"Product activated"));
        return Problem(result.GetErrorOrThrow());
    }

    /// <summary>Deactivates a product. Requires Admin or SuperAdmin role.</summary>
    [HttpPost("{id:guid}/deactivate")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> DeactivateProduct(Guid id, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new DeactivateProductCommand(id), ct);
        if (result.IsSuccess)
            return Ok(ApiResponse<object>.Ok(new { },"Product deactivated"));
        return Problem(result.GetErrorOrThrow());
    }

    /// <summary>Adds an image to a product. Requires Admin or SuperAdmin role.</summary>
    [HttpPost("{id:guid}/images")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    [ValidationFilter]
    [ProducesResponseType(typeof(ApiResponse<ProductDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> AddProductImage(
        Guid id,
        [FromBody] AddProductImageCommand command,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(command with { ProductId = id }, ct);
        if (result.IsSuccess)
            return Ok(ApiResponse<ProductDetailDto>.Ok(result.GetDataOrThrow(), "Image added"));
        return Problem(result.GetErrorOrThrow());
    }

    /// <summary>Sets the primary image of a product. Requires Admin or SuperAdmin role.</summary>
    [HttpPost("{id:guid}/images/{imageId:guid}/primary")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    [ProducesResponseType(typeof(ApiResponse<ProductDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetPrimaryImage(Guid id, Guid imageId, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new SetPrimaryImageCommand(id, imageId), ct);
        if (result.IsSuccess)
            return Ok(ApiResponse<ProductDetailDto>.Ok(result.GetDataOrThrow(), "Primary image set"));
        return Problem(result.GetErrorOrThrow());
    }
}
