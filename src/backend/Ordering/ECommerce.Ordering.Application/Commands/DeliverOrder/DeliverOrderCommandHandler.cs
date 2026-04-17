using MediatR;
using ECommerce.SharedKernel.Results;
using ECommerce.SharedKernel.Domain;
using ECommerce.Ordering.Application.DTOs;
using ECommerce.Ordering.Application.Interfaces;
using ECommerce.Ordering.Application.Mapping;
using ECommerce.Ordering.Domain.Interfaces;

namespace ECommerce.Ordering.Application.Commands.DeliverOrder;

public class DeliverOrderCommandHandler(
    IOrderRepository orders,
    IOrderIntegrationEventPublisher orderIntegrationEventPublisher) : IRequestHandler<DeliverOrderCommand, Result<OrderDto>>
{
    public async Task<Result<OrderDto>> Handle(DeliverOrderCommand command, CancellationToken ct)
    {
        var order = await orders.GetByIdAsync(command.OrderId, ct);
        if (order is null)
            return Result<OrderDto>.Fail(new DomainError("ORDER_NOT_FOUND", "Order not found."));

        var result = order.Deliver();
        if (!result.IsSuccess)
            return Result<OrderDto>.Fail(result.GetErrorOrThrow());

        await orders.UpdateAsync(order, ct);

        await orderIntegrationEventPublisher.PublishOrderDeliveredAsync(
            order.Id,
            order.UserId,
            order.Items.Select(i => i.ProductId).ToArray(),
            ct);

        return Result<OrderDto>.Ok(order.ToDto());
    }
}
