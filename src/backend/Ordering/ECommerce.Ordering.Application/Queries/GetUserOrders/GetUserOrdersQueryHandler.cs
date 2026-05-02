using ECommerce.Ordering.Application.Mapping;

namespace ECommerce.Ordering.Application.Queries.GetUserOrders;

public class GetUserOrdersQueryHandler(IOrderRepository orders) : IRequestHandler<GetUserOrdersQuery, Result<List<OrderDto>>>
{
    public async Task<Result<List<OrderDto>>> Handle(GetUserOrdersQuery request, CancellationToken ct)
    {
        var ordersList = await orders.GetByUserIdAsync(request.UserId, ct);
        var dtos = ordersList.Select(o => o.ToDto()).ToList();
        return Result<List<OrderDto>>.Ok(dtos);
    }
}
