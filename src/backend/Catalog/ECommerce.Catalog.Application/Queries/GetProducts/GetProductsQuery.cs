using System;
using MediatR;
using ECommerce.SharedKernel.Constants;
using ECommerce.SharedKernel.Results;
using ECommerce.Catalog.Application.DTOs.Products;
using ECommerce.SharedKernel.Pagination;

namespace ECommerce.Catalog.Application.Queries;

public record GetProductsQuery(
    int      Page       = PaginationConstants.MinPageNumber,
    int      PageSize   = PaginationConstants.DefaultPageSize,
    Guid?    CategoryId = null,
    string?  Search     = null,
    decimal? MinPrice   = null,
    decimal? MaxPrice   = null,
    decimal? MinRating  = null,
    bool?    IsFeatured = null,
    string?  SortBy     = null
) : IRequest<Result<PaginatedResult<ProductDto>>>;
