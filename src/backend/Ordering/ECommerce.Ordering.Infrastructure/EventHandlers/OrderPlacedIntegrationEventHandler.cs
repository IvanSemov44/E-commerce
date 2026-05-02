using ECommerce.Contracts;
using ECommerce.Ordering.Application.Interfaces;
using MediatR;

namespace ECommerce.Ordering.Infrastructure.EventHandlers;

public sealed class OrderPlacedIntegrationEventHandler(IOrderFulfillmentSagaService sagaService)
    : INotificationHandler<OrderPlacedIntegrationEvent>
{
    public Task Handle(OrderPlacedIntegrationEvent notification, CancellationToken cancellationToken)
        => sagaService.StartAsync(notification, cancellationToken);
}
