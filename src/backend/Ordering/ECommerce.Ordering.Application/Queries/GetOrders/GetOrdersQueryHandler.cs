using MediatR;
using ECommerce.SharedKernel.Results;
using ECommerce.Ordering.Application.DTOs;
using ECommerce.Ordering.Application.Mapping;
using ECommerce.Ordering.Domain.Interfaces;

namespace ECommerce.Ordering.Application.Queries.GetOrders;

public class GetOrdersQueryHandler(IOrderRepository orders) : IRequestHandler<GetOrdersQuery, Result<List<OrderDto>>>
{
    public async Task<Result<List<OrderDto>>> Handle(GetOrdersQuery request, CancellationToken ct)
    {
        var ordersList = await orders.GetAllAsync(ct);
        var dtos = ordersList.Select(o => o.ToDto()).ToList();
        return Result<List<OrderDto>>.Ok(dtos);
    }
}
