using ECommerce.Contracts;

namespace ECommerce.Inventory.Infrastructure.Integration;

public interface IInventoryOutboxEventWriter
{
    Task EnqueueAsync(IntegrationEvent integrationEvent, CancellationToken cancellationToken = default);
}
