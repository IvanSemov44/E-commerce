using MediatR;
using ECommerce.SharedKernel.Results;
using ECommerce.Catalog.Application.DTOs.Products;
using ECommerce.SharedKernel.Pagination;

namespace ECommerce.Catalog.Application.Queries;

public record SearchProductsQuery(string Query = "", int Page = 1, int PageSize = 20) : IRequest<Result<PaginatedResult<ProductDto>>>;
