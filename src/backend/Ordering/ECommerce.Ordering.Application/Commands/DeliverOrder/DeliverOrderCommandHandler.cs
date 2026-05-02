namespace ECommerce.Ordering.Application.Commands.DeliverOrder;

public class DeliverOrderCommandHandler(IOrderRepository orders) : IRequestHandler<DeliverOrderCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(DeliverOrderCommand command, CancellationToken ct)
    {
        var order = await orders.GetByIdAsync(command.OrderId, ct);
        if (order is null)
            return Result<Guid>.Fail(OrderingApplicationErrors.OrderNotFound);

        var result = order.Deliver();
        if (!result.IsSuccess)
            return Result<Guid>.Fail(result.GetErrorOrThrow());

        return Result<Guid>.Ok(order.Id);
    }
}
