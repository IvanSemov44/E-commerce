using ECommerce.API.ActionFilters;
using ECommerce.Application.DTOs.Common;
using ECommerce.Application.DTOs.Products;
using ECommerce.Application.Services;
using ECommerce.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.API.Controllers;

/// <summary>
/// Controller for product management and retrieval operations.
/// Clean controller with no try-catch blocks - global exception handler manages all errors.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class ProductsController : ControllerBase
{
    private readonly IProductService _productService;
    private readonly ILogger<ProductsController> _logger;

    public ProductsController(IProductService productService, ILogger<ProductsController> logger)
    {
        _productService = productService;
        _logger = logger;
    }

    /// <summary>
    /// Retrieves a paginated list of products with optional filtering and sorting.
    /// </summary>
    /// <param name="parameters">Query parameters for paging, filtering and sorting.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A paginated list of products.</returns>
    /// <response code="200">Products retrieved successfully.</response>
    /// <response code="500">Internal server error.</response>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResult<ProductDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<PaginatedResult<ProductDto>>>> GetProducts([FromQuery] ProductQueryParameters parameters, CancellationToken cancellationToken)
    {
        var result = await _productService.GetProductsAsync(parameters, cancellationToken: cancellationToken);
        return Ok(ApiResponse<PaginatedResult<ProductDto>>.Ok(result, "Products retrieved successfully"));
    }

    /// <summary>
    /// Retrieves featured products.
    /// </summary>
    /// <param name="count">The number of featured products to retrieve (default: 10).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of featured products.</returns>
    /// <response code="200">Featured products retrieved successfully.</response>
    /// <response code="500">Internal server error.</response>
    [HttpGet("featured")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<List<ProductDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<List<ProductDto>>>> GetFeaturedProducts([FromQuery] int count = 10, CancellationToken cancellationToken = default)
    {
        var result = await _productService.GetFeaturedProductsAsync(count, cancellationToken: cancellationToken);
        return Ok(ApiResponse<List<ProductDto>>.Ok(result, "Featured products retrieved successfully"));
    }

    /// <summary>
    /// Retrieves a product by its ID.
    /// </summary>
    /// <param name="id">The product ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The product details.</returns>
    /// <response code="200">Product retrieved successfully.</response>
    /// <response code="404">Product not found.</response>
    /// <response code="500">Internal server error.</response>
    [HttpGet("{id}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<ProductDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<ProductDetailDto>>> GetProductById([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var result = await _productService.GetProductByIdAsync(id, cancellationToken: cancellationToken);
        if (!result.IsSuccess)
            return NotFound(ApiResponse<object>.Error(result.Message));
        return Ok(ApiResponse<ProductDetailDto>.Ok(result.Data, "Product retrieved successfully"));
    }

    /// <summary>
    /// Retrieves a product by its slug.
    /// </summary>
    /// <param name="slug">The product slug.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The product details.</returns>
    /// <response code="200">Product retrieved successfully.</response>
    /// <response code="404">Product not found.</response>
    /// <response code="500">Internal server error.</response>
    [HttpGet("slug/{slug}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<ProductDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<ProductDetailDto>>> GetProductBySlug([FromRoute] string slug, CancellationToken cancellationToken)
    {
        var result = await _productService.GetProductBySlugAsync(slug, cancellationToken: cancellationToken);
        if (!result.IsSuccess)
            return NotFound(ApiResponse<object>.Error(result.Message));
        return Ok(ApiResponse<ProductDetailDto>.Ok(result.Data, "Product retrieved successfully"));
    }


    /// <summary>
    /// Creates a new product (admin only).
    /// </summary>
    /// <param name="createProductDto">The product creation details.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The newly created product.</returns>
    /// <response code="201">Product created successfully.</response>
    /// <response code="400">Invalid product data.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="403">User does not have permission to create products.</response>
    /// <response code="409">Product with the same slug already exists.</response>
    /// <response code="500">Internal server error.</response>
    [HttpPost]
    [Authorize(Roles = "Admin,SuperAdmin")]
    [ValidationFilter]
    [ProducesResponseType(typeof(ApiResponse<ProductDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<ProductDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<ProductDto>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<ProductDto>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<ProductDetailDto>>> CreateProduct([FromBody] CreateProductDto createProductDto, CancellationToken cancellationToken)
    {
        var result = await _productService.CreateProductAsync(createProductDto, cancellationToken: cancellationToken);
        if (!result.IsSuccess)
            return Conflict(ApiResponse<object>.Error(result.Message));
        _logger.LogInformation("Product created: {ProductId}", result.Data.Id);

        return CreatedAtAction(nameof(GetProductById), new { id = result.Data.Id },
            ApiResponse<ProductDetailDto>.Ok(result.Data, "Product created successfully"));
    }

    /// <summary>
    /// Updates an existing product (admin only).
    /// </summary>
    /// <param name="id">The product ID.</param>
    /// <param name="updateProductDto">The updated product details.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated product.</returns>
    /// <response code="200">Product updated successfully.</response>
    /// <response code="400">Invalid product data.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="403">User does not have permission to update products.</response>
    /// <response code="404">Product not found.</response>
    /// <response code="409">Product with the same slug already exists.</response>
    /// <response code="500">Internal server error.</response>
    [HttpPut("{id}")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    [ValidationFilter]
    [ProducesResponseType(typeof(ApiResponse<ProductDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<ProductDetailDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<ProductDetailDto>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<ProductDetailDto>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<ProductDetailDto>>> UpdateProduct([FromRoute] Guid id, [FromBody] UpdateProductDto updateProductDto, CancellationToken cancellationToken)
    {
        var result = await _productService.UpdateProductAsync(id, updateProductDto, cancellationToken: cancellationToken);
        if (!result.IsSuccess)
        {
            if (result.Code == "DUPLICATE_PRODUCT_SLUG")
                return Conflict(ApiResponse<object>.Error(result.Message));
            return NotFound(ApiResponse<object>.Error(result.Message));
        }
        _logger.LogInformation("Product updated: {ProductId}", id);

        return Ok(ApiResponse<ProductDetailDto>.Ok(result.Data, "Product updated successfully"));
    }

    /// <summary>
    /// Deletes a product (admin only).
    /// </summary>
    /// <param name="id">The product ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Deletion result.</returns>
    /// <response code="200">Product deleted successfully.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="403">User does not have permission to delete products.</response>
    /// <response code="404">Product not found.</response>
    /// <response code="500">Internal server error.</response>
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<object>>> DeleteProduct([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var result = await _productService.DeleteProductAsync(id, cancellationToken: cancellationToken);
        if (!result.IsSuccess)
            return NotFound(ApiResponse<object>.Error(result.Message));
        _logger.LogInformation("Product deleted: {ProductId}", id);

        return Ok(ApiResponse<object>.Ok(new object(), "Product deleted successfully"));
    }
}

