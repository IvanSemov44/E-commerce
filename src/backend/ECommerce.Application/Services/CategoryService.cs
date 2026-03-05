using ECommerce.Application.Interfaces;
using AutoMapper;
using ECommerce.Application.DTOs.Common;
using ECommerce.Core.Entities;
using ECommerce.Core.Exceptions;
using ECommerce.Core.Interfaces.Repositories;
using ECommerce.Core.Constants;
using ECommerce.Core.Results;
using Microsoft.Extensions.Logging;
using System.Threading;

namespace ECommerce.Application.Services;

public class CategoryService : ICategoryService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<CategoryService> _logger;

    public CategoryService(IUnitOfWork unitOfWork, IMapper mapper, ILogger<CategoryService> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<PaginatedResult<CategoryDto>> GetAllCategoriesAsync(
        int pageNumber = 1,
        int pageSize = 100,
        CancellationToken cancellationToken = default)
    {
        if (pageNumber <= 0)
            throw new InvalidPaginationException(pageNumber);
        
        // Enforce max page size to prevent DoS attacks
        pageSize = pageSize < 1 ? PaginationConstants.DefaultPageSize : Math.Min(pageSize, PaginationConstants.MaxPageSize);

        var categories = await _unitOfWork.Categories.GetAllAsync(trackChanges: false, cancellationToken: cancellationToken);
        var totalCount = categories.Count();
        
        var paginatedCategories = categories
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        var dtos = _mapper.Map<List<CategoryDto>>(paginatedCategories);
        
        return new PaginatedResult<CategoryDto>
        {
            Items = dtos,
            TotalCount = totalCount,
            Page = pageNumber,
            PageSize = pageSize
        };
    }

    public async Task<PaginatedResult<CategoryDto>> GetTopLevelCategoriesAsync(
        int pageNumber = 1,
        int pageSize = 100,
        CancellationToken cancellationToken = default)
    {
        if (pageNumber <= 0)
            throw new InvalidPaginationException(pageNumber);
        
        // Enforce max page size to prevent DoS attacks
        pageSize = pageSize < 1 ? PaginationConstants.DefaultPageSize : Math.Min(pageSize, PaginationConstants.MaxPageSize);

        var categories = await _unitOfWork.Categories.GetTopLevelCategoriesAsync(trackChanges: false, cancellationToken: cancellationToken);
        var totalCount = categories.Count();

        var paginatedCategories = categories
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        var dtos = _mapper.Map<List<CategoryDto>>(paginatedCategories).ToList();

        // FIX: Use batch query instead of N+1 loop
        var categoryIds = dtos.Select(d => d.Id).ToList();
        var productCounts = await _unitOfWork.Categories.GetProductCountsAsync(categoryIds, cancellationToken);

        dtos = dtos
            .Select(dto => dto with { ProductCount = productCounts.GetValueOrDefault(dto.Id, 0) })
            .ToList();

        return new PaginatedResult<CategoryDto>
        {
            Items = dtos,
            TotalCount = totalCount,
            Page = pageNumber,
            PageSize = pageSize
        };
    }

    public async Task<Result<CategoryDetailDto>> GetCategoryByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var category = await _unitOfWork.Categories.GetByIdAsync(id, trackChanges: false, cancellationToken: cancellationToken);
        if (category == null)
            return Result<CategoryDetailDto>.Fail(ErrorCodes.CategoryNotFound, $"Category with id '{id}' not found");

        var dto = _mapper.Map<CategoryDetailDto>(category);
        var productCount = await _unitOfWork.Categories.GetProductCountAsync(id, cancellationToken: cancellationToken);
        return Result<CategoryDetailDto>.Ok(dto with { ProductCount = productCount });
    }

    public async Task<Result<CategoryDetailDto>> GetCategoryBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        var category = await _unitOfWork.Categories.GetBySlugAsync(slug, trackChanges: false, cancellationToken: cancellationToken);
        if (category == null)
            return Result<CategoryDetailDto>.Fail(ErrorCodes.CategoryNotFound, $"Category with slug '{slug}' not found");

        var dto = _mapper.Map<CategoryDetailDto>(category);
        var productCount = await _unitOfWork.Categories.GetProductCountAsync(category.Id, cancellationToken: cancellationToken);
        return Result<CategoryDetailDto>.Ok(dto with { ProductCount = productCount });
    }

    public async Task<Result<CategoryDetailDto>> CreateCategoryAsync(CreateCategoryDto dto, CancellationToken cancellationToken = default)
    {
        // Validate slug uniqueness
        if (!await _unitOfWork.Categories.IsSlugUniqueAsync(dto.Slug, cancellationToken: cancellationToken))
        {
            return Result<CategoryDetailDto>.Fail(ErrorCodes.DuplicateCategorySlug, $"Slug '{dto.Slug}' already exists");
        }

        var category = _mapper.Map<Category>(dto);
        category.IsActive = true;

        await _unitOfWork.Categories.AddAsync(category, cancellationToken: cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken: cancellationToken);

        return Result<CategoryDetailDto>.Ok(_mapper.Map<CategoryDetailDto>(category));
    }

    public async Task<Result<CategoryDetailDto>> UpdateCategoryAsync(Guid id, UpdateCategoryDto dto, CancellationToken cancellationToken = default)
    {
        var category = await _unitOfWork.Categories.GetByIdAsync(id, trackChanges: true, cancellationToken: cancellationToken);
        if (category == null)
        {
            return Result<CategoryDetailDto>.Fail(ErrorCodes.CategoryNotFound, $"Category with id '{id}' not found");
        }

        // Validate slug uniqueness if changed
        if (!string.IsNullOrEmpty(dto.Slug) && dto.Slug != category.Slug)
        {
            if (!await _unitOfWork.Categories.IsSlugUniqueAsync(dto.Slug, id, cancellationToken: cancellationToken))
            {
                return Result<CategoryDetailDto>.Fail(ErrorCodes.DuplicateCategorySlug, $"Slug '{dto.Slug}' already exists");
            }
        }

        _mapper.Map(dto, category);
        category.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.Categories.Update(category);
        await _unitOfWork.SaveChangesAsync(cancellationToken: cancellationToken);

        return Result<CategoryDetailDto>.Ok(_mapper.Map<CategoryDetailDto>(category));
    }

    public async Task<Result<Unit>> DeleteCategoryAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var category = await _unitOfWork.Categories.GetByIdAsync(id, trackChanges: true, cancellationToken: cancellationToken);
        if (category == null)
            return Result<Unit>.Fail(ErrorCodes.CategoryNotFound, $"Category with id '{id}' not found");

        // Check if category has products
        var productCount = await _unitOfWork.Categories.GetProductCountAsync(id, cancellationToken: cancellationToken);
        if (productCount > 0)
        {
            return Result<Unit>.Fail(ErrorCodes.CategoryHasProducts, $"Category has {productCount} product(s) and cannot be deleted");
        }

        _unitOfWork.Categories.Delete(category);
        await _unitOfWork.SaveChangesAsync(cancellationToken: cancellationToken);
        return Result<Unit>.Ok(new Unit());
    }
}
