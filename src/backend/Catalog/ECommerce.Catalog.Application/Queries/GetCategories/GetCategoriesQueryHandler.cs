using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using ECommerce.SharedKernel.Results;
using ECommerce.Catalog.Application.DTOs.Categories;
using ECommerce.Catalog.Application.Extensions;
using ECommerce.Catalog.Domain.Interfaces;

namespace ECommerce.Catalog.Application.Queries.GetCategories;

public class GetCategoriesQueryHandler(
    ICategoryRepository _categories
) : IRequestHandler<GetCategoriesQuery, Result<IEnumerable<CategoryDto>>>
{
    public async Task<Result<IEnumerable<CategoryDto>>> Handle(GetCategoriesQuery request, CancellationToken cancellationToken)
    {
        var categories = await _categories.GetAllAsync(cancellationToken);
        var dtos = categories
            .Where(c => c.ParentId == null)
            .Select(c => c.ToDto())
            .ToList();
        return Result<IEnumerable<CategoryDto>>.Ok(dtos);
    }
}
