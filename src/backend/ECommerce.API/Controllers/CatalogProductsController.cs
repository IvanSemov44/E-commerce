using MediatR;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using ECommerce.SharedKernel.Results;
using ECommerce.Application.DTOs.Common;
using ECommerce.Catalog.Application.DTOs.Products;
using CatalogCommon = ECommerce.Catalog.Application.DTOs.Common;
using ECommerce.Catalog.Application.Queries.GetProducts;
using ECommerce.Catalog.Application.Queries.GetProductById;
using ECommerce.Catalog.Application.Queries.GetProductBySlug;
using ECommerce.Catalog.Application.Queries.GetFeaturedProducts;
using ECommerce.Catalog.Application.Queries.SearchProducts;
using ECommerce.Catalog.Application.Queries.GetProductsByCategory;
using ECommerce.Catalog.Application.Queries.GetProductsByPriceRange;
using ECommerce.Catalog.Application.Commands.CreateProduct;
using ECommerce.Catalog.Application.Commands.UpdateProduct;
using ECommerce.Catalog.Application.Commands.UpdateProductPrice;
using ECommerce.Catalog.Application.Commands.ActivateProduct;
using ECommerce.Catalog.Application.Commands.DeactivateProduct;
using ECommerce.Catalog.Application.Commands.AddProductImage;
using ECommerce.Catalog.Application.Commands.SetPrimaryImage;
using ECommerce.API.ActionFilters;

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

    /// <summary>
    /// Retrieves a paginated list of products.
    /// Supports optional paging parameters and returns product summaries.
    /// </summary>
    /// <param name="page">Page number (default: 1).</param>
    /// <param name="pageSize">Page size (default: 20, max: 100).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A paginated result containing product DTOs.</returns>
    /// <response code="200">Products retrieved successfully.</response>
    /// <response code="400">Invalid query or request parameters.</response>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<CatalogCommon.PaginatedResult<ProductDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetProducts([FromQuery, Range(1, int.MaxValue)] int page = 1, [FromQuery, Range(1, 100)] int pageSize = 20, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetProductsQuery(page, pageSize), ct);
        if (result.IsSuccess)
            return Ok(ApiResponse<CatalogCommon.PaginatedResult<ProductDto>>.Ok(result.GetDataOrThrow(), "Products retrieved"));
        return Problem(result.GetErrorOrThrow());
    }

    /// <summary>
    /// Retrieves detailed information for a product by its identifier.
    /// </summary>
    /// <param name="id">Product unique identifier (GUID).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Product details including metadata and images.</returns>
    /// <response code="200">Product retrieved successfully.</response>
    /// <response code="404">Product not found.</response>
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

    /// <summary>
    /// Retrieves product details by URL-friendly slug.
    /// </summary>
    /// <param name="slug">Product slug (URL-friendly identifier).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Product details if a product with the provided slug exists.</returns>
    /// <response code="200">Product retrieved successfully.</response>
    /// <response code="404">Product not found.</response>
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

    /// <summary>
    /// Retrieves a list of featured products.
    /// </summary>
    /// <param name="limit">Maximum number of featured products to return (default: 10).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A collection of product summary DTOs marked as featured.</returns>
    /// <response code="200">Featured products retrieved successfully.</response>
    /// <response code="400">Invalid request parameters.</response>
    [HttpGet("featured")]
    [ProducesResponseType(typeof(ApiResponse<List<ProductDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetFeaturedProducts([FromQuery] int limit = 10, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetFeaturedProductsQuery(limit), ct);
        if (result.IsSuccess)
            return Ok(ApiResponse<List<ProductDto>>.Ok(result.GetDataOrThrow(), "Featured products retrieved"));
        return Problem(result.GetErrorOrThrow());
    }

    /// <summary>
    /// Searches products by text query and returns paginated results.
    /// </summary>
    /// <param name="q">Search query (partial matches supported).</param>
    /// <param name="page">Page number (default: 1).</param>
    /// <param name="pageSize">Page size (default: 20, max: 100).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Paginated list of products matching the search criteria.</returns>
    /// <response code="200">Search results returned successfully.</response>
    /// <response code="400">Invalid search parameters.</response>
    [HttpGet("search")]
    [ProducesResponseType(typeof(ApiResponse<CatalogCommon.PaginatedResult<ProductDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SearchProducts([FromQuery] string? q, [FromQuery, Range(1, int.MaxValue)] int page = 1, [FromQuery, Range(1, 100)] int pageSize = 20, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new SearchProductsQuery(q ?? string.Empty, page, pageSize), ct);
        if (result.IsSuccess)
            return Ok(ApiResponse<CatalogCommon.PaginatedResult<ProductDto>>.Ok(result.GetDataOrThrow(), "Search results"));
        return Problem(result.GetErrorOrThrow());
    }

    /// <summary>
    /// Retrieves products for a specific category (paged).
    /// </summary>
    /// <param name="categoryId">Category identifier (GUID).</param>
    /// <param name="page">Page number (default: 1).</param>
    /// <param name="pageSize">Page size (default: 20, max: 100).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Paginated list of products in the specified category.</returns>
    /// <response code="200">Products for category returned successfully.</response>
    /// <response code="400">Invalid parameters.</response>
    [HttpGet("by-category/{categoryId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<CatalogCommon.PaginatedResult<ProductDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetProductsByCategory(Guid categoryId, [FromQuery, Range(1, int.MaxValue)] int page = 1, [FromQuery, Range(1, 100)] int pageSize = 20, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetProductsByCategoryQuery(categoryId, page, pageSize), ct);
        if (result.IsSuccess)
            return Ok(ApiResponse<CatalogCommon.PaginatedResult<ProductDto>>.Ok(result.GetDataOrThrow(), "Products by category"));
        return Problem(result.GetErrorOrThrow());
    }

    /// <summary>
    /// Retrieves products whose prices fall within the specified range (paged).
    /// </summary>
    /// <param name="min">Minimum price (inclusive).</param>
    /// <param name="max">Maximum price (inclusive).</param>
    /// <param name="page">Page number (default: 1).</param>
    /// <param name="pageSize">Page size (default: 20, max: 100).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Paginated list of products matching the price range.</returns>
    /// <response code="200">Products by price range returned successfully.</response>
    /// <response code="400">Invalid price range or parameters.</response>
    [HttpGet("by-price")]
    [ProducesResponseType(typeof(ApiResponse<CatalogCommon.PaginatedResult<ProductDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetProductsByPriceRange([FromQuery] decimal min, [FromQuery] decimal max, [FromQuery, Range(1, int.MaxValue)] int page = 1, [FromQuery, Range(1, 100)] int pageSize = 20, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetProductsByPriceRangeQuery(min, max, page, pageSize), ct);
        if (result.IsSuccess)
            return Ok(ApiResponse<CatalogCommon.PaginatedResult<ProductDto>>.Ok(result.GetDataOrThrow(), "Products by price range"));
        return Problem(result.GetErrorOrThrow());
    }

    /// <summary>
    /// Creates a new product. Requires `Admin` or `SuperAdmin` role.
    /// </summary>
    /// <param name="command">Create product command containing product details.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The created product details.</returns>
    /// <response code="201">Product created successfully.</response>
    /// <response code="400">Invalid product data.</response>
    /// <response code="422">Business validation failed (e.g., duplicate slug).</response>
    [HttpPost]
    [Authorize(Roles = "Admin,SuperAdmin")]
    [ValidationFilter]
    [ProducesResponseType(typeof(ApiResponse<ProductDetailDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> CreateProduct([FromBody] CreateProductCommand command, CancellationToken ct = default)
    {
        var result = await _mediator.Send(command, ct);
        if (result.IsSuccess)
        {
            var dto = result.GetDataOrThrow();
            return CreatedAtAction(nameof(GetProductById), new { id = dto.Id }, ApiResponse<ProductDetailDto>.Ok(dto, "Product created"));
        }
        return Problem(result.GetErrorOrThrow());
    }

    /// <summary>
    /// Updates an existing product. Requires `Admin` or `SuperAdmin` role.
    /// </summary>
    /// <param name="id">Product identifier (GUID) to update.</param>
    /// <param name="command">Update command with new product values.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The updated product details.</returns>
    /// <response code="200">Product updated successfully.</response>
    /// <response code="404">Product not found.</response>
    /// <response code="400">Invalid request data.</response>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    [ValidationFilter]
    [ProducesResponseType(typeof(ApiResponse<ProductDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateProduct(Guid id, [FromBody] UpdateProductCommand command, CancellationToken ct = default)
    {
        var result = await _mediator.Send(command with { Id = id }, ct);
        if (result.IsSuccess)
            return Ok(ApiResponse<ProductDetailDto>.Ok(result.GetDataOrThrow(), "Product updated"));
        return Problem(result.GetErrorOrThrow());
    }

    /// <summary>
    /// Deletes a product permanently. Requires `Admin` or `SuperAdmin` role.
    /// </summary>
    /// <param name="id">Product identifier (GUID) to delete.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Deletion acknowledgement.</returns>
    /// <response code="200">Product deleted successfully.</response>
    /// <response code="404">Product not found.</response>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteProduct(Guid id, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new ECommerce.Catalog.Application.Commands.DeleteProduct.DeleteProductCommand(id), ct);
        if (result.IsSuccess)
            return Ok(ApiResponse<object>.Ok(new object(), "Product deleted"));
        return Problem(result.GetErrorOrThrow());
    }

    /// <summary>
    /// Updates the price of an existing product. Requires `Admin` or `SuperAdmin` role.
    /// </summary>
    /// <param name="id">Product identifier (GUID) whose price is updated.</param>
    /// <param name="command">Command containing the new price.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The product with updated price.</returns>
    /// <response code="200">Price updated successfully.</response>
    /// <response code="404">Product not found.</response>
    [HttpPut("{id:guid}/price")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    [ValidationFilter]
    [ProducesResponseType(typeof(ApiResponse<ProductDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateProductPrice(Guid id, [FromBody] UpdateProductPriceCommand command, CancellationToken ct = default)
    {
        var result = await _mediator.Send(command with { Id = id }, ct);
        if (result.IsSuccess)
            return Ok(ApiResponse<ProductDetailDto>.Ok(result.GetDataOrThrow(), "Price updated"));
        return Problem(result.GetErrorOrThrow());
    }

    /// <summary>
    /// Sets a product to active state. Requires `Admin` or `SuperAdmin` role.
    /// </summary>
    /// <param name="id">Product identifier (GUID) to activate.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Acknowledgement of activation.</returns>
    /// <response code="200">Product activated successfully.</response>
    /// <response code="404">Product not found.</response>
    [HttpPost("{id:guid}/activate")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ActivateProduct(Guid id, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new ActivateProductCommand(id), ct);
        if (result.IsSuccess)
            return Ok(ApiResponse<object>.Ok(new object(), "Product activated"));
        return Problem(result.GetErrorOrThrow());
    }

    /// <summary>
    /// Sets a product to inactive state. Requires `Admin` or `SuperAdmin` role.
    /// </summary>
    /// <param name="id">Product identifier (GUID) to deactivate.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Acknowledgement of deactivation.</returns>
    /// <response code="200">Product deactivated successfully.</response>
    /// <response code="404">Product not found.</response>
    [HttpPost("{id:guid}/deactivate")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeactivateProduct(Guid id, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new DeactivateProductCommand(id), ct);
        if (result.IsSuccess)
            return Ok(ApiResponse<object>.Ok(new object(), "Product deactivated"));
        return Problem(result.GetErrorOrThrow());
    }

    /// <summary>
    /// Adds an image to the specified product. Requires `Admin` or `SuperAdmin` role.
    /// </summary>
    /// <param name="id">Product identifier (GUID) to attach the image to.</param>
    /// <param name="command">Command containing image details (URL, metadata).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The updated product details including the new image.</returns>
    /// <response code="200">Image added and product returned.</response>
    /// <response code="404">Product or image resource not found.</response>
    [HttpPost("{id:guid}/images")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    [ValidationFilter]
    [ProducesResponseType(typeof(ApiResponse<ProductDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddProductImage(Guid id, [FromBody] AddProductImageCommand command, CancellationToken ct = default)
    {
        var result = await _mediator.Send(command with { ProductId = id }, ct);
        if (result.IsSuccess)
            return Ok(ApiResponse<ProductDetailDto>.Ok(result.GetDataOrThrow(), "Image added"));
        return Problem(result.GetErrorOrThrow());
    }

    /// <summary>
    /// Marks a product image as the primary image. Requires `Admin` or `SuperAdmin` role.
    /// </summary>
    /// <param name="id">Product identifier (GUID).</param>
    /// <param name="imageId">Image identifier (GUID) to set as primary.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The product details with the primary image updated.</returns>
    /// <response code="200">Primary image set successfully.</response>
    /// <response code="404">Product or image not found.</response>
    [HttpPost("{id:guid}/images/{imageId:guid}/primary")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    [ProducesResponseType(typeof(ApiResponse<ProductDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetPrimaryImage(Guid id, Guid imageId, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new SetPrimaryImageCommand(id, imageId), ct);
        if (result.IsSuccess)
            return Ok(ApiResponse<ProductDetailDto>.Ok(result.GetDataOrThrow(), "Primary image set"));
        return Problem(result.GetErrorOrThrow());
    }
}
