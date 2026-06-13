using ECommerce.Catalog.Application.DTOs.Products;
using ECommerce.SharedKernel.Results;
using MediatR;

namespace ECommerce.Catalog.Application.Queries;

public record GetProductStatsQuery : IRequest<Result<ProductStatsDto>>;
