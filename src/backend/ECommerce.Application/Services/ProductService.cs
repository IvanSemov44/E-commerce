using System.Threading;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using ECommerce.Application.DTOs.Common;
using ECommerce.Application.DTOs.Products;
using ECommerce.Application.Interfaces;
using ECommerce.Core.Entities;
using ECommerce.Core.Exceptions;
using ECommerce.Core.Interfaces.Repositories;
using ECommerce.Core.Results;
using ECommerce.Core.Constants;
using Microsoft.Extensions.Logging;

namespace ECommerce.Application.Services;

/// <summary>
/// Service for managing product operations.
/// All async methods support CancellationToken for graceful cancellation.
/// </summary>
public class ProductService : IProductService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<ProductService> _logger;

    public ProductService(IUnitOfWork unitOfWork, IMapper mapper, ILogger<ProductService> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<PaginatedResult<ProductDto>> GetProductsAsync(ProductQueryParameters parameters, CancellationToken cancellationToken = default)
    {
        // Validate and cap page size to prevent DoS
        var effectivePageSize = Math.Min(parameters.PageSize, PaginationConstants.MaxPageSize);

        var (products, totalCount) = await _unitOfWork.Products.GetProductsWithFiltersAsync(
            parameters.GetSkip(),
            effectivePageSize,
            parameters.CategoryId,
            parameters.Search,
            parameters.MinPrice,
            parameters.MaxPrice,
            parameters.MinRating,
            parameters.IsFeatured,
            parameters.SortBy,
            cancellationToken: cancellationToken);

        return new PaginatedResult<ProductDto>
        {
            Items = products.Select(p => _mapper.Map<ProductDto>(p)).ToList(),
            TotalCount = totalCount,
            Page = parameters.Page,
            PageSize = effectivePageSize
        };
    }

    public async Task<Result<ProductDetailDto>> GetProductBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        var product = await _unitOfWork.Products.GetBySlugAsync(slug, cancellationToken: cancellationToken);
        if (product == null)
            return Result<ProductDetailDto>.Fail(ErrorCodes.ProductNotFound, $"Product with slug '{slug}' not found");

        return Result<ProductDetailDto>.Ok(_mapper.Map<ProductDetailDto>(product));
    }

    public async Task<Result<ProductDetailDto>> GetProductByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var product = await _unitOfWork.Products.GetByIdAsync(id, trackChanges: false, cancellationToken: cancellationToken);
        if (product == null)
            return Result<ProductDetailDto>.Fail(ErrorCodes.ProductNotFound, $"Product with id '{id}' not found");

        return Result<ProductDetailDto>.Ok(_mapper.Map<ProductDetailDto>(product));
    }

    public async Task<Result<ProductDetailDto>> CreateProductAsync(CreateProductDto dto, CancellationToken cancellationToken = default)
    {
        if (!await _unitOfWork.Products.IsSlugUniqueAsync(dto.Slug, cancellationToken: cancellationToken))
            return Result<ProductDetailDto>.Fail(ErrorCodes.DuplicateProductSlug, $"Slug '{dto.Slug}' already exists");

        var product = _mapper.Map<Product>(dto);
        product.IsActive = true;

        await _unitOfWork.Products.AddAsync(product, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<ProductDetailDto>.Ok(_mapper.Map<ProductDetailDto>(product));
    }

    public async Task<Result<ProductDetailDto>> UpdateProductAsync(Guid id, UpdateProductDto dto, CancellationToken cancellationToken = default)
    {
        var product = await _unitOfWork.Products.GetByIdAsync(id, trackChanges: true, cancellationToken: cancellationToken);
        if (product == null)
            return Result<ProductDetailDto>.Fail(ErrorCodes.ProductNotFound, $"Product with id '{id}' not found");

        if (!string.IsNullOrEmpty(dto.Slug) && dto.Slug != product.Slug)
        {
            if (!await _unitOfWork.Products.IsSlugUniqueAsync(dto.Slug, id, cancellationToken))
                return Result<ProductDetailDto>.Fail(ErrorCodes.DuplicateProductSlug, $"Slug '{dto.Slug}' already exists");
        }

        _mapper.Map(dto, product);
        product.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.Products.UpdateAsync(product, cancellationToken);
        try
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogWarning(ex, "Concurrency conflict while updating product {ProductId}", id);
            return Result<ProductDetailDto>.Fail(ErrorCodes.ConcurrencyConflict, "Product was modified by another request. Please refresh and try again.");
        }

        return Result<ProductDetailDto>.Ok(_mapper.Map<ProductDetailDto>(product));
    }

    public Task<PaginatedResult<ProductDto>> GetAllProductsAsync(int page = 1, int pageSize = PaginationConstants.DefaultPageSize, CancellationToken cancellationToken = default)
    {
        var parameters = new ProductQueryParameters { Page = page, PageSize = pageSize };
        return GetProductsAsync(parameters, cancellationToken);
    }

    public async Task<PaginatedResult<ProductDto>> GetFeaturedProductsAsync(int page = 1, int pageSize = PaginationConstants.DefaultPageSize, CancellationToken cancellationToken = default)
    {
        var effectivePageSize = Math.Min(pageSize, PaginationConstants.MaxPageSize);
        var totalCount = await _unitOfWork.Products.GetFeaturedProductsCountAsync(cancellationToken);
        var skip = (page - 1) * effectivePageSize;
        var products = await _unitOfWork.Products.GetFeaturedAsync(skip, effectivePageSize, cancellationToken: cancellationToken);

        return new PaginatedResult<ProductDto>
        {
            Items = products.Select(p => _mapper.Map<ProductDto>(p)).ToList(),
            TotalCount = totalCount,
            Page = page,
            PageSize = effectivePageSize
        };
    }

    public async Task<PaginatedResult<ProductDto>> SearchProductsAsync(string query, int page = 1, int pageSize = PaginationConstants.DefaultPageSize, CancellationToken cancellationToken = default)
    {
        var effectivePageSize = Math.Min(pageSize, PaginationConstants.MaxPageSize);
        var skip = (page - 1) * effectivePageSize;

        var searchPattern = $"%{query}%";
        var searchQuery = _unitOfWork.Products
            .FindByCondition(p => p.IsActive &&
                (EF.Functions.Like(p.Name, searchPattern) ||
                 (p.Description != null && EF.Functions.Like(p.Description, searchPattern)) ||
                 (p.Sku != null && EF.Functions.Like(p.Sku, searchPattern))),
                trackChanges: false)
            .Include(p => p.Images)
            .Include(p => p.Category);

        var totalCount = await searchQuery.CountAsync(cancellationToken);
        var products = await searchQuery
            .Skip(skip)
            .Take(effectivePageSize)
            .ToListAsync(cancellationToken);

        return new PaginatedResult<ProductDto>
        {
            Items = products.Select(p => _mapper.Map<ProductDto>(p)).ToList(),
            TotalCount = totalCount,
            Page = page,
            PageSize = effectivePageSize
        };
    }

    public async Task<PaginatedResult<ProductDto>> GetProductsByCategoryAsync(Guid categoryId, int page = 1, int pageSize = PaginationConstants.DefaultPageSize, CancellationToken cancellationToken = default)
    {
        var effectivePageSize = Math.Min(pageSize, PaginationConstants.MaxPageSize);
        var skip = (page - 1) * effectivePageSize;

        var query = _unitOfWork.Products
            .FindByCondition(p => p.CategoryId == categoryId && p.IsActive, trackChanges: false)
            .Include(p => p.Images)
            .Include(p => p.Category);

        var totalCount = await query.CountAsync(cancellationToken);

        var products = await query
            .Skip(skip)
            .Take(effectivePageSize)
            .ToListAsync(cancellationToken);

        return new PaginatedResult<ProductDto>
        {
            Items = products.Select(p => _mapper.Map<ProductDto>(p)).ToList(),
            TotalCount = totalCount,
            Page = page,
            PageSize = effectivePageSize
        };
    }

    public async Task<PaginatedResult<ProductDto>> GetProductsByPriceRangeAsync(decimal minPrice, decimal maxPrice, int page = 1, int pageSize = PaginationConstants.DefaultPageSize, CancellationToken cancellationToken = default)
    {
        var effectivePageSize = Math.Min(pageSize, PaginationConstants.MaxPageSize);
        var skip = (page - 1) * effectivePageSize;

        var query = _unitOfWork.Products
            .FindByCondition(p => p.IsActive && p.Price >= minPrice && p.Price <= maxPrice, trackChanges: false)
            .Include(p => p.Images)
            .Include(p => p.Category);

        var totalCount = await query.CountAsync(cancellationToken);

        var products = await query
            .Skip(skip)
            .Take(effectivePageSize)
            .ToListAsync(cancellationToken);

        return new PaginatedResult<ProductDto>
        {
            Items = products.Select(p => _mapper.Map<ProductDto>(p)).ToList(),
            TotalCount = totalCount,
            Page = page,
            PageSize = effectivePageSize
        };
    }

    public async Task<List<ProductDto>> GetLowStockProductsAsync(CancellationToken cancellationToken = default)
    {
        var lowStockProducts = await _unitOfWork.Products
            .FindByCondition(p => p.StockQuantity <= p.LowStockThreshold && p.IsActive, trackChanges: false)
            .Include(p => p.Images)
            .Include(p => p.Category)
            .ToListAsync(cancellationToken);

        return lowStockProducts.Select(p => _mapper.Map<ProductDto>(p)).ToList();
    }

    public async Task<Result<Unit>> DeleteProductAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var product = await _unitOfWork.Products.GetByIdAsync(id, trackChanges: true, cancellationToken: cancellationToken);
        if (product == null)
            return Result<Unit>.Fail(ErrorCodes.ProductNotFound, $"Product with id '{id}' not found");

        await _unitOfWork.Products.DeleteAsync(product, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<Unit>.Ok(new Unit());
    }
}
