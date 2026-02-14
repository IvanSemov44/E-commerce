using System.Threading;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using ECommerce.Application.DTOs.Common;
using ECommerce.Application.DTOs.Products;
using ECommerce.Application.Interfaces;
using ECommerce.Core.Entities;
using ECommerce.Core.Exceptions;
using ECommerce.Core.Interfaces.Repositories;

namespace ECommerce.Application.Services;

/// <summary>
/// Service for managing product operations.
/// All async methods support CancellationToken for graceful cancellation.
/// </summary>
public class ProductService : IProductService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public ProductService(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<PaginatedResult<ProductDto>> GetProductsAsync(ProductQueryParameters parameters, CancellationToken cancellationToken = default)
    {
        var (products, totalCount) = await _unitOfWork.Products.GetProductsWithFiltersAsync(
            parameters.GetSkip(),
            parameters.PageSize,
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
            PageSize = parameters.PageSize
        };
    }

    public async Task<ProductDetailDto> GetProductBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        var product = await _unitOfWork.Products.GetBySlugAsync(slug, cancellationToken: cancellationToken);
        if (product == null)
            throw new ProductNotFoundException(slug);

        return _mapper.Map<ProductDetailDto>(product);
    }

    public async Task<ProductDetailDto> GetProductByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var product = await _unitOfWork.Products.GetByIdAsync(id, cancellationToken: cancellationToken);
        if (product == null)
            throw new ProductNotFoundException(id);

        return _mapper.Map<ProductDetailDto>(product);
    }

    public async Task<List<ProductDto>> GetFeaturedProductsAsync(int count = 10, CancellationToken cancellationToken = default)
    {
        var products = await _unitOfWork.Products.GetFeaturedAsync(count, cancellationToken: cancellationToken);
        return products.Select(p => _mapper.Map<ProductDto>(p)).ToList();
    }

    public async Task<ProductDetailDto> CreateProductAsync(CreateProductDto dto, CancellationToken cancellationToken = default)
    {
        if (!await _unitOfWork.Products.IsSlugUniqueAsync(dto.Slug, cancellationToken: cancellationToken))
            throw new DuplicateProductSlugException(dto.Slug);

        var product = _mapper.Map<Product>(dto);
        product.IsActive = true;

        await _unitOfWork.Products.AddAsync(product, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return _mapper.Map<ProductDetailDto>(product);
    }

    public async Task<ProductDetailDto> UpdateProductAsync(Guid id, UpdateProductDto dto, CancellationToken cancellationToken = default)
    {
        var product = await _unitOfWork.Products.GetByIdAsync(id, trackChanges: true, cancellationToken: cancellationToken);
        if (product == null)
            throw new ProductNotFoundException(id);

        if (!string.IsNullOrEmpty(dto.Slug) && dto.Slug != product.Slug)
        {
            if (!await _unitOfWork.Products.IsSlugUniqueAsync(dto.Slug, id, cancellationToken))
                throw new DuplicateProductSlugException(dto.Slug);
        }

        _mapper.Map(dto, product);
        product.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.Products.UpdateAsync(product, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return _mapper.Map<ProductDetailDto>(product);
    }

    public async Task<PaginatedResult<ProductDto>> GetAllProductsAsync(int page = 1, int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var parameters = new ProductQueryParameters { Page = page, PageSize = pageSize };
        return await GetProductsAsync(parameters, cancellationToken);
    }

    public async Task<PaginatedResult<ProductDto>> GetFeaturedProductsAsync(int page = 1, int pageSize = 20)
    {
        var totalCount = await _unitOfWork.Products.GetActiveProductsCountAsync();
        var skip = (page - 1) * pageSize;
        var products = await _unitOfWork.Products.GetFeaturedAsync(pageSize);

        return new PaginatedResult<ProductDto>
        {
            Items = products.Select(p => _mapper.Map<ProductDto>(p)).ToList(),
            TotalCount = products.Count(),
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<PaginatedResult<ProductDto>> SearchProductsAsync(string query, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var skip = (page - 1) * pageSize;

        // Push filtering to database instead of loading all products into memory
        var queryLower = query.ToLower();
        var searchQuery = _unitOfWork.Products
            .FindByCondition(p => p.IsActive &&
                (EF.Functions.Like(p.Name.ToLower(), $"%{queryLower}%") ||
                 (p.Description != null && EF.Functions.Like(p.Description.ToLower(), $"%{queryLower}%")) ||
                 (p.Sku != null && EF.Functions.Like(p.Sku.ToLower(), $"%{queryLower}%"))),
                trackChanges: false);

        var totalCount = await searchQuery.CountAsync(cancellationToken);
        var products = await searchQuery
            .Skip(skip)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PaginatedResult<ProductDto>
        {
            Items = products.Select(p => _mapper.Map<ProductDto>(p)).ToList(),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<PaginatedResult<ProductDto>> GetProductsByCategoryAsync(Guid categoryId, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var skip = (page - 1) * pageSize;

        var query = _unitOfWork.Products
            .FindByCondition(p => p.CategoryId == categoryId && p.IsActive, trackChanges: false);

        var totalCount = await query.CountAsync(cancellationToken);

        var products = await query
            .Skip(skip)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PaginatedResult<ProductDto>
        {
            Items = products.Select(p => _mapper.Map<ProductDto>(p)).ToList(),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<PaginatedResult<ProductDto>> GetProductsByPriceRangeAsync(decimal minPrice, decimal maxPrice, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var skip = (page - 1) * pageSize;

        var query = _unitOfWork.Products
            .FindByCondition(p => p.IsActive && p.Price >= minPrice && p.Price <= maxPrice, trackChanges: false);

        var totalCount = await query.CountAsync(cancellationToken);

        var products = await query
            .Skip(skip)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PaginatedResult<ProductDto>
        {
            Items = products.Select(p => _mapper.Map<ProductDto>(p)).ToList(),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<List<ProductDto>> GetLowStockProductsAsync(CancellationToken cancellationToken = default)
    {
        var lowStockProducts = await _unitOfWork.Products
            .FindByCondition(p => p.StockQuantity <= p.LowStockThreshold && p.IsActive, trackChanges: false)
            .ToListAsync(cancellationToken);

        return lowStockProducts.Select(p => _mapper.Map<ProductDto>(p)).ToList();
    }

    public async Task DeleteProductAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var product = await _unitOfWork.Products.GetByIdAsync(id, trackChanges: true, cancellationToken: cancellationToken);
        if (product == null)
            throw new ProductNotFoundException(id);

        await _unitOfWork.Products.DeleteAsync(product, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
