using ECommerce.Contracts;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ECommerce.Infrastructure.Integration;

public sealed class IntegrationEventDispatcher(
    InboxIdempotencyProcessor inbox,
    IPublisher mediator,
    IOrderFulfillmentSagaService sagaService,
    ILogger<IntegrationEventDispatcher> logger) : IIntegrationEventDispatcher
{
    public async Task DispatchAsync(IntegrationEvent integrationEvent, CancellationToken cancellationToken = default)
    {
        switch (integrationEvent)
        {
            case ProductProjectionUpdatedIntegrationEvent message:
                await inbox.ExecuteAsync(message, ct => mediator.Publish(message, ct), cancellationToken);
                break;

            case ProductImageProjectionUpdatedIntegrationEvent message:
                await inbox.ExecuteAsync(message, ct => mediator.Publish(message, ct), cancellationToken);
                break;

            case PromoCodeProjectionUpdatedIntegrationEvent message:
                await inbox.ExecuteAsync(message, ct => mediator.Publish(message, ct), cancellationToken);
                break;

            case AddressProjectionUpdatedIntegrationEvent message:
                await inbox.ExecuteAsync(message, ct => mediator.Publish(message, ct), cancellationToken);
                break;

            case InventoryStockProjectionUpdatedIntegrationEvent message:
                await inbox.ExecuteAsync(message, ct => mediator.Publish(message, ct), cancellationToken);
                break;

            case OrderPlacedIntegrationEvent message:
                await inbox.ExecuteAsync(message, ct => sagaService.StartAsync(message, ct), cancellationToken);
                break;

            case InventoryReservedIntegrationEvent message:
                await inbox.ExecuteAsync(message, ct => sagaService.HandleInventoryReservedAsync(message, ct), cancellationToken);
                break;

            case InventoryReservationFailedIntegrationEvent message:
                await inbox.ExecuteAsync(message, ct => sagaService.HandleInventoryReservationFailedAsync(message, ct), cancellationToken);
                break;

            default:
                logger.LogWarning("Unhandled integration event type: {EventType}", integrationEvent.GetType().FullName);
                break;
        }

        logger.LogDebug("Processed integration event {EventType} with idempotency key {IdempotencyKey}", integrationEvent.GetType().Name, integrationEvent.IdempotencyKey);
    }
}
