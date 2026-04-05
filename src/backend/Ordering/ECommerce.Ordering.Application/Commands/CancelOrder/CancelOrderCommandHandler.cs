using MediatR;
using ECommerce.SharedKernel.Results;
using ECommerce.SharedKernel.Domain;
using ECommerce.Ordering.Application.DTOs;
using ECommerce.Ordering.Application.Mapping;
using ECommerce.Ordering.Domain.Interfaces;

namespace ECommerce.Ordering.Application.Commands.CancelOrder;

public class CancelOrderCommandHandler(IOrderRepository orders) : IRequestHandler<CancelOrderCommand, Result<OrderDto>>
{
    public async Task<Result<OrderDto>> Handle(CancelOrderCommand command, CancellationToken ct)
    {
        var order = await orders.GetByIdAsync(command.OrderId, ct);
        if (order is null)
            return Result<OrderDto>.Fail(new DomainError("ORDER_NOT_FOUND", "Order not found."));

        var result = order.Cancel(command.Reason);
        if (!result.IsSuccess)
            return Result<OrderDto>.Fail(result.GetErrorOrThrow());

        await orders.UpdateAsync(order, ct);
        return Result<OrderDto>.Ok(order.ToDto());
    }
}
