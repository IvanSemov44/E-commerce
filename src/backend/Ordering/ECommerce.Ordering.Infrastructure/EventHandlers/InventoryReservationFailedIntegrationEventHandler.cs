using ECommerce.Contracts;
using ECommerce.Ordering.Application.Interfaces;
using MediatR;

namespace ECommerce.Ordering.Infrastructure.EventHandlers;

public sealed class InventoryReservationFailedIntegrationEventHandler(IOrderFulfillmentSagaService sagaService)
    : INotificationHandler<InventoryReservationFailedIntegrationEvent>
{
    public Task Handle(InventoryReservationFailedIntegrationEvent notification, CancellationToken cancellationToken)
        => sagaService.HandleInventoryReservationFailedAsync(notification, cancellationToken);
}
