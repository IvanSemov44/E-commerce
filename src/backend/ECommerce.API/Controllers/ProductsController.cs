using ECommerce.Application.DTOs.Common;
using ECommerce.Application.DTOs.Products;
using ECommerce.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.API.Controllers;

/// <summary>
/// Controller for product management and retrieval operations.
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
    /// Retrieves a paginated list of all products.
    /// </summary>
    /// <param name="page">The page number (default: 1).</param>
    /// <param name="pageSize">The number of items per page (default: 20, max: 100).</param>
    /// <returns>A paginated list of products.</returns>
    /// <response code="200">Products retrieved successfully.</response>
    /// <response code="500">Internal server error.</response>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResult<ProductDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResult<ProductDto>>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<PaginatedResult<ProductDto>>>> GetAllProducts([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        try
        {
            var result = await _productService.GetAllProductsAsync(page, pageSize);
            return Ok(ApiResponse<PaginatedResult<ProductDto>>.Ok(result, "Products retrieved successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all products");
            return StatusCode(500, ApiResponse<PaginatedResult<ProductDto>>.Error("An error occurred while retrieving products"));
        }
    }

    /// <summary>
    /// Retrieves featured products.
    /// </summary>
    /// <param name="page">The page number (default: 1).</param>
    /// <param name="pageSize">The number of items per page (default: 20).</param>
    /// <returns>A list of featured products.</returns>
    /// <response code="200">Featured products retrieved successfully.</response>
    /// <response code="500">Internal server error.</response>
    [HttpGet("featured")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResult<ProductDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResult<ProductDto>>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<PaginatedResult<ProductDto>>>> GetFeaturedProducts([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        try
        {
            var result = await _productService.GetFeaturedProductsAsync(page, pageSize);
            return Ok(ApiResponse<PaginatedResult<ProductDto>>.Ok(result, "Featured products retrieved successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving featured products");
            return StatusCode(500, ApiResponse<PaginatedResult<ProductDto>>.Error("An error occurred while retrieving featured products"));
        }
    }

    /// <summary>
    /// Retrieves a product by its ID.
    /// </summary>
    /// <param name="id">The product ID.</param>
    /// <returns>The product details.</returns>
    /// <response code="200">Product retrieved successfully.</response>
    /// <response code="404">Product not found.</response>
    /// <response code="500">Internal server error.</response>
    [HttpGet("{id}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<ProductDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<ProductDetailDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<ProductDetailDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<ProductDetailDto>>> GetProductById([FromRoute] Guid id)
    {
        try
        {
            var product = await _productService.GetProductByIdAsync(id);
            if (product == null)
            {
                return NotFound(ApiResponse<ProductDetailDto>.Error("Product not found"));
            }

            return Ok(ApiResponse<ProductDetailDto>.Ok(product, "Product retrieved successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving product: {ProductId}", id);
            return StatusCode(500, ApiResponse<ProductDetailDto>.Error("An error occurred while retrieving the product"));
        }
    }

    /// <summary>
    /// Retrieves a product by its slug.
    /// </summary>
    /// <param name="slug">The product slug.</param>
    /// <returns>The product details.</returns>
    /// <response code="200">Product retrieved successfully.</response>
    /// <response code="404">Product not found.</response>
    /// <response code="500">Internal server error.</response>
    [HttpGet("slug/{slug}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<ProductDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<ProductDetailDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<ProductDetailDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<ProductDetailDto>>> GetProductBySlug([FromRoute] string slug)
    {
        try
        {
            var product = await _productService.GetProductBySlugAsync(slug);
            if (product == null)
            {
                return NotFound(ApiResponse<ProductDetailDto>.Error("Product not found"));
            }

            return Ok(ApiResponse<ProductDetailDto>.Ok(product, "Product retrieved successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving product by slug: {Slug}", slug);
            return StatusCode(500, ApiResponse<ProductDetailDto>.Error("An error occurred while retrieving the product"));
        }
    }

    /// <summary>
    /// Searches for products by name or description.
    /// </summary>
    /// <param name="query">The search query.</param>
    /// <param name="page">The page number (default: 1).</param>
    /// <param name="pageSize">The number of items per page (default: 20).</param>
    /// <returns>A paginated list of matching products.</returns>
    /// <response code="200">Products found successfully.</response>
    /// <response code="400">Invalid search query.</response>
    /// <response code="500">Internal server error.</response>
    [HttpGet("search")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResult<ProductDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResult<ProductDto>>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResult<ProductDto>>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<PaginatedResult<ProductDto>>>> SearchProducts([FromQuery] string? query, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return BadRequest(ApiResponse<PaginatedResult<ProductDto>>.Error("Search query is required"));
            }

            var result = await _productService.SearchProductsAsync(query, page, pageSize);
            return Ok(ApiResponse<PaginatedResult<ProductDto>>.Ok(result, "Products found successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching products with query: {Query}", query);
            return StatusCode(500, ApiResponse<PaginatedResult<ProductDto>>.Error("An error occurred while searching products"));
        }
    }

    /// <summary>
    /// Retrieves products by category.
    /// </summary>
    /// <param name="categoryId">The category ID.</param>
    /// <param name="page">The page number (default: 1).</param>
    /// <param name="pageSize">The number of items per page (default: 20).</param>
    /// <returns>A paginated list of products in the category.</returns>
    /// <response code="200">Products retrieved successfully.</response>
    /// <response code="500">Internal server error.</response>
    [HttpGet("category/{categoryId}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResult<ProductDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResult<ProductDto>>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<PaginatedResult<ProductDto>>>> GetProductsByCategory([FromRoute] Guid categoryId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        try
        {
            var result = await _productService.GetProductsByCategoryAsync(categoryId, page, pageSize);
            return Ok(ApiResponse<PaginatedResult<ProductDto>>.Ok(result, "Products retrieved successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving products for category: {CategoryId}", categoryId);
            return StatusCode(500, ApiResponse<PaginatedResult<ProductDto>>.Error("An error occurred while retrieving products"));
        }
    }

    /// <summary>
    /// Filters products by price range.
    /// </summary>
    /// <param name="minPrice">The minimum price.</param>
    /// <param name="maxPrice">The maximum price.</param>
    /// <param name="page">The page number (default: 1).</param>
    /// <param name="pageSize">The number of items per page (default: 20).</param>
    /// <returns>A paginated list of products within the price range.</returns>
    /// <response code="200">Products retrieved successfully.</response>
    /// <response code="500">Internal server error.</response>
    [HttpGet("price-range")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResult<ProductDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResult<ProductDto>>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<PaginatedResult<ProductDto>>>> GetProductsByPriceRange([FromQuery] decimal minPrice, [FromQuery] decimal maxPrice, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        try
        {
            var result = await _productService.GetProductsByPriceRangeAsync(minPrice, maxPrice, page, pageSize);
            return Ok(ApiResponse<PaginatedResult<ProductDto>>.Ok(result, "Products retrieved successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving products by price range: {MinPrice}-{MaxPrice}", minPrice, maxPrice);
            return StatusCode(500, ApiResponse<PaginatedResult<ProductDto>>.Error("An error occurred while retrieving products"));
        }
    }

    /// <summary>
    /// Creates a new product (admin only).
    /// </summary>
    /// <param name="createProductDto">The product creation details.</param>
    /// <returns>The newly created product.</returns>
    /// <response code="201">Product created successfully.</response>
    /// <response code="400">Invalid product data.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="403">User does not have permission to create products.</response>
    /// <response code="500">Internal server error.</response>
    [HttpPost]
    [Authorize(Roles = "Admin,SuperAdmin")]
    [ProducesResponseType(typeof(ApiResponse<ProductDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<ProductDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<ProductDto>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<ProductDto>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<ProductDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<ProductDto>>> CreateProduct([FromBody] CreateProductDto createProductDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                return BadRequest(ApiResponse<ProductDto>.Error("Validation failed", errors));
            }

            var product = await _productService.CreateProductAsync(createProductDto);
            _logger.LogInformation("Product created: {ProductId}", product.Id);

            return CreatedAtAction(nameof(GetProductById), new { id = product.Id }, ApiResponse<ProductDto>.Ok(product, "Product created successfully"));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResponse<ProductDto>.Error(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<ProductDto>.Error(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating product");
            return StatusCode(500, ApiResponse<ProductDto>.Error("An error occurred while creating the product"));
        }
    }

    /// <summary>
    /// Updates an existing product (admin only).
    /// </summary>
    /// <param name="id">The product ID.</param>
    /// <param name="updateProductDto">The updated product details.</param>
    /// <returns>The updated product.</returns>
    /// <response code="200">Product updated successfully.</response>
    /// <response code="400">Invalid product data.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="403">User does not have permission to update products.</response>
    /// <response code="404">Product not found.</response>
    /// <response code="500">Internal server error.</response>
    [HttpPut("{id}")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    [ProducesResponseType(typeof(ApiResponse<ProductDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<ProductDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<ProductDto>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<ProductDto>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<ProductDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<ProductDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<ProductDto>>> UpdateProduct([FromRoute] Guid id, [FromBody] UpdateProductDto updateProductDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                return BadRequest(ApiResponse<ProductDto>.Error("Validation failed", errors));
            }

            var product = await _productService.UpdateProductAsync(id, updateProductDto);
            if (product == null)
            {
                return NotFound(ApiResponse<ProductDto>.Error("Product not found"));
            }

            _logger.LogInformation("Product updated: {ProductId}", id);
            return Ok(ApiResponse<ProductDto>.Ok(product, "Product updated successfully"));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResponse<ProductDto>.Error(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<ProductDto>.Error(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating product: {ProductId}", id);
            return StatusCode(500, ApiResponse<ProductDto>.Error("An error occurred while updating the product"));
        }
    }

    /// <summary>
    /// Deletes a product (admin only).
    /// </summary>
    /// <param name="id">The product ID.</param>
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
    public async Task<ActionResult<ApiResponse<object>>> DeleteProduct([FromRoute] Guid id)
    {
        try
        {
            var success = await _productService.DeleteProductAsync(id);
            if (!success)
            {
                return NotFound(ApiResponse<object>.Error("Product not found"));
            }

            _logger.LogInformation("Product deleted: {ProductId}", id);
            return Ok(ApiResponse<object>.Ok(null, "Product deleted successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting product: {ProductId}", id);
            return StatusCode(500, ApiResponse<object>.Error("An error occurred while deleting the product"));
        }
    }

    /// <summary>
    /// Retrieves products with low stock (admin only).
    /// </summary>
    /// <returns>A list of low stock products.</returns>
    /// <response code="200">Low stock products retrieved successfully.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="403">User does not have permission.</response>
    /// <response code="500">Internal server error.</response>
    [HttpGet("admin/low-stock")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    [ProducesResponseType(typeof(ApiResponse<List<ProductDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<List<ProductDto>>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<List<ProductDto>>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<List<ProductDto>>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<List<ProductDto>>>> GetLowStockProducts()
    {
        try
        {
            var products = await _productService.GetLowStockProductsAsync();
            return Ok(ApiResponse<List<ProductDto>>.Ok(products, "Low stock products retrieved successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving low stock products");
            return StatusCode(500, ApiResponse<List<ProductDto>>.Error("An error occurred while retrieving low stock products"));
        }
    }
}
