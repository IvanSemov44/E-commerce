using ECommerce.Contracts;
using MediatR;

namespace ECommerce.Infrastructure.Integration.EventHandlers;

public sealed class InventoryReservationFailedIntegrationEventHandler(IOrderFulfillmentSagaService sagaService)
    : INotificationHandler<InventoryReservationFailedIntegrationEvent>
{
    public Task Handle(InventoryReservationFailedIntegrationEvent notification, CancellationToken cancellationToken)
        => sagaService.HandleInventoryReservationFailedAsync(notification, cancellationToken);
}
