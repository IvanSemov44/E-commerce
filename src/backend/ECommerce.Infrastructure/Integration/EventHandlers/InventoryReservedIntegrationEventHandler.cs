using ECommerce.Contracts;
using MediatR;

namespace ECommerce.Infrastructure.Integration.EventHandlers;

public sealed class InventoryReservedIntegrationEventHandler(IOrderFulfillmentSagaService sagaService)
    : INotificationHandler<InventoryReservedIntegrationEvent>
{
    public Task Handle(InventoryReservedIntegrationEvent notification, CancellationToken cancellationToken)
        => sagaService.HandleInventoryReservedAsync(notification, cancellationToken);
}
