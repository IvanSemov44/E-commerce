using System.Threading;
using System.Threading.Tasks;
using MediatR;
using ECommerce.SharedKernel.Results;
using ECommerce.Catalog.Application.DTOs.Categories;
using ECommerce.Catalog.Application.Errors;
using ECommerce.Catalog.Application.Extensions;
using ECommerce.Catalog.Domain.Interfaces;
using ECommerce.Catalog.Domain.Aggregates.Category;
using ECommerce.Catalog.Domain.ValueObjects;

namespace ECommerce.Catalog.Application.Commands.CreateCategory;

public class CreateCategoryCommandHandler(
    ICategoryRepository _categories
) : IRequestHandler<CreateCategoryCommand, Result<CategoryDto>>
{
    public async Task<Result<CategoryDto>> Handle(CreateCategoryCommand request, CancellationToken cancellationToken)
    {
        // Validate name first so domain errors surface before slug processing
        var nameValidation = CategoryName.Create(request.Name);
        if (!nameValidation.IsSuccess)
            return Result<CategoryDto>.Fail(nameValidation.GetErrorOrThrow());

        // Determine the slug that will be used (explicit or auto-generated from name)
        string slugRaw = string.IsNullOrWhiteSpace(request.Slug)
            ? request.Name
            : request.Slug;

        // Validate slug format before checking uniqueness
        var slugValidation = Slug.Create(slugRaw);
        if (!slugValidation.IsSuccess)
            return Result<CategoryDto>.Fail(slugValidation.GetErrorOrThrow());

        string slug = slugValidation.GetDataOrThrow().Value;
        if (await _categories.SlugExistsAsync(slug, cancellationToken))
            return Result<CategoryDto>.Fail(CatalogApplicationErrors.DuplicateCategorySlug);

        var categoryResult = Category.Create(request.Name, request.ParentId, slugRaw);
        if (!categoryResult.IsSuccess)
            return Result<CategoryDto>.Fail(categoryResult.GetErrorOrThrow());

        var category = categoryResult.GetDataOrThrow();

        await _categories.AddAsync(category, cancellationToken);

        return Result<CategoryDto>.Ok(category.ToDto());
    }
}
