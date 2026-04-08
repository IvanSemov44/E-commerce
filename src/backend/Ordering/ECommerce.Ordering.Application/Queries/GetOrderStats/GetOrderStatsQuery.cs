using ECommerce.Ordering.Application.DTOs.Dashboard;
using ECommerce.SharedKernel.Results;
using MediatR;

namespace ECommerce.Ordering.Application.Queries.GetOrderStats;

public record GetOrderStatsQuery(int Days = 30) : IRequest<Result<OrderStatsDto>>;
