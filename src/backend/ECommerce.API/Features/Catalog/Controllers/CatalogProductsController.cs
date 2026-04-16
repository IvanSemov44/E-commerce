using System.Collections.Frozen;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MediatR;
using ECommerce.API.Common.Extensions;
using ECommerce.Contracts.DTOs.Common;
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
using ECommerce.Contracts.DTOs.Inventory;
using ECommerce.Catalog.Application.Commands.UpdateProductStock;
using ECommerce.Catalog.Application.Queries.GetLowStockProducts;
using ECommerce.Catalog.Application.Queries.SearchProducts;
using CatalogCommon = ECommerce.Catalog.Application.DTOs.Common;

namespace ECommerce.API.Features.Catalog.Controllers;

[ApiController]
[Route("api/products")]
public class CatalogProductsController(IMediator mediator) : ControllerBase
{
    private readonly IMediator _mediator = mediator;

    private static readonly FrozenSet<string> _notFound = FrozenSet.Create(
        "PRODUCT_NOT_FOUND", "CATEGORY_NOT_FOUND", "IMAGE_NOT_FOUND");

    private static readonly FrozenSet<string> _conflict = FrozenSet.Create(
        "SKU_ALREADY_EXISTS");

    private static readonly FrozenSet<string> _unprocessable = FrozenSet.Create(
        "PRODUCT_NAME_EMPTY", "PRODUCT_NAME_TOO_LONG",
        "SKU_EMPTY",          "SKU_TOO_LONG",
        "MONEY_NEGATIVE",     "MONEY_INVALID_CURRENCY", "MONEY_CURRENCY_MISMATCH",
        "WEIGHT_NEGATIVE",    "STOCK_QUANTITY_NEGATIVE",
        "PRODUCT_DISCONTINUED", "PRODUCT_MAX_IMAGES",
        "DUPLICATE_PRODUCT_SLUG");

    private IActionResult Problem(DomainError error)
    {
        var body = ApiResponse<object>.Failure(error.Message, error.Code);
        if (_notFound.Contains(error.Code))       return NotFound(body);
        if (_conflict.Contains(error.Code))       return Conflict(body);
        if (_unprocessable.Contains(error.Code))  return UnprocessableEntity(body);
        return BadRequest(body);
    }

    // -------------------------------------------------------------------------
    // Queries
    // -------------------------------------------------------------------------

    /// <summary>
    /// Returns a paged list of products.
    /// </summary>
    /// <param name="page">The page number (default: 1).</param>
    /// <param name="pageSize">The page size (default: 20, max: 100).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A paginated list of product DTOs.</returns>
    /// <response code="200">Products retrieved successfully.</response>
    /// <response code="400">Invalid request parameters.</response>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<CatalogCommon.PaginatedResult<ProductDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetProducts(
        [FromQuery, Range(1, int.MaxValue)] int page = 1,
        [FromQuery, Range(1, 100)] int pageSize = 20,
        [FromQuery] Guid? categoryId = null,
        [FromQuery] string? search = null,
        [FromQuery] decimal? minPrice = null,
        [FromQuery] decimal? maxPrice = null,
        [FromQuery] decimal? minRating = null,
        [FromQuery] bool? isFeatured = null,
        [FromQuery] string? sortBy = null,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetProductsQuery(page, pageSize, categoryId, search, minPrice, maxPrice, minRating, isFeatured, sortBy), ct);
        return result.ToActionResult(
            data => Ok(ApiResponse<CatalogCommon.PaginatedResult<ProductDto>>.Ok(data, "Products retrieved")),
            Problem);
    }

    /// <summary>
    /// Returns a single product by ID.
    /// </summary>
    /// <param name="id">The product ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The product details.</returns>
    /// <response code="200">Product retrieved successfully.</response>
    /// <response code="404">Product not found.</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<ProductDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProductById(Guid id, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetProductByIdQuery(id), ct);
        return result.ToActionResult(
            data => Ok(ApiResponse<ProductDetailDto>.Ok(data, "Product retrieved")),
            Problem);
    }

    /// <summary>
    /// Returns a single product by slug.
    /// </summary>
    /// <param name="slug">The product slug.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The product details.</returns>
    /// <response code="200">Product retrieved successfully.</response>
    /// <response code="404">Product not found.</response>
    [HttpGet("slug/{slug}")]
    [ProducesResponseType(typeof(ApiResponse<ProductDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProductBySlug(string slug, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetProductBySlugQuery(slug), ct);
        return result.ToActionResult(
            data => Ok(ApiResponse<ProductDetailDto>.Ok(data, "Product retrieved")),
            Problem);
    }

    /// <summary>
    /// Returns paged featured products.
    /// </summary>
    /// <param name="page">The page number (default: 1).</param>
    /// <param name="pageSize">The page size (default: 10).</param>
    /// <param name="ct">Cancellation token.</param>
    [HttpGet("featured")]
    [ProducesResponseType(typeof(ApiResponse<CatalogCommon.PaginatedResult<ProductDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetFeaturedProducts(
        [FromQuery, Range(1, int.MaxValue)] int page = 1,
        [FromQuery, Range(1, 100)] int pageSize = 10,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetFeaturedProductsQuery(page, pageSize), ct);
        return result.ToActionResult(
            data => Ok(ApiResponse<CatalogCommon.PaginatedResult<ProductDto>>.Ok(data, "Featured products retrieved")),
            Problem);
    }

    /// <summary>
    /// Returns products with stock at or below the given threshold.
    /// </summary>
    /// <param name="threshold">Stock threshold to consider low (default: 10).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of products with low stock.</returns>
    /// <response code="200">Low stock products retrieved successfully.</response>
    /// <response code="400">Invalid request parameters.</response>
    [HttpGet("low-stock")]
    [ProducesResponseType(typeof(ApiResponse<List<ProductDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetLowStockProducts(
        [FromQuery, Range(1, int.MaxValue)] int threshold = 10,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetLowStockProductsQuery(threshold), ct);
        return result.ToActionResult(
            data => Ok(ApiResponse<List<ProductDto>>.Ok(data, "Low stock products retrieved")),
            Problem);
    }

    /// <summary>
    /// Searches products by text query. Returns paged results.
    /// </summary>
    /// <param name="q">Search query text.</param>
    /// <param name="page">The page number (default: 1).</param>
    /// <param name="pageSize">The page size (default: 20, max: 100).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Paged search results of products.</returns>
    /// <response code="200">Search results returned successfully.</response>
    /// <response code="400">Invalid search parameters.</response>
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
        return result.ToActionResult(
            data => Ok(ApiResponse<CatalogCommon.PaginatedResult<ProductDto>>.Ok(data, "Search results")),
            Problem);
    }

    /// <summary>
    /// Returns paged products belonging to a category.
    /// </summary>
    /// <param name="categoryId">The category ID to filter products by.</param>
    /// <param name="page">The page number (default: 1).</param>
    /// <param name="pageSize">The page size (default: 20, max: 100).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Paged list of products in the specified category.</returns>
    /// <response code="200">Products by category returned successfully.</response>
    /// <response code="400">Invalid request parameters.</response>
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
        return result.ToActionResult(
            data => Ok(ApiResponse<CatalogCommon.PaginatedResult<ProductDto>>.Ok(data, "Products by category")),
            Problem);
    }

    /// <summary>
    /// Returns paged products within a price range.
    /// </summary>
    /// <param name="min">Minimum price (inclusive).</param>
    /// <param name="max">Maximum price (inclusive).</param>
    /// <param name="page">The page number (default: 1).</param>
    /// <param name="pageSize">The page size (default: 20, max: 100).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Paged list of products within the specified price range.</returns>
    /// <response code="200">Products by price range returned successfully.</response>
    /// <response code="400">Invalid request parameters.</response>
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
        return result.ToActionResult(
            data => Ok(ApiResponse<CatalogCommon.PaginatedResult<ProductDto>>.Ok(data, "Products by price range")),
            Problem);
    }

    // -------------------------------------------------------------------------
    // Commands
    // -------------------------------------------------------------------------

    /// <summary>
    /// Creates a new product. Requires Admin or SuperAdmin role.
    /// </summary>
    /// <param name="command">Product creation command payload.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Redirect to the created product resource.</returns>
    /// <response code="302">Redirect to product details.</response>
    /// <response code="400">Invalid product data.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="403">User does not have permission to create products.</response>
    /// <response code="409">Product with the same SKU already exists.</response>
    /// <response code="422">Unprocessable product data (validation errors).</response>
    [HttpPost]
    [Authorize(Roles = "Admin,SuperAdmin")]
    [ProducesResponseType(StatusCodes.Status302Found)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> CreateProduct(
        [FromBody] CreateProductCommand command,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(command, ct);
        return result.ToActionResult(
            id => RedirectToAction(nameof(GetProductById), new { id }),
            Problem);
    }

    /// <summary>
    /// Updates an existing product. Requires Admin or SuperAdmin role.
    /// </summary>
    /// <param name="id">The product ID.</param>
    /// <param name="command">Product update command payload.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Redirect to the updated product resource.</returns>
    /// <response code="302">Redirect to product details.</response>
    /// <response code="400">Invalid product data.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="403">User does not have permission to update products.</response>
    /// <response code="404">Product not found.</response>
    /// <response code="422">Unprocessable product data (validation errors).</response>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    [ProducesResponseType(StatusCodes.Status302Found)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> UpdateProduct(
        Guid id,
        [FromBody] UpdateProductCommand command,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(command with { Id = id }, ct);
        return result.ToActionResult(
            productId => RedirectToAction(nameof(GetProductById), new { id = productId }),
            Problem);
    }

    /// <summary>
    /// Deletes a product permanently. Requires Admin or SuperAdmin role.
    /// </summary>
    /// <param name="id">The product ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Deletion result.</returns>
    /// <response code="200">Product deleted successfully.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="403">User does not have permission to delete products.</response>
    /// <response code="404">Product not found.</response>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteProduct(Guid id, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new DeleteProductCommand(id), ct);
        return result.ToActionResult(
            () => Ok(ApiResponse<object>.Ok(new { }, "Product deleted")),
            Problem);
    }

    /// <summary>
    /// Updates the price of a product. Requires Admin or SuperAdmin role.
    /// </summary>
    /// <param name="id">The product ID.</param>
    /// <param name="command">Price update command payload.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Redirect to the updated product resource.</returns>
    /// <response code="302">Redirect to product details.</response>
    /// <response code="400">Invalid price data.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="403">User does not have permission to update price.</response>
    /// <response code="404">Product not found.</response>
    /// <response code="422">Unprocessable price data (validation errors).</response>
    [HttpPut("{id:guid}/price")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    [ProducesResponseType(StatusCodes.Status302Found)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> UpdateProductPrice(
        Guid id,
        [FromBody] UpdateProductPriceCommand command,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(command with { Id = id }, ct);
        return result.ToActionResult(
            productId => RedirectToAction(nameof(GetProductById), new { id = productId }),
            Problem);
    }

    /// <summary>
    /// Updates the stock quantity of a product. Requires Admin or SuperAdmin role.
    /// </summary>
    /// <param name="id">The product ID.</param>
    /// <param name="request">Stock update payload.</param>
    /// <param name="ct">Cancellation token.</param>
    [HttpPut("{id:guid}/stock")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    [ProducesResponseType(typeof(ApiResponse<ProductDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> UpdateProductStock(Guid id, [FromBody] AdjustStockRequest request, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new UpdateProductStockCommand(id, request.Quantity, request.Reason ?? string.Empty), ct);
        return result.ToActionResult(
            () => Ok(ApiResponse<object>.Ok(new { }, "Product stock updated")),
            Problem);
    }

    /// <summary>
    /// Activates a product. Requires Admin or SuperAdmin role.
    /// </summary>
    /// <param name="id">The product ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Activation result.</returns>
    /// <response code="200">Product activated successfully.</response>
    /// <response code="404">Product not found.</response>
    /// <response code="422">Activation cannot be performed (e.g. product is discontinued).</response>
    [HttpPost("{id:guid}/activate")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> ActivateProduct(Guid id, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new ActivateProductCommand(id), ct);
        return result.ToActionResult(
            () => Ok(ApiResponse<object>.Ok(new { }, "Product activated")),
            Problem);
    }

    /// <summary>
    /// Deactivates a product. Requires Admin or SuperAdmin role.
    /// </summary>
    /// <param name="id">The product ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Deactivation result.</returns>
    /// <response code="200">Product deactivated successfully.</response>
    /// <response code="404">Product not found.</response>
    /// <response code="422">Deactivation cannot be performed.</response>
    [HttpPost("{id:guid}/deactivate")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> DeactivateProduct(Guid id, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new DeactivateProductCommand(id), ct);
        return result.ToActionResult(
            () => Ok(ApiResponse<object>.Ok(new { }, "Product deactivated")),
            Problem);
    }

    /// <summary>
    /// Adds an image to a product. Requires Admin or SuperAdmin role.
    /// </summary>
    /// <param name="id">The product ID.</param>
    /// <param name="command">Image addition command payload.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Redirect to the updated product resource.</returns>
    /// <response code="302">Redirect to product details.</response>
    /// <response code="400">Invalid image data.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="403">User does not have permission to add images.</response>
    /// <response code="404">Product not found.</response>
    /// <response code="422">Unprocessable image data (e.g. max images reached).</response>
    [HttpPost("{id:guid}/images")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    [ProducesResponseType(StatusCodes.Status302Found)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> AddProductImage(
        Guid id,
        [FromBody] AddProductImageCommand command,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(command with { ProductId = id }, ct);
        return result.ToActionResult(
            productId => RedirectToAction(nameof(GetProductById), new { id = productId }),
            Problem);
    }

    /// <summary>
    /// Sets the primary image of a product. Requires Admin or SuperAdmin role.
    /// </summary>
    /// <param name="id">The product ID.</param>
    /// <param name="imageId">The image ID to set as primary.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Redirect to the updated product resource.</returns>
    /// <response code="302">Redirect to product details.</response>
    /// <response code="404">Product or image not found.</response>
    [HttpPost("{id:guid}/images/{imageId:guid}/primary")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    [ProducesResponseType(StatusCodes.Status302Found)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetPrimaryImage(Guid id, Guid imageId, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new SetPrimaryImageCommand(id, imageId), ct);
        return result.ToActionResult(
            productId => RedirectToAction(nameof(GetProductById), new { id = productId }),
            Problem);
    }
}



