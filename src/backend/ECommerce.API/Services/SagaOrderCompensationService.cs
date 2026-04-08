using ECommerce.Infrastructure.Integration;
using ECommerce.Ordering.Application.Commands.CancelOrder;
using MediatR;

namespace ECommerce.API.Services;

public sealed class SagaOrderCompensationService(
    IMediator mediator,
    ILogger<SagaOrderCompensationService> logger) : IOrderCompensationService
{
    public async Task CompensateOrderAsync(Guid orderId, string reason, CancellationToken cancellationToken = default)
    {
        var result = await mediator.Send(new CancelOrderCommand(orderId, reason), cancellationToken);
        if (!result.IsSuccess)
        {
            var error = result.GetErrorOrThrow();
            logger.LogWarning(
                "Order compensation failed for order {OrderId}. Error code: {Code}. Error message: {Message}",
                orderId,
                error.Code,
                error.Message);
        }
    }
}
