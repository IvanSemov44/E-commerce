namespace ECommerce.Ordering.Application.Commands.ShipOrder;

public class ShipOrderCommandHandler(IOrderRepository orders) : IRequestHandler<ShipOrderCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(ShipOrderCommand command, CancellationToken ct)
    {
        var order = await orders.GetByIdAsync(command.OrderId, ct);
        if (order is null)
            return Result<Guid>.Fail(OrderingApplicationErrors.OrderNotFound);

        var result = order.Ship(command.TrackingNumber);
        if (!result.IsSuccess)
            return Result<Guid>.Fail(result.GetErrorOrThrow());

        return Result<Guid>.Ok(order.Id);
    }
}
