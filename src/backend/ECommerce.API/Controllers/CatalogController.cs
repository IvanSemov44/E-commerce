using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ECommerce.SharedKernel.Results;
using ECommerce.Application.DTOs.Common;
using ECommerce.Catalog.Application.DTOs.Products;
using CatalogCommon = ECommerce.Catalog.Application.DTOs.Common;
using CatalogCategory = ECommerce.Catalog.Application.DTOs.Categories.CategoryDto;
using ECommerce.Catalog.Application.Queries.GetProducts;
using ECommerce.Catalog.Application.Queries.GetProductById;
using ECommerce.Catalog.Application.Queries.GetProductBySlug;
using ECommerce.Catalog.Application.Queries.GetFeaturedProducts;
using ECommerce.Catalog.Application.Queries.SearchProducts;
using ECommerce.Catalog.Application.Queries.GetProductsByCategory;
using ECommerce.Catalog.Application.Queries.GetProductsByPriceRange;
using ECommerce.Catalog.Application.Queries.GetCategories;
using ECommerce.Catalog.Application.Queries.GetCategoryById;
using ECommerce.Catalog.Application.Queries.GetCategoryBySlug;
using ECommerce.Catalog.Application.Commands.CreateProduct;
using ECommerce.Catalog.Application.Commands.UpdateProduct;
using ECommerce.Catalog.Application.Commands.UpdateProductPrice;
using ECommerce.Catalog.Application.Commands.ActivateProduct;
using ECommerce.Catalog.Application.Commands.DeactivateProduct;
using ECommerce.Catalog.Application.Commands.AddProductImage;
using ECommerce.Catalog.Application.Commands.SetPrimaryImage;
using ECommerce.Catalog.Application.Commands.CreateCategory;
using ECommerce.Catalog.Application.Commands.UpdateCategory;
using ECommerce.Catalog.Application.Commands;
using ECommerce.Catalog.Application.Commands.DeleteCategory;
using ECommerce.API.ActionFilters;

namespace ECommerce.API.Controllers;

[ApiController]
[Route("api/catalog")]
public class CatalogController(IMediator mediator) : ControllerBase
{
    private readonly IMediator _mediator = mediator;

    private IActionResult Problem(DomainError error) => error.Code switch
    {
        "PRODUCT_NOT_FOUND" or "CATEGORY_NOT_FOUND" or "IMAGE_NOT_FOUND"
            => NotFound(ApiResponse<object>.Failure(error.Message, error.Code)),
        _ => BadRequest(ApiResponse<object>.Failure(error.Message, error.Code))
    };

    [HttpGet("products")]
    public async Task<IActionResult> GetProducts([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetProductsQuery(page, pageSize), ct);
        if (result.IsSuccess)
            return Ok(ApiResponse<CatalogCommon.PaginatedResult<ProductDto>>.Ok(result.GetDataOrThrow(), "Products retrieved"));
        return Problem(result.GetErrorOrThrow());
    }

    [HttpGet("products/{id:guid}")]
    public async Task<IActionResult> GetProductById(Guid id, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetProductByIdQuery(id), ct);
        if (result.IsSuccess)
            return Ok(ApiResponse<ProductDetailDto>.Ok(result.GetDataOrThrow(), "Product retrieved"));
        return Problem(result.GetErrorOrThrow());
    }

    [HttpGet("products/slug/{slug}")]
    public async Task<IActionResult> GetProductBySlug(string slug, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetProductBySlugQuery(slug), ct);
        if (result.IsSuccess)
            return Ok(ApiResponse<ProductDetailDto>.Ok(result.GetDataOrThrow(), "Product retrieved"));
        return Problem(result.GetErrorOrThrow());
    }

    [HttpGet("products/featured")]
    public async Task<IActionResult> GetFeaturedProducts([FromQuery] int limit = 10, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetFeaturedProductsQuery(limit), ct);
        if (result.IsSuccess)
            return Ok(ApiResponse<List<ProductDto>>.Ok(result.GetDataOrThrow(), "Featured products retrieved"));
        return Problem(result.GetErrorOrThrow());
    }

    [HttpGet("products/search")]
    public async Task<IActionResult> SearchProducts([FromQuery] string q, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new SearchProductsQuery(q, page, pageSize), ct);
        if (result.IsSuccess)
            return Ok(ApiResponse<CatalogCommon.PaginatedResult<ProductDto>>.Ok(result.GetDataOrThrow(), "Search results"));
        return Problem(result.GetErrorOrThrow());
    }

    [HttpGet("products/by-category/{categoryId:guid}")]
    public async Task<IActionResult> GetProductsByCategory(Guid categoryId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetProductsByCategoryQuery(categoryId, page, pageSize), ct);
        if (result.IsSuccess)
            return Ok(ApiResponse<CatalogCommon.PaginatedResult<ProductDto>>.Ok(result.GetDataOrThrow(), "Products by category"));
        return Problem(result.GetErrorOrThrow());
    }

    [HttpGet("products/by-price")]
    public async Task<IActionResult> GetProductsByPriceRange([FromQuery] decimal min, [FromQuery] decimal max, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetProductsByPriceRangeQuery(min, max, page, pageSize), ct);
        if (result.IsSuccess)
            return Ok(ApiResponse<CatalogCommon.PaginatedResult<ProductDto>>.Ok(result.GetDataOrThrow(), "Products by price range"));
        return Problem(result.GetErrorOrThrow());
    }

    [HttpPost("products")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    [ValidationFilter]
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

    [HttpPut("products/{id:guid}")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    [ValidationFilter]
    public async Task<IActionResult> UpdateProduct(Guid id, [FromBody] UpdateProductCommand command, CancellationToken ct = default)
    {
        var result = await _mediator.Send(command with { Id = id }, ct);
        if (result.IsSuccess)
            return Ok(ApiResponse<ProductDetailDto>.Ok(result.GetDataOrThrow(), "Product updated"));
        return Problem(result.GetErrorOrThrow());
    }

    [HttpDelete("products/{id:guid}")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> DeleteProduct(Guid id, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new ECommerce.Catalog.Application.Commands.DeleteProduct.DeleteProductCommand(id), ct);
        if (result.IsSuccess)
            return Ok(ApiResponse<object>.Ok(new object(), "Product deleted"));
        return Problem(result.GetErrorOrThrow());
    }

    [HttpPut("products/{id:guid}/price")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    [ValidationFilter]
    public async Task<IActionResult> UpdateProductPrice(Guid id, [FromBody] UpdateProductPriceCommand command, CancellationToken ct = default)
    {
        var result = await _mediator.Send(command with { Id = id }, ct);
        if (result.IsSuccess)
            return Ok(ApiResponse<ProductDetailDto>.Ok(result.GetDataOrThrow(), "Price updated"));
        return Problem(result.GetErrorOrThrow());
    }

    [HttpPost("products/{id:guid}/activate")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> ActivateProduct(Guid id, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new ActivateProductCommand(id), ct);
        if (result.IsSuccess)
            return Ok(ApiResponse<object>.Ok(new object(), "Product activated"));
        return Problem(result.GetErrorOrThrow());
    }

    [HttpPost("products/{id:guid}/deactivate")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> DeactivateProduct(Guid id, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new DeactivateProductCommand(id), ct);
        if (result.IsSuccess)
            return Ok(ApiResponse<object>.Ok(new object(), "Product deactivated"));
        return Problem(result.GetErrorOrThrow());
    }

    [HttpPost("products/{id:guid}/images")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    [ValidationFilter]
    public async Task<IActionResult> AddProductImage(Guid id, [FromBody] AddProductImageCommand command, CancellationToken ct = default)
    {
        var result = await _mediator.Send(command with { ProductId = id }, ct);
        if (result.IsSuccess)
            return Ok(ApiResponse<ProductDetailDto>.Ok(result.GetDataOrThrow(), "Image added"));
        return Problem(result.GetErrorOrThrow());
    }

    [HttpPost("products/{id:guid}/images/{imageId:guid}/primary")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> SetPrimaryImage(Guid id, Guid imageId, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new SetPrimaryImageCommand(id, imageId), ct);
        if (result.IsSuccess)
            return Ok(ApiResponse<ProductDetailDto>.Ok(result.GetDataOrThrow(), "Primary image set"));
        return Problem(result.GetErrorOrThrow());
    }

    [HttpGet("categories")]
    public async Task<IActionResult> GetCategories(CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetCategoriesQuery(), ct);
        if (result.IsSuccess)
            return Ok(ApiResponse<IEnumerable<CatalogCategory>>.Ok(result.GetDataOrThrow(), "Categories retrieved"));
        return Problem(result.GetErrorOrThrow());
    }

    [HttpGet("categories/{id:guid}")]
    public async Task<IActionResult> GetCategoryById(Guid id, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetCategoryByIdQuery(id), ct);
        if (result.IsSuccess)
            return Ok(ApiResponse<CatalogCategory>.Ok(result.GetDataOrThrow(), "Category retrieved"));
        return Problem(result.GetErrorOrThrow());
    }

    [HttpGet("categories/slug/{slug}")]
    public async Task<IActionResult> GetCategoryBySlug(string slug, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetCategoryBySlugQuery(slug), ct);
        if (result.IsSuccess)
            return Ok(ApiResponse<CatalogCategory>.Ok(result.GetDataOrThrow(), "Category retrieved"));
        return Problem(result.GetErrorOrThrow());
    }

    [HttpPost("categories")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    [ValidationFilter]
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

    [HttpPut("categories/{id:guid}")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    [ValidationFilter]
    public async Task<IActionResult> UpdateCategory(Guid id, [FromBody] UpdateCategoryCommand command, CancellationToken ct = default)
    {
        var result = await _mediator.Send(command with { Id = id }, ct);
        if (result.IsSuccess)
            return Ok(ApiResponse<CatalogCategory>.Ok(result.GetDataOrThrow(), "Category updated"));
        return Problem(result.GetErrorOrThrow());
    }

    [HttpDelete("categories/{id:guid}")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> DeleteCategory(Guid id, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new DeleteCategoryCommand(id), ct);
        if (result.IsSuccess)
            return Ok(ApiResponse<object>.Ok(new object(), "Category deleted"));
        return Problem(result.GetErrorOrThrow());
    }
}
