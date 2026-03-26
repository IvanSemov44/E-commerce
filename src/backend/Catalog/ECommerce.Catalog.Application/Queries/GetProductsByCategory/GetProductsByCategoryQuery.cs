using MediatR;
using ECommerce.SharedKernel.Results;
using ECommerce.Catalog.Application.DTOs.Products;
using ECommerce.Catalog.Application.DTOs.Common;
using System;

namespace ECommerce.Catalog.Application.Queries.GetProductsByCategory;

public record GetProductsByCategoryQuery(Guid CategoryId, int Page = 1, int PageSize = 20) : IRequest<Result<PaginatedResult<ProductDto>>>;
