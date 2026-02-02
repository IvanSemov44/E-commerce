using AutoMapper;
using ECommerce.Application.DTOs.Common;
using ECommerce.Application.DTOs.Products;
using ECommerce.Application.Interfaces;
using ECommerce.Core.Entities;
using ECommerce.Core.Exceptions;
using ECommerce.Core.Interfaces.Repositories;

namespace ECommerce.Application.Services;

public class ProductService : IProductService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public ProductService(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<PaginatedResult<ProductDto>> GetProductsAsync(ProductQueryDto query)
    {
        var page = query?.Page ?? 1;
        var pageSize = query?.PageSize ?? 8;
        var skip = (page - 1) * pageSize;

        var (products, totalCount) = await _unitOfWork.Products.GetProductsWithFiltersAsync(
            skip,
            pageSize,
            query?.CategoryId,
            query?.Search,
            query?.MinPrice,
            query?.MaxPrice,
            query?.MinRating,
            query?.IsFeatured,
            query?.SortBy);

        return new PaginatedResult<ProductDto>
        {
            Items = products.Select(p => _mapper.Map<ProductDto>(p)).ToList(),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<ProductDetailDto> GetProductBySlugAsync(string slug)
    {
        var product = await _unitOfWork.Products.GetBySlugAsync(slug);
        if (product == null)
            throw new ProductNotFoundException(slug);

        return _mapper.Map<ProductDetailDto>(product);
    }

    public async Task<ProductDetailDto> GetProductByIdAsync(Guid id)
    {
        var product = await _unitOfWork.Products.GetByIdAsync(id);
        if (product == null)
            throw new ProductNotFoundException(id);

        return _mapper.Map<ProductDetailDto>(product);
    }

    public async Task<List<ProductDto>> GetFeaturedProductsAsync(int count = 10)
    {
        var products = await _unitOfWork.Products.GetFeaturedAsync(count);
        return products.Select(p => _mapper.Map<ProductDto>(p)).ToList();
    }

    public async Task<ProductDetailDto> CreateProductAsync(CreateProductDto dto)
    {
        if (!await _unitOfWork.Products.IsSlugUniqueAsync(dto.Slug))
            throw new DuplicateProductSlugException(dto.Slug);

        var product = _mapper.Map<Product>(dto);
        product.IsActive = true;

        await _unitOfWork.Products.AddAsync(product);
        await _unitOfWork.SaveChangesAsync();

        return _mapper.Map<ProductDetailDto>(product);
    }

    public async Task<ProductDetailDto> UpdateProductAsync(Guid id, UpdateProductDto dto)
    {
        var product = await _unitOfWork.Products.GetByIdAsync(id);
        if (product == null)
            throw new ProductNotFoundException(id);

        if (!string.IsNullOrEmpty(dto.Slug) && dto.Slug != product.Slug)
        {
            if (!await _unitOfWork.Products.IsSlugUniqueAsync(dto.Slug, id))
                throw new DuplicateProductSlugException(dto.Slug);
        }

        _mapper.Map(dto, product);
        product.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.Products.UpdateAsync(product);
        await _unitOfWork.SaveChangesAsync();

        return _mapper.Map<ProductDetailDto>(product);
    }

    public async Task<PaginatedResult<ProductDto>> GetAllProductsAsync(int page = 1, int pageSize = 20)
    {
        var query = new ProductQueryDto { Page = page, PageSize = pageSize };
        return await GetProductsAsync(query);
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

    public async Task<PaginatedResult<ProductDto>> SearchProductsAsync(string query, int page = 1, int pageSize = 20)
    {
        var skip = (page - 1) * pageSize;
        var allProducts = await _unitOfWork.Products.GetAllAsync();
        var searchResults = allProducts
            .Where(p => p.IsActive && (p.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                                        p.Description != null && p.Description.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                                        p.Sku != null && p.Sku.Contains(query, StringComparison.OrdinalIgnoreCase)))
            .ToList();

        var totalCount = searchResults.Count;
        var products = searchResults
            .Skip(skip)
            .Take(pageSize)
            .ToList();

        return new PaginatedResult<ProductDto>
        {
            Items = products.Select(p => _mapper.Map<ProductDto>(p)).ToList(),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<PaginatedResult<ProductDto>> GetProductsByCategoryAsync(Guid categoryId, int page = 1, int pageSize = 20)
    {
        var skip = (page - 1) * pageSize;
        var products = await _unitOfWork.Products.GetByCategoryAsync(categoryId);
        var totalCount = products.Count();

        var paginatedProducts = products
            .Skip(skip)
            .Take(pageSize)
            .ToList();

        return new PaginatedResult<ProductDto>
        {
            Items = paginatedProducts.Select(p => _mapper.Map<ProductDto>(p)).ToList(),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<PaginatedResult<ProductDto>> GetProductsByPriceRangeAsync(decimal minPrice, decimal maxPrice, int page = 1, int pageSize = 20)
    {
        var skip = (page - 1) * pageSize;
        var allProducts = await _unitOfWork.Products.GetAllAsync();
        var filteredProducts = allProducts
            .Where(p => p.IsActive && p.Price >= minPrice && p.Price <= maxPrice)
            .ToList();

        var totalCount = filteredProducts.Count;
        var products = filteredProducts
            .Skip(skip)
            .Take(pageSize)
            .ToList();

        return new PaginatedResult<ProductDto>
        {
            Items = products.Select(p => _mapper.Map<ProductDto>(p)).ToList(),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<List<ProductDto>> GetLowStockProductsAsync()
    {
        var allProducts = await _unitOfWork.Products.GetAllAsync();
        var lowStockProducts = allProducts
            .Where(p => p.StockQuantity <= p.LowStockThreshold && p.IsActive)
            .ToList();

        return lowStockProducts.Select(p => _mapper.Map<ProductDto>(p)).ToList();
    }

    public async Task DeleteProductAsync(Guid id)
    {
        var product = await _unitOfWork.Products.GetByIdAsync(id);
        if (product == null)
            throw new ProductNotFoundException(id);

        await _unitOfWork.Products.DeleteAsync(product);
        await _unitOfWork.SaveChangesAsync();
    }
}
