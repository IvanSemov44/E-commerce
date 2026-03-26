using System.Threading;
using System.Threading.Tasks;
using MediatR;
using ECommerce.SharedKernel.Results;
using ECommerce.Catalog.Application.Errors;
using ECommerce.Catalog.Application.DTOs.Categories;
using ECommerce.Catalog.Application.Extensions;
using ECommerce.Catalog.Domain.Interfaces;

namespace ECommerce.Catalog.Application.Queries.GetCategoryById;

public class GetCategoryByIdQueryHandler(
    ICategoryRepository _categories
) : IRequestHandler<GetCategoryByIdQuery, Result<CategoryDto>>
{
    public async Task<Result<CategoryDto>> Handle(GetCategoryByIdQuery request, CancellationToken cancellationToken)
    {
        var category = await _categories.GetByIdAsync(request.Id, cancellationToken);
        if (category is null)
            return Result<CategoryDto>.Fail(CatalogApplicationErrors.CategoryNotFound);

        var dto = category.ToDto();

        return Result<CategoryDto>.Ok(dto);
    }
}
