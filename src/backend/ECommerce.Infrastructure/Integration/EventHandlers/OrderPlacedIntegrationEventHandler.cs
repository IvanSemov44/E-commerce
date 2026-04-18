using ECommerce.Contracts;
using MediatR;

namespace ECommerce.Infrastructure.Integration.EventHandlers;

public sealed class OrderPlacedIntegrationEventHandler(IOrderFulfillmentSagaService sagaService)
    : INotificationHandler<OrderPlacedIntegrationEvent>
{
    public Task Handle(OrderPlacedIntegrationEvent notification, CancellationToken cancellationToken)
        => sagaService.StartAsync(notification, cancellationToken);
}
