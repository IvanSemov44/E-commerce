using ECommerce.Contracts;
using ECommerce.Ordering.Application.Interfaces;
using MediatR;

namespace ECommerce.Ordering.Infrastructure.EventHandlers;

public sealed class InventoryReservedIntegrationEventHandler(IOrderFulfillmentSagaService sagaService)
    : INotificationHandler<InventoryReservedIntegrationEvent>
{
    public Task Handle(InventoryReservedIntegrationEvent notification, CancellationToken cancellationToken)
        => sagaService.HandleInventoryReservedAsync(notification, cancellationToken);
}
