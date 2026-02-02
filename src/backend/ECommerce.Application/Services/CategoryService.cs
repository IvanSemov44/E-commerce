using ECommerce.Application.Interfaces;
using AutoMapper;
using ECommerce.Application.DTOs.Common;
using ECommerce.Core.Entities;
using ECommerce.Core.Exceptions;
using ECommerce.Core.Interfaces.Repositories;
using System.Threading;

namespace ECommerce.Application.Services;

public class CategoryService : ICategoryService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public CategoryService(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<IEnumerable<CategoryDto>> GetAllCategoriesAsync(CancellationToken cancellationToken = default)
    {
        var categories = await _unitOfWork.Categories.GetAllAsync(trackChanges: false, cancellationToken: cancellationToken);
        return _mapper.Map<IEnumerable<CategoryDto>>(categories);
    }

    public async Task<IEnumerable<CategoryDto>> GetTopLevelCategoriesAsync(CancellationToken cancellationToken = default)
    {
        var categories = await _unitOfWork.Categories.GetTopLevelCategoriesAsync(trackChanges: false, cancellationToken: cancellationToken);
        var dtos = _mapper.Map<IEnumerable<CategoryDto>>(categories);

        // Populate product counts
        foreach (var dto in dtos)
        {
            dto.ProductCount = await _unitOfWork.Categories.GetProductCountAsync(dto.Id, cancellationToken: cancellationToken);
        }

        return dtos;
    }

    public async Task<CategoryDetailDto> GetCategoryByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var category = await _unitOfWork.Categories.GetByIdAsync(id, trackChanges: false, cancellationToken: cancellationToken);
        if (category == null)
            throw new CategoryNotFoundException(id);

        var dto = _mapper.Map<CategoryDetailDto>(category);
        dto.ProductCount = await _unitOfWork.Categories.GetProductCountAsync(id, cancellationToken: cancellationToken);
        return dto;
    }

    public async Task<CategoryDetailDto> GetCategoryBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        var category = await _unitOfWork.Categories.GetBySlugAsync(slug, trackChanges: false, cancellationToken: cancellationToken);
        if (category == null)
            throw new CategoryNotFoundException($"Category with slug '{slug}' not found");

        var dto = _mapper.Map<CategoryDetailDto>(category);
        dto.ProductCount = await _unitOfWork.Categories.GetProductCountAsync(category.Id, cancellationToken: cancellationToken);
        return dto;
    }

    public async Task<CategoryDetailDto> CreateCategoryAsync(CreateCategoryDto dto, CancellationToken cancellationToken = default)
    {
        // Validate slug uniqueness
        if (!await _unitOfWork.Categories.IsSlugUniqueAsync(dto.Slug, cancellationToken: cancellationToken))
        {
            throw new DuplicateCategorySlugException(dto.Slug);
        }

        var category = _mapper.Map<Category>(dto);
        category.IsActive = true;

        await _unitOfWork.Categories.AddAsync(category, cancellationToken: cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken: cancellationToken);

        return _mapper.Map<CategoryDetailDto>(category);
    }

    public async Task<CategoryDetailDto> UpdateCategoryAsync(Guid id, UpdateCategoryDto dto, CancellationToken cancellationToken = default)
    {
        var category = await _unitOfWork.Categories.GetByIdAsync(id, trackChanges: true, cancellationToken: cancellationToken);
        if (category == null)
        {
            throw new CategoryNotFoundException(id);
        }

        // Validate slug uniqueness if changed
        if (!string.IsNullOrEmpty(dto.Slug) && dto.Slug != category.Slug)
        {
            if (!await _unitOfWork.Categories.IsSlugUniqueAsync(dto.Slug, id, cancellationToken: cancellationToken))
            {
                throw new DuplicateCategorySlugException(dto.Slug);
            }
        }

        _mapper.Map(dto, category);
        category.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.Categories.Update(category);
        await _unitOfWork.SaveChangesAsync(cancellationToken: cancellationToken);

        return _mapper.Map<CategoryDetailDto>(category);
    }

    public async Task DeleteCategoryAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var category = await _unitOfWork.Categories.GetByIdAsync(id, trackChanges: true, cancellationToken: cancellationToken);
        if (category == null)
            throw new CategoryNotFoundException(id);

        // Check if category has products
        var productCount = await _unitOfWork.Categories.GetProductCountAsync(id, cancellationToken: cancellationToken);
        if (productCount > 0)
        {
            throw new CategoryHasProductsException(id);
        }

        _unitOfWork.Categories.Delete(category);
        await _unitOfWork.SaveChangesAsync(cancellationToken: cancellationToken);
    }
}
