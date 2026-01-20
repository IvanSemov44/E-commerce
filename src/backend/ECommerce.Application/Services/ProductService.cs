using AutoMapper;
using ECommerce.Application.DTOs.Common;
using ECommerce.Application.DTOs.Products;
using ECommerce.Core.Entities;
using ECommerce.Core.Interfaces.Repositories;

namespace ECommerce.Application.Services;

public interface IProductService
{
    Task<PaginatedResult<ProductDto>> GetProductsAsync(int page = 1, int pageSize = 20);
    Task<ProductDetailDto?> GetProductBySlugAsync(string slug);
    Task<ProductDetailDto?> GetProductByIdAsync(Guid id);
    Task<List<ProductDto>> GetFeaturedProductsAsync(int count = 10);
    Task<ProductDetailDto> CreateProductAsync(CreateProductDto dto);
    Task<ProductDetailDto> UpdateProductAsync(Guid id, UpdateProductDto dto);
    Task<bool> DeleteProductAsync(Guid id);
}

public class ProductService : IProductService
{
    private readonly IProductRepository _productRepository;
    private readonly IMapper _mapper;

    public ProductService(IProductRepository productRepository, IMapper mapper)
    {
        _productRepository = productRepository;
        _mapper = mapper;
    }

    public async Task<PaginatedResult<ProductDto>> GetProductsAsync(int page = 1, int pageSize = 20)
    {
        var totalCount = await _productRepository.GetActiveProductsCountAsync();
        var skip = (page - 1) * pageSize;
        var products = await _productRepository.GetActiveProductsAsync(skip, pageSize);

        return new PaginatedResult<ProductDto>
        {
            Items = products.Select(p => _mapper.Map<ProductDto>(p)).ToList(),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<ProductDetailDto?> GetProductBySlugAsync(string slug)
    {
        var product = await _productRepository.GetBySlugAsync(slug);
        return product == null ? null : _mapper.Map<ProductDetailDto>(product);
    }

    public async Task<ProductDetailDto?> GetProductByIdAsync(Guid id)
    {
        var product = await _productRepository.GetByIdAsync(id);
        return product == null ? null : _mapper.Map<ProductDetailDto>(product);
    }

    public async Task<List<ProductDto>> GetFeaturedProductsAsync(int count = 10)
    {
        var products = await _productRepository.GetFeaturedAsync(count);
        return products.Select(p => _mapper.Map<ProductDto>(p)).ToList();
    }

    public async Task<ProductDetailDto> CreateProductAsync(CreateProductDto dto)
    {
        // Validate slug uniqueness
        if (!await _productRepository.IsSlugUniqueAsync(dto.Slug))
        {
            throw new ArgumentException($"Product with slug '{dto.Slug}' already exists");
        }

        var product = _mapper.Map<Product>(dto);
        product.IsActive = true;

        await _productRepository.AddAsync(product);
        await _productRepository.SaveChangesAsync();
        return _mapper.Map<ProductDetailDto>(product);
    }

    public async Task<ProductDetailDto> UpdateProductAsync(Guid id, UpdateProductDto dto)
    {
        var product = await _productRepository.GetByIdAsync(id);
        if (product == null)
            throw new ArgumentException($"Product with ID {id} not found");

        // Validate slug uniqueness if changed
        if (!string.IsNullOrEmpty(dto.Slug) && dto.Slug != product.Slug)
        {
            if (!await _productRepository.IsSlugUniqueAsync(dto.Slug, id))
            {
                throw new ArgumentException($"Product with slug '{dto.Slug}' already exists");
            }
        }

        _mapper.Map(dto, product);
        product.UpdatedAt = DateTime.UtcNow;

        await _productRepository.UpdateAsync(product);
        await _productRepository.SaveChangesAsync();
        return _mapper.Map<ProductDetailDto>(product);
    }

    public async Task<PaginatedResult<ProductDto>> GetAllProductsAsync(int page = 1, int pageSize = 20)
    {
        return await GetProductsAsync(page, pageSize);
    }

    public async Task<PaginatedResult<ProductDto>> GetFeaturedProductsAsync(int page = 1, int pageSize = 20)
    {
        var totalCount = await _productRepository.GetActiveProductsCountAsync();
        var skip = (page - 1) * pageSize;
        var products = await _productRepository.GetFeaturedAsync(pageSize);

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
        var allProducts = await _productRepository.GetAllAsync();
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
        var products = await _productRepository.GetByCategoryAsync(categoryId);
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
        var allProducts = await _productRepository.GetAllAsync();
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
        var allProducts = await _productRepository.GetAllAsync();
        var lowStockProducts = allProducts
            .Where(p => p.StockQuantity <= p.LowStockThreshold && p.IsActive)
            .ToList();

        return lowStockProducts.Select(p => _mapper.Map<ProductDto>(p)).ToList();
    }

    public async Task<bool> DeleteProductAsync(Guid id)
    {
        var product = await _productRepository.GetByIdAsync(id);
        if (product == null)
            return false;

        await _productRepository.DeleteAsync(product);
        await _productRepository.SaveChangesAsync();
        return true;
    }
}
