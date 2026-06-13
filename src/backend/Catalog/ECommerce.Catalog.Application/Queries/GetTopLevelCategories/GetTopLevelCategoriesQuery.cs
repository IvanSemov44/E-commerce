using System.Collections.Generic;
using MediatR;
using ECommerce.SharedKernel.Results;
using ECommerce.Catalog.Application.DTOs.Categories;
using ECommerce.SharedKernel.Pagination;

namespace ECommerce.Catalog.Application.Queries;

public record GetTopLevelCategoriesQuery(int Page = 1, int PageSize = 20) : IRequest<Result<PaginatedResult<CategoryDto>>>;
