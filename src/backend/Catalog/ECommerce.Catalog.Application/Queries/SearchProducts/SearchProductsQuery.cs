using MediatR;
using ECommerce.SharedKernel.Results;
using ECommerce.Catalog.Application.DTOs.Products;
using ECommerce.Catalog.Application.DTOs.Common;

namespace ECommerce.Catalog.Application.Queries.SearchProducts;

public record SearchProductsQuery(string Query, int Page = 1, int PageSize = 20) : IRequest<Result<PaginatedResult<ProductDto>>>;
