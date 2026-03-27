using System.Collections.Generic;
using MediatR;
using ECommerce.SharedKernel.Results;
using ECommerce.Catalog.Application.DTOs.Categories;
using ECommerce.Catalog.Application.DTOs.Common;

namespace ECommerce.Catalog.Application.Queries.GetCategories;

public record GetCategoriesQuery(int Page = 1, int PageSize = 20) : IRequest<Result<PaginatedResult<CategoryDto>>>;
