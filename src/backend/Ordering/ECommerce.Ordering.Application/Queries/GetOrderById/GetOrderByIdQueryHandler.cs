using MediatR;
using ECommerce.SharedKernel.Results;
using ECommerce.SharedKernel.Domain;
using ECommerce.Ordering.Application.DTOs;
using ECommerce.Ordering.Application.Mapping;
using ECommerce.Ordering.Domain.Interfaces;

namespace ECommerce.Ordering.Application.Queries.GetOrderById;

public class GetOrderByIdQueryHandler(IOrderRepository orders) : IRequestHandler<GetOrderByIdQuery, Result<OrderDto>>
{
    public async Task<Result<OrderDto>> Handle(GetOrderByIdQuery request, CancellationToken ct)
    {
        var order = await orders.GetByIdWithItemsAsync(request.OrderId, ct);
        if (order is null)
            return Result<OrderDto>.Fail(new DomainError("ORDER_NOT_FOUND", "Order not found."));

        return Result<OrderDto>.Ok(order.ToDto());
    }
}
