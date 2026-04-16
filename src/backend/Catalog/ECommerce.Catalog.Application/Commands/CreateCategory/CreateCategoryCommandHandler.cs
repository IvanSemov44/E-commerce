using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using ECommerce.SharedKernel.Results;
using ECommerce.Catalog.Application.Errors;
using ECommerce.Catalog.Domain.Interfaces;
using ECommerce.Catalog.Domain.Aggregates.Category;

namespace ECommerce.Catalog.Application.Commands.CreateCategory;

public class CreateCategoryCommandHandler(
    ICategoryRepository _categories
) : IRequestHandler<CreateCategoryCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateCategoryCommand request, CancellationToken cancellationToken)
    {
        string slugRaw = string.IsNullOrWhiteSpace(request.Slug)
            ? request.Name
            : request.Slug;

        var categoryResult = Category.Create(request.Name, request.ParentId, slugRaw);
        if (!categoryResult.IsSuccess)
            return Result<Guid>.Fail(categoryResult.GetErrorOrThrow());

        var category = categoryResult.GetDataOrThrow();

        if (await _categories.SlugExistsAsync(category.Slug.Value, cancellationToken))
            return Result<Guid>.Fail(CatalogApplicationErrors.DuplicateCategorySlug);

        await _categories.AddAsync(category, cancellationToken);

        return Result<Guid>.Ok(category.Id);
    }
}
