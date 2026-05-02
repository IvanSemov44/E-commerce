namespace ECommerce.Ordering.Application.Commands.CancelOrder;

public class CancelOrderCommandHandler(IOrderRepository orders) : IRequestHandler<CancelOrderCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CancelOrderCommand command, CancellationToken ct)
    {
        var order = await orders.GetByIdAsync(command.OrderId, ct);
        if (order is null)
            return Result<Guid>.Fail(OrderingApplicationErrors.OrderNotFound);

        var result = order.Cancel(command.Reason);
        if (!result.IsSuccess)
            return Result<Guid>.Fail(result.GetErrorOrThrow());

        return Result<Guid>.Ok(order.Id);
    }
}
