using System;
using MediatR;
using ECommerce.SharedKernel.Results;
using ECommerce.Catalog.Application.DTOs.Products;
using ECommerce.SharedKernel.Pagination;

namespace ECommerce.Catalog.Application.Queries;

public record GetFeaturedProductsQuery(int Page = 1, int PageSize = 10) : IRequest<Result<PaginatedResult<ProductDto>>>;
