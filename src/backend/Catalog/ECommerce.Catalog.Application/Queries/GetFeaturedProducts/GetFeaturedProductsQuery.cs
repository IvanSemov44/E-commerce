using MediatR;
using ECommerce.SharedKernel.Constants;
using ECommerce.SharedKernel.Results;
using ECommerce.Catalog.Application.DTOs.Products;
using ECommerce.SharedKernel.Pagination;

namespace ECommerce.Catalog.Application.Queries;

public record GetFeaturedProductsQuery(
    int Page     = PaginationConstants.MinPageNumber,
    int PageSize = PaginationConstants.DefaultPageSize
) : IRequest<Result<PaginatedResult<ProductDto>>>;
