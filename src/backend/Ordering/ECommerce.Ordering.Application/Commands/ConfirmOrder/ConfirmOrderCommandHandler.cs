namespace ECommerce.Ordering.Application.Commands.ConfirmOrder;

public class ConfirmOrderCommandHandler(IOrderRepository orders) : IRequestHandler<ConfirmOrderCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(ConfirmOrderCommand command, CancellationToken ct)
    {
        var order = await orders.GetByIdAsync(command.OrderId, ct);
        if (order is null)
            return Result<Guid>.Fail(OrderingApplicationErrors.OrderNotFound);

        var result = order.Confirm();
        if (!result.IsSuccess)
            return Result<Guid>.Fail(result.GetErrorOrThrow());

        return Result<Guid>.Ok(order.Id);
    }
}
