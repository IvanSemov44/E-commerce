using ECommerce.Ordering.Application.DTOs.Dashboard;
using ECommerce.Ordering.Domain.Interfaces;
using ECommerce.SharedKernel.Results;
using MediatR;

namespace ECommerce.Ordering.Application.Queries.GetOrderStats;

public class GetOrderStatsQueryHandler(IOrderRepository orders)
    : IRequestHandler<GetOrderStatsQuery, Result<OrderStatsDto>>
{
    public async Task<Result<OrderStatsDto>> Handle(GetOrderStatsQuery request, CancellationToken ct)
    {
        var days = request.Days <= 0 ? 30 : request.Days;

        var totalOrders = await orders.GetTotalOrdersCountAsync(ct);
        var totalRevenue = await orders.GetTotalRevenueAsync(ct);
        var ordersTrend = await orders.GetOrdersTrendAsync(days, ct);
        var revenueTrend = await orders.GetRevenueTrendAsync(days, ct);

        var dto = new OrderStatsDto
        {
            TotalOrders = totalOrders,
            TotalRevenue = totalRevenue,
            OrdersTrend = ordersTrend
                .OrderByDescending(x => x.Key)
                .Take(days)
                .Select(x => OrderTrendPointDto.From(x.Key, x.Value))
                .ToList(),
            RevenueTrend = revenueTrend
                .OrderByDescending(x => x.Key)
                .Take(days)
                .Select(x => RevenueTrendPointDto.From(x.Key, x.Value))
                .ToList()
        };

        return Result<OrderStatsDto>.Ok(dto);
    }
}
