using ECommerce.Ordering.Application.Mapping;

namespace ECommerce.Ordering.Application.Queries.GetOrderById;

public class GetOrderByIdQueryHandler(IOrderRepository orders) : IRequestHandler<GetOrderByIdQuery, Result<OrderDto>>
{
    public async Task<Result<OrderDto>> Handle(GetOrderByIdQuery request, CancellationToken ct)
    {
        var order = await orders.GetByIdWithItemsAsync(request.OrderId, ct);
        if (order is null)
            return Result<OrderDto>.Fail(OrderingApplicationErrors.OrderNotFound);

        return Result<OrderDto>.Ok(order.ToDto());
    }
}
