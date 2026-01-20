using AutoMapper;
using ECommerce.Application.DTOs;
using ECommerce.Core.Entities;
using ECommerce.Core.Interfaces.Repositories;

namespace ECommerce.Application.Services;

public class CategoryService : ICategoryService
{
    private readonly ICategoryRepository _categoryRepository;
    private readonly IMapper _mapper;

    public CategoryService(ICategoryRepository categoryRepository, IMapper mapper)
    {
        _categoryRepository = categoryRepository;
        _mapper = mapper;
    }

    public async Task<IEnumerable<CategoryDto>> GetAllCategoriesAsync()
    {
        var categories = await _categoryRepository.GetAllAsync();
        return _mapper.Map<IEnumerable<CategoryDto>>(categories);
    }

    public async Task<IEnumerable<CategoryDto>> GetTopLevelCategoriesAsync()
    {
        var categories = await _categoryRepository.GetTopLevelCategoriesAsync();
        var dtos = _mapper.Map<IEnumerable<CategoryDto>>(categories);

        // Populate product counts
        foreach (var dto in dtos)
        {
            dto.ProductCount = await _categoryRepository.GetProductCountAsync(dto.Id);
        }

        return dtos;
    }

    public async Task<CategoryDetailDto?> GetCategoryByIdAsync(Guid id)
    {
        var category = await _categoryRepository.GetByIdAsync(id);
        if (category == null) return null;

        var dto = _mapper.Map<CategoryDetailDto>(category);
        dto.ProductCount = await _categoryRepository.GetProductCountAsync(id);
        return dto;
    }

    public async Task<CategoryDetailDto?> GetCategoryBySlugAsync(string slug)
    {
        var category = await _categoryRepository.GetBySlugAsync(slug);
        if (category == null) return null;

        var dto = _mapper.Map<CategoryDetailDto>(category);
        dto.ProductCount = await _categoryRepository.GetProductCountAsync(category.Id);
        return dto;
    }

    public async Task<CategoryDetailDto> CreateCategoryAsync(CreateCategoryDto dto)
    {
        // Validate slug uniqueness
        if (!await _categoryRepository.IsSlugUniqueAsync(dto.Slug))
        {
            throw new ArgumentException($"Category with slug '{dto.Slug}' already exists");
        }

        var category = _mapper.Map<Category>(dto);
        category.IsActive = true;

        await _categoryRepository.AddAsync(category);
        await _categoryRepository.SaveChangesAsync();

        return _mapper.Map<CategoryDetailDto>(category);
    }

    public async Task<CategoryDetailDto> UpdateCategoryAsync(Guid id, UpdateCategoryDto dto)
    {
        var category = await _categoryRepository.GetByIdAsync(id);
        if (category == null)
        {
            throw new ArgumentException($"Category with ID {id} not found");
        }

        // Validate slug uniqueness if changed
        if (!string.IsNullOrEmpty(dto.Slug) && dto.Slug != category.Slug)
        {
            if (!await _categoryRepository.IsSlugUniqueAsync(dto.Slug, id))
            {
                throw new ArgumentException($"Category with slug '{dto.Slug}' already exists");
            }
        }

        _mapper.Map(dto, category);
        category.UpdatedAt = DateTime.UtcNow;

        await _categoryRepository.UpdateAsync(category);
        await _categoryRepository.SaveChangesAsync();

        return _mapper.Map<CategoryDetailDto>(category);
    }

    public async Task<bool> DeleteCategoryAsync(Guid id)
    {
        var category = await _categoryRepository.GetByIdAsync(id);
        if (category == null) return false;

        // Check if category has products
        var productCount = await _categoryRepository.GetProductCountAsync(id);
        if (productCount > 0)
        {
            throw new InvalidOperationException("Cannot delete category with existing products");
        }

        await _categoryRepository.DeleteAsync(category);
        await _categoryRepository.SaveChangesAsync();

        return true;
    }
}
