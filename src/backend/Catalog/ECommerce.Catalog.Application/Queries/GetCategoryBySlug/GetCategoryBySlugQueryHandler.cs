using System.Threading;
using System.Threading.Tasks;
using MediatR;
using ECommerce.SharedKernel.Results;
using ECommerce.Catalog.Application.Errors;
using ECommerce.Catalog.Application.DTOs.Categories;
using ECommerce.Catalog.Application.Extensions;
using ECommerce.Catalog.Domain.Interfaces;

namespace ECommerce.Catalog.Application.Queries.GetCategoryBySlug;

public class GetCategoryBySlugQueryHandler(
    ICategoryRepository _categories
) : IRequestHandler<GetCategoryBySlugQuery, Result<CategoryDto>>
{
    public async Task<Result<CategoryDto>> Handle(GetCategoryBySlugQuery request, CancellationToken cancellationToken)
    {
        var category = await _categories.GetBySlugAsync(request.Slug, cancellationToken);
        if (category is null)
            return Result<CategoryDto>.Fail(CatalogApplicationErrors.CategoryNotFound);

        return Result<CategoryDto>.Ok(category.ToDto());
    }
}
