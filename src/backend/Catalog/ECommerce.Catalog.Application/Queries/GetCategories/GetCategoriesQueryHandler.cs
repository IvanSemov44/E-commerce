using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using ECommerce.SharedKernel.Results;
using ECommerce.Catalog.Application.DTOs.Categories;
using ECommerce.SharedKernel.Pagination;
using ECommerce.Catalog.Application.Extensions;
using ECommerce.Catalog.Domain.Interfaces;

namespace ECommerce.Catalog.Application.Queries;

public class GetCategoriesQueryHandler(
    ICategoryRepository _categories
) : IRequestHandler<GetCategoriesQuery, Result<PaginatedResult<CategoryDto>>>
{
    public async Task<Result<PaginatedResult<CategoryDto>>> Handle(GetCategoriesQuery request, CancellationToken cancellationToken)
    {
        var (items, total) = await _categories.GetPagedAsync(request.Page, request.PageSize, cancellationToken);
        var dtos = items.Select(c => c.ToDto()).ToList();

        var page = new PaginatedResult<CategoryDto>
        {
            Items = dtos,
            TotalCount = total,
            Page = request.Page,
            PageSize = request.PageSize
        };

        return Result<PaginatedResult<CategoryDto>>.Ok(page);
    }
}
